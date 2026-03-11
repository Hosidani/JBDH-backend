import logging
from typing import Any, Dict, List

from apps.ai_services.ai_models import RequirementExtractionResponse
from apps.jobs.models import Job
from core.utils import build_embedding_text, rank_by_vector_similarity, vectorize_text

logger = logging.getLogger(__name__)


class CVJobVectorService:
    @staticmethod
    def _build_cv_embedding_text(cv_extraction: RequirementExtractionResponse) -> str:
        if not cv_extraction:
            return ""

        profession = cv_extraction.profession
        requirements = cv_extraction.requirements or []

        requirement_names = [r.name.strip() for r in requirements if getattr(r, "name", None) and r.name.strip()]
        return build_embedding_text([
            getattr(profession, "main", "") if profession else "",
            getattr(profession, "sub", "") if profession else "",
            getattr(profession, "specialty", "") if profession else "",
            " ".join(requirement_names),
        ])

    @staticmethod
    def find_best_matching_jobs(
        cv_extraction: RequirementExtractionResponse,
        top_k: int = 5,
        min_score: float = 0.2,
    ) -> List[Dict[str, Any]]:
        if not cv_extraction:
            logger.debug("find_best_matching_jobs: cv_extraction saknas")
            return []

        if top_k <= 0:
            logger.debug("find_best_matching_jobs: ogiltig top_k=%s", top_k)
            return []

        cv_text = CVJobVectorService._build_cv_embedding_text(cv_extraction)
        query_vector = vectorize_text(cv_text)
        if not query_vector:
            logger.warning("find_best_matching_jobs: kunde inte skapa CV-vektor")
            return []

        candidates = (
            Job.objects
            .exclude(text_vector__isnull=True)
            .exclude(text_vector=[])
            .only("id", "job_id", "title", "company", "location", "text_vector")
            .iterator(chunk_size=500)
        )

        ranked = rank_by_vector_similarity(
            query_vector=query_vector,
            candidates=candidates,
            vector_getter=lambda job: job.text_vector or [],
            min_score=min_score,
            top_k=top_k,
        )

        results: List[Dict[str, Any]] = []
        for job, score in ranked:
            results.append(
                {
                    "jobId": job.id,
                    "externalJobId": job.job_id,
                    "title": job.title,
                    "company": job.company,
                    "location": job.location,
                    "score": round(score, 6),
                }
            )

        return results
