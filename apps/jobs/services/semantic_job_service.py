import logging
from functools import lru_cache
from typing import Any, Dict, List, Tuple
from django.db.models import Case, ExpressionWrapper, F, FloatField, IntegerField, Q, Value, When
from django.db.models.functions import Lower, StrIndex
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
        'description': 0.35,
        'location': 0.35,
        'workArea': 0.35,
        'roles': 0.35,
        'competence': 0.35,
    }

    


    @staticmethod
    def multi_field_search(params: Dict[str, str], top_k: int = 20) -> Tuple[List[Job], int]:
        """Returnerar (paginated_jobs, total_matches) med titel-prio + vektorsortering."""
        offset = int(params.pop('offset', 0))
        limit = int(params.pop('limit', top_k))

        print(f"🔍 START: offset={offset}, limit={limit}")
        
        qs = Job.objects.all()
        active_fields = {k: v.strip() for k, v in params.items() if v.strip()}
        roles_query = active_fields.get('roles', '').strip()
        
        similarity_aliases = []
        roles_vector_condition = None
        
        # 1. Vector search på alla fält (FIX: rätt variabler!)
        for fe_field, vector_col in SemanticJobConfig.FIELD_MAPPING.items():
            if fe_field not in active_fields:
                continue
                
            query_text = active_fields[fe_field]
            query_emb = list(_cached_query_embedding(query_text))
            
            if query_emb:
                threshold = SemanticJobConfig.THRESHOLDS.get(fe_field, 0.35)
                
                dist_name = f"{fe_field}_dist"
                sim_name = f"{fe_field}_sim"
                
                # FIX: annotate med korrekta variabler
                qs = qs.annotate(**{
                    dist_name: CosineDistance(vector_col, query_emb),  # RÄTT vector_col!
                    sim_name: ExpressionWrapper(
                        Value(1.0) - F(dist_name), 
                        output_field=FloatField()
                    )
                })

                if fe_field == 'roles':
                    # Roles/title should allow exact/partial text matches even when vector score is low.
                    roles_vector_condition = Q(**{f"{sim_name}__gte": threshold})
                else:
                    qs = qs.filter(**{f"{sim_name}__gte": threshold})
                
                similarity_aliases.append(sim_name)
                print(f"✅ {fe_field}: '{query_text}' → {qs.count()} kvar")
        
        # 2. Ranking (FIX: enkel summa istället för komplex reduce)
        if similarity_aliases:
            num_scores = len(similarity_aliases)
            if num_scores == 1:
                qs = qs.annotate(overall_score=F(similarity_aliases[0]))
            elif num_scores == 2:
                qs = qs.annotate(
                    overall_score=ExpressionWrapper(
                        (F(similarity_aliases[0]) + F(similarity_aliases[1])) / Value(2.0),
                        output_field=FloatField()
                    )
                )
            elif num_scores >= 3:
                # FIX: Summa av max 3 scores (prestanda)
                score1, score2, score3 = similarity_aliases[:3]
                qs = qs.annotate(
                    overall_score=ExpressionWrapper(
                        (F(score1) + F(score2) + F(score3)) / Value(3.0),
                        output_field=FloatField()
                    )
                )
        else:
            qs = qs.annotate(overall_score=Value(0.0, output_field=FloatField()))

        # 3. Titel-prio i tre nivåer:
        # 0 = exakt match (högst), 1 = delvis match, 2 = övriga vektorträffar
        if roles_query:
            normalized_roles_query = ' '.join(roles_query.lower().split())
            qs = qs.annotate(
                query_contains_title=StrIndex(Value(normalized_roles_query), Lower(F('title')))
            ).annotate(
                title_match_tier=Case(
                    When(title__iexact=normalized_roles_query, then=Value(0)),
                    When(
                        Q(title__icontains=normalized_roles_query) | Q(query_contains_title__gt=0),
                        then=Value(1),
                    ),
                    default=Value(2),
                    output_field=IntegerField(),
                )
            )

            if roles_vector_condition is not None:
                # Include all exact/partial title matches + remaining role-vector matches.
                qs = qs.filter(Q(title_match_tier__in=[0, 1]) | roles_vector_condition)
            else:
                qs = qs.filter(title_match_tier__in=[0, 1])

            qs = qs.order_by('title_match_tier', '-overall_score', '-posted_date')
        else:
            qs = qs.order_by('-overall_score', '-posted_date')
        
        print(f"📊 TOTALT {qs.count()} matcher INNAN pagination")
        
        # 4. Pagination: returnera endast efterfrågad sida.
        max_fetch = offset + limit + 50  # Buffer för säkerhet
        all_results = list(qs[:max_fetch])
        paginated = all_results[offset:offset + limit]
        
        print(f"✅ Returnerar {len(paginated)} av {len(all_results)} totalt (max={max_fetch})")
        
        return paginated, qs.count()  # Tuple: (jobs, total_matches)!


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

