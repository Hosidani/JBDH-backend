import json
import logging
import os
from typing import Dict, Iterable, List

from django.conf import settings

from apps.ai_services.services.file_text_extraction_service import extract_text_from_file
from apps.ai_services.services.minilm_embedding_service import cosine_similarity
from apps.ai_services.services.cv_job_analysis_service import extract_cv_job_analysis, extract_field_analysis
from apps.ai_services.services.cv_anonymization_service import CVAnonymizationService
from core.ats_client import score_cv_job_ats
from core.utils import vectorize_text

logger = logging.getLogger(__name__)


class CVJobSSEService:
    """Orchestrates the SSE flow for CV vs job ad analysis."""

    @staticmethod
    def _build_event(
        progress: int,
        message: str | None = None,
        error: str | None = None,
        result: Dict | None = None,
        vector_result: Dict | None = None,
        ats_result: Dict | None = None,
    ) -> str:
        payload: Dict = {"progress": progress}
        if message:
            payload["message"] = message
        if error:
            payload["error"] = error
        if result is not None:
            payload["result"] = result
        if vector_result is not None:
            payload["vectorResult"] = vector_result
        if ats_result is not None:
            payload["atsResult"] = ats_result
        return f"data: {json.dumps(payload)}\n\n"

    @staticmethod
    def _save_and_extract_cv_text(cv_file) -> str:
        upload_dir = os.path.join(settings.MEDIA_ROOT, "uploads")
        os.makedirs(upload_dir, exist_ok=True)

        saved_name = cv_file.name
        saved_path = os.path.join(upload_dir, saved_name)
        with open(saved_path, "wb+") as destination:
            for chunk in cv_file.chunks():
                destination.write(chunk)

        return (extract_text_from_file(saved_path) or "").strip()

    @staticmethod
    def _join_profession_fields(profession: Dict) -> str:
        if not isinstance(profession, dict):
            return ""
        return " ".join(
            [profession.get(key, "") for key in ["main", "sub", "specialty"] if profession.get(key)]
        )

    @staticmethod
    def _join_skill_names(skills: List[Dict]) -> str:
        if not isinstance(skills, list):
            return ""
        return " ".join(
            [skill.get("name", "") for skill in skills if isinstance(skill, dict) and skill.get("name")]
        )

    @staticmethod
    def _build_weighted_skill_string(skills: List[Dict]) -> str:
        """Repeat skill names by importance so MiniLM weighs high-importance skills more.
        importance 5 → 3x, 4 → 2x, 3 → 1x, <3 → excluded.
        Sorted DESC so critical skills survive 256-token truncation.
        """
        if not isinstance(skills, list):
            return ""
        sorted_skills = sorted(skills, key=lambda s: s.get("importance", 3), reverse=True)
        parts: List[str] = []
        for skill in sorted_skills:
            name = skill.get("name", "").strip()
            importance = skill.get("importance", 3)
            if not name or importance < 3:
                continue
            repeats = importance - 2  # 3→1, 4→2, 5→3
            parts.extend([name] * repeats)
        return " ".join(parts)

    @staticmethod
    def _safe_similarity_percent(left_text: str, right_text: str) -> float:
        if not left_text.strip() or not right_text.strip():
            return 0.0
        return round(cosine_similarity(vectorize_text(left_text), vectorize_text(right_text)) * 100, 2)

    @staticmethod
    def _load_cv_text(request) -> tuple[str | None, str | None]:
        """Extract CV text from the request. Returns (cv_text, error_message)."""
        cv_file = request.FILES.get("CvPath")
        external_cv_id = (
            request.POST.get("external_cv_id")
            or getattr(request, "data", {}).get("external_cv_id")
        )
        if external_cv_id:
            from apps.ai_services.models import ExternalCV
            try:
                cv_obj = ExternalCV.objects.get(pk=int(external_cv_id), user=request.user)
                return cv_obj.text.strip(), None
            except (ExternalCV.DoesNotExist, ValueError, TypeError):
                return None, "Valt CV hittades inte."
        elif cv_file:
            cv_text = CVJobSSEService._save_and_extract_cv_text(cv_file)
            if not cv_text:
                return None, "Kunde inte extrahera CV-text."
            return cv_text, None
        else:
            return None, "CV och Jobbtext krävs."

    @staticmethod
    def _run_ats(cv_text: str, job_ad_text: str) -> Dict | None:
        """Run ATS scoring. Returns structured ATS payload or None on failure."""
        try:
            raw_ats = score_cv_job_ats(cv_text, job_ad_text)
            return {
                "totalScore": raw_ats.get("total_score"),
                "breakdown": raw_ats.get("breakdown"),
                "matchedSkills": raw_ats.get("matched_skills", []),
                "missingSkills": raw_ats.get("missing_skills", []),
                "bonusSkills": raw_ats.get("bonus_skills", []),
                "recommendations": raw_ats.get("recommendations", []),
            }
        except Exception as exc:
            logger.warning("ATS-analys misslyckades (fortsätter utan den): %s", exc)
            return None

    @staticmethod
    def _build_t5_payload(result_json: Dict) -> tuple[Dict, str, str, str, str]:
        """Compute vector similarities and build the frontend result payload.
        Returns (payload, cv_profession_str, cv_skills_str, job_profession_str, job_requirements_str).
        """
        cv_profession_str = CVJobSSEService._join_profession_fields(result_json.get("cv_profession", {}))
        job_profession_str = CVJobSSEService._join_profession_fields(result_json.get("job_profession", {}))
        profession_vector_similarity = CVJobSSEService._safe_similarity_percent(cv_profession_str, job_profession_str)

        cv_weighted_skills = CVJobSSEService._build_weighted_skill_string(result_json.get("cv_skills", []))
        job_weighted_requirements = CVJobSSEService._build_weighted_skill_string(result_json.get("job_requirements", []))
        skills_vector_similarity = CVJobSSEService._safe_similarity_percent(cv_weighted_skills, job_weighted_requirements)

        # Plain skill strings for Template 6 (readable, not repeated)
        cv_skills_str = CVJobSSEService._join_skill_names(result_json.get("cv_skills", []))
        job_requirements_str = CVJobSSEService._join_skill_names(result_json.get("job_requirements", []))

        payload: Dict = {
            "summary": result_json.get("summary", ""),
            "matching_skills": result_json.get("matching_skills", []),
            "missing_requirements": result_json.get("gaps", []),
            "relevant_experiences": result_json.get("relevant_experiences", []),
            "education_and_certifications": result_json.get("education_and_certifications", []),
            "strengths": result_json.get("strengths", []),
            "gaps": result_json.get("gaps", []),
            "profession_vector_similarity": profession_vector_similarity,
            "skills_vector_similarity": skills_vector_similarity,
        }
        return payload, cv_profession_str, cv_skills_str, job_profession_str, job_requirements_str

    def generate_events(self, request) -> Iterable[str]:
        """Orchestrates the SSE flow: CV load → ATS → T5 → field analysis → done."""
        yield self._build_event(progress=10, message="🔄 Laddar CV...")

        job_ad_text = (
            getattr(request, "data", {}).get("JobAdText")
            or request.POST.get("JobAdText")
            or ""
        ).strip()
        if not job_ad_text:
            yield self._build_event(progress=100, error="CV och Jobbtext krävs.")
            return

        cv_text, cv_error = self._load_cv_text(request)
        if cv_error:
            yield self._build_event(progress=100, error=cv_error)
            return

        # --- GDPR / AI Act: anonymize PII before sending to any AI model ---
        logger.info("CV-text INNAN anonymisering (första 300 tecken):\n%s", cv_text[:300])
        try:
            anon_result = CVAnonymizationService.anonymize_raw_text(cv_text)
            cv_text = anon_result.anonymized_text
            logger.info("CV anonymiserat innan AI-anrop: %s", anon_result.audit_summary())
            logger.info("CV-text EFTER anonymisering (första 300 tecken):\n%s", cv_text[:300])
        except Exception as exc:
            logger.warning("Anonymisering misslyckades (fortsätter med original): %s", exc)

        yield self._build_event(progress=30, message="📋 ATS-analys pågår...")

        ats_result = self._run_ats(cv_text, job_ad_text)
        if ats_result:
            yield self._build_event(
                progress=60,
                message=f"📋 ATS-poäng: {ats_result.get('totalScore')}%",
                ats_result=ats_result,
            )
        else:
            yield self._build_event(progress=60, message="⚠️ ATS-analys ej tillgänglig.")

        yield self._build_event(progress=70, message="🧠 DeepSeek analyserar...")

        result_json = extract_cv_job_analysis(cv_text, job_ad_text)
        if not result_json:
            yield self._build_event(progress=100, error="AI-analys misslyckades.")
            return

        frontend_payload, cv_profession_str, cv_skills_str, job_profession_str, job_requirements_str = (
            self._build_t5_payload(result_json)
        )

        yield self._build_event(progress=90, message="🔍 Fältanalys pågår...")

        try:
            field_analysis = extract_field_analysis(
                cv_profession=cv_profession_str,
                cv_skills=cv_skills_str,
                job_profession=job_profession_str,
                job_requirements=job_requirements_str,
            )
            if field_analysis:
                frontend_payload["field_analysis"] = field_analysis
        except Exception as exc:
            logger.warning("Fältanalys misslyckades (fortsätter utan den): %s", exc)

        yield self._build_event(
            progress=100,
            message="✅ AI-analys klar!",
            result=frontend_payload,
            ats_result=ats_result,
        )
