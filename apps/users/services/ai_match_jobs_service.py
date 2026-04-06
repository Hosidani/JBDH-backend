from core.exceptions import InputValidationError
from apps.ai_services.services.user_ai_job_matching_service import UserAIJobMatchingService

class AIMatchService:
    @staticmethod
    def ai_match_jobs(user, session_id, language="English", job_id=None):
        session_id = (session_id or "").strip()
        language = (language or "English").strip()

        if not session_id:
            raise InputValidationError("sessionId is required.")

        if job_id is not None:
            try:
                job_id = int(job_id)
            except (TypeError, ValueError):
                raise InputValidationError("jobId must be an integer.")

        result = UserAIJobMatchingService().ai_match_from_session(
            user=user,
            session_id=session_id,
            language=language,
            job_id=job_id,
        )

        if not result.get("success"):
            raise InputValidationError(result.get("error", "AI matching failed"))

        return result