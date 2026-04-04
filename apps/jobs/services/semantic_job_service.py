import logging
from functools import lru_cache
from typing import Any, Dict, List
from django.db.models import Case, ExpressionWrapper, F, FloatField, Q, Value, When
from django.db.models.functions import Coalesce
from pgvector.django import CosineDistance

from apps.jobs.dto_mapper import build_job_details_dto
from apps.jobs.models import Job
from core.utils import build_embedding_text, rank_by_vector_similarity, vectorize_text

logger = logging.getLogger(__name__)


class SemanticJobConfig:
    FIELD_MAPPING = {
        'description': 'description_vector',
        'location': 'location_vector',
        'workArea': 'line_of_business_vector',
        'roles': 'title_vector',
        'competence': 'competence_vector',
    }

    # NOTE on "competence" field:
    # CURRENT (until AI enrichment is ready):
    #   - required_skills column is mostly empty in the database
    #   - Competence search uses TEXT_FALLBACK to search on description field
    #   - User can input "Python", "React", etc. and it will match in job descriptions
    #   - competence_vector remains NULL (no vectors generated for empty required_skills)
    #
    # FUTURE (once AI fills required_skills):
    #   - AI will extract and normalize skills from job descriptions → required_skills
    #   - competence_vector will be generated from the enriched required_skills
    #   - Competence search will use vector similarity (semantic matching) + text fallback
    #   - This will provide much better skill matching (e.g., "Backend" matches "Node.js developer")
    #
    # NO FRONTEND CHANGES NEEDED: The API accepts ?competence=... parameter now and will
    # continue to work seamlessly once the backend has skill vectors.

    TEXT_FALLBACK = {
        'description': ['description', 'title'],
        'location': ['location'],
        'workArea': ['line_of_business', 'title', 'description'],
        'roles': ['title', 'description'],
        'competence': ['required_skills', 'description'],
    }

    THRESHOLDS = {
        'description': 0.3,
        'location': 0.3,
        'workArea': 0.3,
        'roles': 0.3,
        'competence': 0.3,
    }

    @staticmethod
    def multi_field_search(params: Dict[str, str], top_k: int = 20) -> List[Job]:
        """Search med 1-5 fält + vektor + text fallback + pagination."""
        offset = int(params.pop('offset', 0))
        limit = int(params.pop('limit', 20))
        offset = max(0, offset)
        limit = max(1, min(100, limit))
        
        active_fields = {k: v.strip() for k, v in params.items() if isinstance(v, str) and v.strip()}
        if not active_fields:
            jobs = Job.objects.order_by('-posted_date')[offset:offset + limit]
            return list(jobs)

        jobs = Job.objects.all()
        similarity_aliases: List[str] = []
        priority_aliases: List[str] = []

        for fe_field, vector_col in SemanticJobConfig.FIELD_MAPPING.items():
            if fe_field not in active_fields:
                continue

            query_text = active_fields[fe_field]
            query_emb = list(_cached_query_embedding(query_text))
            text_fields = SemanticJobConfig.TEXT_FALLBACK.get(fe_field, [])
            text_q = Q()
            for tf in text_fields:
                text_q |= Q(**{f"{tf}__icontains": query_text})

            if query_emb:
                threshold = SemanticJobConfig.THRESHOLDS.get(fe_field, 0.3)
                sim_name = f"{fe_field}_sim"
                dist_name = f"{fe_field}_dist"

                jobs = jobs.annotate(
                    **{dist_name: CosineDistance(vector_col, query_emb)}
                ).annotate(
                    **{sim_name: ExpressionWrapper(
                        Value(1.0) - F(dist_name),
                        output_field=FloatField(),
                    )}
                ).filter(
                    Q(**{f"{sim_name}__gt": threshold}) | text_q
                )
                similarity_aliases.append(sim_name)

                if fe_field == 'roles':
                    exact_alias = f"{fe_field}_exact"
                    jobs = jobs.annotate(**{
                        exact_alias: Case(
                            When(title__iexact=query_text, then=Value(1.0)),
                            default=Value(0.0),
                            output_field=FloatField(),
                        )
                    })
                    priority_aliases.append(exact_alias)
            else:
                jobs = jobs.filter(text_q)

        if similarity_aliases:
            from functools import reduce
            sim_values = [Coalesce(F(alias), Value(0.0)) for alias in similarity_aliases]
            total_sim = reduce(lambda a, b: a + b, sim_values)
            jobs = jobs.annotate(
                overall_similarity=ExpressionWrapper(
                    total_sim / Value(float(len(similarity_aliases))),
                    output_field=FloatField(),
                )
            )
            order_fields = [f"-{a}" for a in priority_aliases] + ['-overall_similarity', '-posted_date']
            jobs = jobs.order_by(*order_fields)
        else:
            jobs = jobs.order_by('-posted_date')

        return list(jobs[offset:offset + limit])

    @staticmethod
    def serialize_jobs(jobs: List[Job]) -> List[Dict]:
        """Konvertera Job-objekt till API-format."""
        return [
            build_job_details_dto(
                job=j,
                has_applied=False,
                is_saved=False,
                is_not_interested=False,
                notes=None,
            )
            for j in jobs
        ]

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

    @staticmethod
    def multi_field_search(params: Dict[str, str], top_k: int = 20) -> List[Job]:
        """Multi-field semantic search with text fallback."""
        return SemanticJobConfig.multi_field_search(params, top_k)

    @staticmethod
    def serialize_jobs(jobs: List[Job]) -> List[Dict]:
        """Serialize Job objects to API dicts."""
        return SemanticJobConfig.serialize_jobs(jobs)


@lru_cache(maxsize=512)
def _cached_query_embedding(text: str) -> tuple:
    vector = vectorize_text(text, context="job") or []
    return tuple(vector)

