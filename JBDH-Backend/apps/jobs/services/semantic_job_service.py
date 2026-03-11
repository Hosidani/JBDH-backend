import logging
from typing import Any, Dict, List

from apps.jobs.models import Job
from core.utils import build_embedding_text, rank_by_vector_similarity, vectorize_text

logger = logging.getLogger(__name__)


class SemanticJobService:
    @staticmethod
    def search_similar_jobs(query_text: str, top_k: int = 10, min_score: float = 0.2) -> List[Dict[str, Any]]:
        normalized_query = build_embedding_text([query_text])
        if not normalized_query:
            logger.debug("search_similar_jobs: tom query")
            return []

        if top_k <= 0:
            logger.debug("search_similar_jobs: ogiltig top_k=%s", top_k)
            return []

        query_vector = vectorize_text(normalized_query)
        if not query_vector:
            logger.warning("search_similar_jobs: kunde inte skapa query-vektor")
            return []

        candidates = (
            Job.objects
            .exclude(text_vector__isnull=True)
            .exclude(text_vector=[])
            .only("id", "text_vector")
            .iterator(chunk_size=500)
        )

        ranked = rank_by_vector_similarity(
            query_vector=query_vector,
            candidates=candidates,
            vector_getter=lambda candidate: candidate.text_vector or [],
            min_score=min_score,
            top_k=top_k,
        )

        if not ranked:
            return []

        ordered_ids = [candidate.id for candidate, _ in ranked]
        jobs_map = Job.objects.in_bulk(ordered_ids)

        results: List[Dict[str, Any]] = []
        for candidate, score in ranked:
            job = jobs_map.get(candidate.id)
            if job is None:
                continue
            results.append({"job": job, "score": round(score, 6)})

        logger.info(
            "semantic search klar | query_len=%s ranked=%s results=%s",
            len(normalized_query),
            len(ranked),
            len(results),
        )
        return results
