import json
import logging
import os
from typing import Dict, Iterable, List

from django.conf import settings

from apps.ai_services.services.file_text_extraction_service import extract_text_from_file
from apps.ai_services.services.minilm_embedding_service import cosine_similarity
from apps.ai_services.services.cv_job_analysis_service import extract_cv_job_analysis
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
    def _safe_similarity_percent(left_text: str, right_text: str) -> float:
        if not left_text.strip() or not right_text.strip():
            return 0.0
        return round(cosine_similarity(vectorize_text(left_text), vectorize_text(right_text)) * 100, 2)

    def generate_events(self, request) -> Iterable[str]:
        yield self._build_event(progress=10, message="🔄 Laddar CV...")

        cv_file = request.FILES.get("CvPath")
        external_cv_id = (
            request.POST.get("external_cv_id")
            or getattr(request, "data", {}).get("external_cv_id")
        )
        job_ad_text = (
            getattr(request, "data", {}).get("JobAdText")
            or request.POST.get("JobAdText")
            or ""
        ).strip()

        if not job_ad_text:
            yield self._build_event(progress=100, error="CV och Jobbtext krävs.")
            return

        if external_cv_id:
            from apps.ai_services.models import ExternalCV
            try:
                cv_obj = ExternalCV.objects.get(pk=int(external_cv_id), user=request.user)
                cv_text = cv_obj.text.strip()
            except (ExternalCV.DoesNotExist, ValueError, TypeError):
                yield self._build_event(progress=100, error="Valt CV hittades inte.")
                return
        elif cv_file:
            cv_text = self._save_and_extract_cv_text(cv_file)
        else:
            yield self._build_event(progress=100, error="CV och Jobbtext krävs.")
            return

        if not cv_text:
            yield self._build_event(progress=100, error="Kunde inte extrahera CV-text.")
            return

        try:
            cv_vector = vectorize_text(cv_text)
            job_ad_vector = vectorize_text(job_ad_text)
            vector_similarity = round(cosine_similarity(cv_vector, job_ad_vector) * 100, 2)
            vector_result = {
                "similarityScore": vector_similarity,
                "cvVectorSize": len(cv_vector),
                "jobAdVectorSize": len(job_ad_vector),
            }
            yield self._build_event(
                progress=30,
                message=f"⚡ Vektoranalys klar: {vector_similarity}%",
                vector_result=vector_result,
            )
        except Exception as exc:
            logger.exception("On-the-fly CV vs JobAd vector compare failed: %s", exc)
            yield self._build_event(progress=100, error="Vektoranalys misslyckades.")
            return

        yield self._build_event(progress=50, message="🧠 DeepSeek analyserar...")

        result_json = extract_cv_job_analysis(cv_text, job_ad_text)
        if not result_json:
            yield self._build_event(progress=100, error="AI-analys misslyckades.")
            return

        cv_profession_str = self._join_profession_fields(result_json.get("cv_profession", {}))
        job_profession_str = self._join_profession_fields(result_json.get("job_profession", {}))
        profession_vector_similarity = self._safe_similarity_percent(cv_profession_str, job_profession_str)

        cv_skills_str = self._join_skill_names(result_json.get("cv_skills", []))
        job_requirements_str = self._join_skill_names(result_json.get("job_requirements", []))
        skills_vector_similarity = self._safe_similarity_percent(cv_skills_str, job_requirements_str)

        frontend_payload = {
            "summary": result_json.get("summary", ""),
            "matching_skills": result_json.get("cv_skills", []),
            "missing_requirements": result_json.get("gaps", []),
            "relevant_experiences": result_json.get("relevant_experiences", []),
            "education_and_certifications": result_json.get("education_and_certifications", []),
            "strengths": result_json.get("strengths", []),
            "gaps": result_json.get("gaps", []),
            "profession_vector_similarity": profession_vector_similarity,
            "skills_vector_similarity": skills_vector_similarity,
        }

        yield self._build_event(
            progress=100,
            message="✅ AI-analys klar!",
            result=frontend_payload,
        )
