import json
import logging
import re
import uuid
from typing import Any, Callable, Dict, List, Tuple

from apps.ai_services.infrastructure.ai_client import AIClient
from apps.ai_services.infrastructure.ai_message_templates import AIMessageTemplates
from apps.ai_services.services.user_ai_response_service import UserAIResponseService
from apps.jobs.models import Job
from core.utils import rank_by_vector_similarity, vectorize_text
from django.core.cache import cache

logger = logging.getLogger(__name__)


SESSION_TTL_SECONDS = 15 * 60


class UserAIJobMatchingService:
    def __init__(
        self,
        ai_call: Callable[..., Dict[str, Any]] | None = None,
        vectorize_fn: Callable[[str], List[float]] | None = None,
        user_ai_response_service: UserAIResponseService | None = None,
    ):
        self._ai_call = ai_call or AIClient.call_deepseek
        self._vectorize_fn = vectorize_fn or vectorize_text
        self._user_ai_response_service = user_ai_response_service or UserAIResponseService()

    def compare_latest_user_ai_response_to_jobs(
        self,
        user,
        top_k: int = 5,
        min_score: float = 0.2,
        language: str = "English",
    ) -> Dict[str, Any]:
        vector_result = self.create_vector_search_session(
            user=user,
            top_k=top_k,
            min_score=min_score,
            language=language,
        )
        if not vector_result.get("success"):
            return vector_result

        ai_result = self.ai_match_from_session(
            user=user,
            session_id=vector_result["sessionId"],
            language=language,
        )
        if not ai_result.get("success"):
            return ai_result

        return {
            "success": True,
            "source": "latest_user_ai_response",
            "userAiResponseId": vector_result.get("userAiResponseId"),
            "sessionId": vector_result.get("sessionId"),
            "topJobs": vector_result.get("topJobs", []),
            "jobAnalyses": ai_result.get("jobAnalyses", []),
            "matchingAnalysis": ai_result.get("matchingAnalysis", self._empty_analysis()),
        }

    def create_vector_search_session(
        self,
        user,
        top_k: int = 5,
        min_score: float = 0.2,
        language: str = "English",
    ) -> Dict[str, Any]:
        latest_response = self._user_ai_response_service.get_latest_user_ai_response(user)
        if not latest_response or not (latest_response.response_text or "").strip():
            return {
                "success": False,
                "error": "No saved AI CV analysis found for this user.",
            }

        user_ai_response_text = (latest_response.response_text or "").strip()
        query_vector = self._vectorize_fn(user_ai_response_text)
        if not query_vector:
            return {
                "success": False,
                "error": "Could not create vector from saved AI response.",
            }

        jobs = self._find_top_jobs(query_vector=query_vector, top_k=top_k, min_score=min_score)
        session_id = str(uuid.uuid4())
        cache_payload = {
            "user_id": user.id,
            "user_ai_response_id": latest_response.id,
            "user_ai_response_text": user_ai_response_text,
            "jobs_payload": jobs,
            "top_k": top_k,
            "min_score": min_score,
            "language": language,
        }
        cache.set(self._cache_key(session_id), cache_payload, timeout=SESSION_TTL_SECONDS)

        return {
            "success": True,
            "source": "latest_user_ai_response",
            "userAiResponseId": latest_response.id,
            "sessionId": session_id,
            "topJobs": jobs,
        }

    def ai_match_from_session(
        self,
        user,
        session_id: str,
        language: str = "English",
        job_id: int | None = None,
    ) -> Dict[str, Any]:
        payload = cache.get(self._cache_key(session_id))
        if not payload or payload.get("user_id") != user.id:
            return {
                "success": False,
                "error": "Vector search session not found.",
            }

        user_ai_response_text = (payload.get("user_ai_response_text") or "").strip()
        if not user_ai_response_text:
            return {
                "success": False,
                "error": "Saved user AI response is empty.",
            }

        jobs = payload.get("jobs_payload") or []
        jobs_to_analyze = jobs
        if job_id is not None:
            jobs_to_analyze = [job for job in jobs if int(job.get("jobId") or 0) == int(job_id)]
            if not jobs_to_analyze:
                fallback_job = (
                    Job.objects
                    .filter(id=job_id)
                    .only("id", "job_id", "title", "company", "location", "description")
                    .first()
                )
                if not fallback_job or not (fallback_job.description or "").strip():
                    return {
                        "success": False,
                        "error": "Requested job not found for analysis.",
                    }

                jobs_to_analyze = [
                    {
                        "jobId": fallback_job.id,
                        "externalJobId": fallback_job.job_id,
                        "title": fallback_job.title,
                        "company": fallback_job.company,
                        "location": fallback_job.location,
                        "score": None,
                        "description": fallback_job.description,
                    }
                ]

        analyses = self._analyze_jobs(
            user_ai_response_text=user_ai_response_text,
            jobs=jobs_to_analyze,
            language=language or payload.get("language") or "English",
        )

        primary_analysis = analyses[0] if analyses else self._empty_analysis()

        return {
            "success": True,
            "sessionId": session_id,
            "userAiResponseId": payload.get("user_ai_response_id"),
            "topJobs": jobs,
            "jobAnalyses": analyses,
            "jobId": job_id,
            "matchingAnalysis": {
                "matches": primary_analysis["matches"],
                "nonMatches": primary_analysis["nonMatches"],
                "improvements": primary_analysis["improvements"],
                "coachTips": primary_analysis["coachTips"],
            },
        }

    @staticmethod
    def _cache_key(session_id: str) -> str:
        return f"user-ai-job-session:{session_id}"

    def _find_top_jobs(self, query_vector: List[float], top_k: int, min_score: float) -> List[Dict[str, Any]]:
        candidates = (
            Job.objects
            .exclude(text_vector__isnull=True)
            .exclude(text_vector=[])
            .exclude(description__isnull=True)
            .exclude(description="")
            .only("id", "job_id", "title", "company", "location", "description", "text_vector")
            .iterator(chunk_size=500)
        )

        ranked = rank_by_vector_similarity(
            query_vector=query_vector,
            candidates=candidates,
            vector_getter=lambda job: job.text_vector or [],
            min_score=min_score,
            top_k=top_k,
        )

        jobs: List[Dict[str, Any]] = []
        for job, score in ranked:
            jobs.append(
                {
                    "jobId": job.id,
                    "externalJobId": job.job_id,
                    "title": job.title,
                    "company": job.company,
                    "location": job.location,
                    "score": round(score, 6),
                    "description": job.description,
                }
            )

        return jobs

    def _analyze_jobs(self, user_ai_response_text: str, jobs: List[Dict[str, Any]], language: str) -> List[Dict[str, Any]]:
        template = AIMessageTemplates.USER_MESSAGE_TEMPLATES[4]
        analyses: List[Dict[str, Any]] = []

        for job in jobs:
            prompt = template.format(
                UserAIResponse=user_ai_response_text,
                JobDescription=job.get("description") or "",
            )

            try:
                ai_result = self._ai_call(prompt, language=language)
                content = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
                parsed = self._try_parse_json(content)
                analyses.append(self._to_frontend_analysis(job=job, parsed=parsed, raw=content))
            except Exception as ex:
                logger.exception("Failed to analyze job_id=%s: %s", job.get("jobId"), ex)
                analyses.append(self._to_frontend_analysis(job=job, parsed={}, raw=""))

        return analyses

    @staticmethod
    def _empty_analysis() -> Dict[str, Any]:
        return {
            "matches": [],
            "nonMatches": [],
            "improvements": [],
            "coachTips": "",
        }

    @staticmethod
    def _try_parse_json(content: str) -> Dict[str, Any]:
        cleaned = content.strip()
        cleaned = re.sub(r"^```json", "", cleaned).strip()
        cleaned = re.sub(r"^```", "", cleaned).strip()
        cleaned = re.sub(r"```$", "", cleaned).strip()

        try:
            parsed = json.loads(cleaned)
            return parsed if isinstance(parsed, dict) else {}
        except json.JSONDecodeError:
            return {}

    def _to_frontend_analysis(self, job: Dict[str, Any], parsed: Dict[str, Any], raw: str) -> Dict[str, Any]:
        matching_points = self._pick_list(parsed, ["MatchingPoints", "MatchingSkills"])
        non_matching_points = self._pick_list(parsed, ["NonMatchingPoints", "MissingRequirements"])
        improvements = self._pick_list(parsed, ["ImprovementOpportunities", "SkillDevelopment", "RecommendedImprovements"])

        coach_tips = str(
            parsed.get("CoachTips")
            or parsed.get("FitSummary")
            or ""
        ).strip()

        return {
            "jobId": job.get("jobId"),
            "externalJobId": job.get("externalJobId"),
            "title": job.get("title"),
            "company": job.get("company"),
            "location": job.get("location"),
            "score": job.get("score"),
            "description": job.get("description"),
            "fitSummary": str(parsed.get("FitSummary") or "").strip(),
            "matches": self._to_item_list(matching_points),
            "nonMatches": self._to_item_list(non_matching_points),
            "improvements": self._to_item_list(improvements),
            "coachTips": coach_tips,
            "raw": raw,
        }

    @staticmethod
    def _pick_list(parsed: Dict[str, Any], keys: List[str]) -> List[str]:
        for key in keys:
            value = parsed.get(key)
            if isinstance(value, list):
                return [str(item).strip() for item in value if str(item).strip()]
        return []

    @staticmethod
    def _to_item_list(items: List[str]) -> List[Dict[str, Any]]:
        return [
            {"id": index + 1, "label": item}
            for index, item in enumerate(items)
        ]
