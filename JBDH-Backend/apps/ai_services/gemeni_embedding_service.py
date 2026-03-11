# apps/ai_services/embedding_service.py
"""
Gemini embeddings + skill comparison + profession score.
Mirrors .NET EmbeddingService (CompareSkillsWithEmbeddingsAsync, CalculateProfessionScoreWithEmbeddingsAsync).
"""
import logging
import math
from typing import List, Tuple, Any, Optional

import requests

from apps.ai_services.ai_config import AIConfig
from apps.ai_services.ai_models import RequirementItem, Profession

logger = logging.getLogger(__name__)

# In-memory cache to reduce API calls
_embedding_cache: dict[str, list[float]] = {}


def _get_gemini_embedding(text: str, task_type: str = "RETRIEVAL_DOCUMENT") -> list[float]:
    if not text or not text.strip():
        return []
    url = AIConfig.GEMINI_EMBEDDING_URL
    if not url:
        raise ValueError("GEMINI_EMBEDDING_URL is not set")
    key = AIConfig.GEMINI_API_KEY
    if not key:
        raise ValueError("GEMINI_API_KEY is not set")
    # Google API key often via query param
    if "?" in url:
        full_url = f"{url}&key={key}"
    else:
        full_url = f"{url}?key={key}"
    payload = {
        "model": "models/gemini-embedding-001",
        "content": {"parts": [{"text": text}]},
        "taskType": task_type,
    }
    resp = requests.post(full_url, json=payload, timeout=30)
    if not resp.ok:
        logger.error("Gemini API error: %s - %s", resp.status_code, resp.text)
        raise RuntimeError(f"Gemini API returned {resp.status_code}: {resp.text}")
    data = resp.json()
    emb = data.get("embedding") or {}
    values = emb.get("values") if isinstance(emb, dict) else None
    if not values:
        raise RuntimeError("No embedding values in Gemini response")
    return list(values)


def get_embedding(text: str, task_type: str = "RETRIEVAL_DOCUMENT") -> list[float]:
    cache_key = f"{text}|{task_type}"
    if cache_key in _embedding_cache:
        return _embedding_cache[cache_key]
    try:
        vec = _get_gemini_embedding(text, task_type)
        _embedding_cache[cache_key] = vec
        return vec
    except Exception as e:
        if "429" in str(e) or "quota" in str(e).lower() or "limit" in str(e).lower():
            logger.warning("Gemini quota exceeded, using fallback embedding")
            return _fallback_embedding(text)
        raise


def _fallback_embedding(text: str, dim: int = 768) -> list[float]:
    """Simple deterministic pseudo-embedding when API quota exceeded."""
    import hashlib
    h = int(hashlib.sha256(text.encode()).hexdigest()[:16], 16)
    seed = h % (2**32)
    r = __import__("random").Random(seed)
    vec = [r.uniform(-1, 1) for _ in range(dim)]
    mag = math.sqrt(sum(x * x for x in vec))
    if mag > 0:
        vec = [x / mag for x in vec]
    return vec


def get_embeddings_batch(texts: List[str], task_type: str = "RETRIEVAL_DOCUMENT") -> List[list[float]]:
    return [get_embedding(t, task_type) for t in texts]


def cosine_similarity(a: list[float], b: list[float]) -> float:
    if not a or not b or len(a) != len(b):
        return 0.0
    dot = sum(x * y for x, y in zip(a, b))
    ma = math.sqrt(sum(x * x for x in a))
    mb = math.sqrt(sum(x * x for x in b))
    if ma == 0 or mb == 0:
        return 0.0
    return dot / (ma * mb)


def compare_skills_with_embeddings(
    cv_requirements: List[RequirementItem],
    job_requirements: List[RequirementItem],
) -> Tuple[float, List[Any]]:
    """
    Returns (skills_match_percentage, list of match detail dicts).
    """
    if not job_requirements:
        return 0.0, []

    cv_texts = [r.name or "" for r in cv_requirements]
    job_texts = [r.name or "" for r in job_requirements]
    cv_vectors = get_embeddings_batch(cv_texts, "RETRIEVAL_DOCUMENT") if cv_texts else []
    job_vectors = get_embeddings_batch(job_texts, "RETRIEVAL_QUERY")

    results = []
    weighted_sum = 0.0
    importance_sum = 0.0

    for i, req in enumerate(job_requirements):
        best_sim = 0.0
        matched_cv_skill = None
        matched_cv_importance = 0
        for j, cv_vec in enumerate(cv_vectors):
            sim = cosine_similarity(cv_vec, job_vectors[i])
            if sim > best_sim:
                best_sim = sim
                matched_cv_skill = cv_requirements[j].name or "No match"
                matched_cv_importance = cv_requirements[j].importance
        if matched_cv_skill is None:
            matched_cv_skill = "No match"
        weighted = best_sim * req.importance
        weighted_sum += weighted
        importance_sum += req.importance
        results.append({
            "requirement": req.name,
            "category": req.category,
            "importance": req.importance,
            "matchedCvSkill": matched_cv_skill,
            "cvSkillLevel": matched_cv_importance,
            "vectorSimilarity": round(best_sim, 4),
            "keywordScore": 0.0,
            "finalSimilarity": round(best_sim, 4),
            "weightedScore": round(weighted, 4),
        })

    match_pct = (weighted_sum / importance_sum * 100) if importance_sum > 0 else 0.0
    match_pct = round(match_pct, 2)
    return match_pct, results


def calculate_profession_score_with_embeddings(
    cv_profession: Optional[Profession],
    job_profession: Optional[Profession],
) -> float:
    if not cv_profession or not job_profession:
        return 0.05

    main_score = 0.0
    sub_score = 0.0
    specialty_score = 0.0

    cv_main = (cv_profession.main or "").strip()
    job_main = (job_profession.main or "").strip()
    if cv_main and job_main:
        if cv_main.lower() == job_main.lower():
            main_score = 1.0
        else:
            cv_main_vec = get_embedding(cv_main, "RETRIEVAL_DOCUMENT")
            job_main_vec = get_embedding(job_main, "RETRIEVAL_QUERY")
            main_score = cosine_similarity(cv_main_vec, job_main_vec)

    cv_sub = (cv_profession.sub or "").strip()
    job_sub = (job_profession.sub or "").strip()
    if cv_sub and job_sub:
        if cv_sub.lower() == job_sub.lower():
            sub_score = 1.0
        else:
            cv_sub_vec = get_embedding(cv_sub, "RETRIEVAL_DOCUMENT")
            job_sub_vec = get_embedding(job_sub, "RETRIEVAL_QUERY")
            sub_score = cosine_similarity(cv_sub_vec, job_sub_vec)
    else:
        sub_score = main_score

    cv_spec = (cv_profession.specialty or "").strip()
    job_spec = (job_profession.specialty or "").strip()
    if cv_spec and job_spec:
        if cv_spec.lower() == job_spec.lower():
            specialty_score = 1.0
        else:
            cv_spec_vec = get_embedding(cv_spec, "RETRIEVAL_DOCUMENT")
            job_spec_vec = get_embedding(job_spec, "RETRIEVAL_QUERY")
            specialty_score = cosine_similarity(cv_spec_vec, job_spec_vec)
    else:
        specialty_score = sub_score

    total = main_score * 0.5 + sub_score * 0.3 + specialty_score * 0.2
    domain_bonus = _get_profession_domain_bonus(cv_main, job_main)
    adjusted = max(0.0, min(1.0, total + domain_bonus))
    return adjusted


def _get_profession_domain_bonus(cv_main: Optional[str], job_main: Optional[str]) -> float:
    if not cv_main or not job_main:
        return 0.0
    cv = cv_main.lower()
    job = job_main.lower()
    tech = ["developer", "engineer", "programmer", "coder", "architect", "it", "software", "system"]
    medical = ["doctor", "physician", "nurse", "medical", "healthcare", "clinical", "läkare", "sjuksköterska", "medicin", "vård"]
    finance = ["finance", "economy", "accountant", "controller", "ekonomi", "ekonom", "bokföring", "revisor", "bank"]
    education = ["teacher", "pedagogue", "lärare", "utbildning", "skola", "pedagog"]
    is_cv_tech = any(t in cv for t in tech)
    is_job_tech = any(t in job for t in tech)
    is_cv_med = any(m in cv for m in medical)
    is_job_med = any(m in job for m in medical)
    is_cv_fin = any(f in cv for f in finance)
    is_job_fin = any(f in job for f in finance)
    is_cv_edu = any(e in cv for e in education)
    is_job_edu = any(e in job for e in education)
    if is_cv_tech and is_job_tech:
        return 0.1
    if is_cv_med and is_job_med:
        return 0.15
    if is_cv_fin and is_job_fin:
        return 0.1
    if is_cv_edu and is_job_edu:
        return 0.1
    if (is_cv_tech and is_job_med) or (is_cv_med and is_job_tech):
        return -0.4
    if (is_cv_tech and (is_job_fin or is_job_edu)) or (is_cv_fin and is_job_tech) or (is_cv_edu and is_job_tech):
        return -0.2
    if (is_cv_med and (is_job_fin or is_job_edu)) or (is_cv_fin and is_job_med) or (is_cv_edu and is_job_med):
        return -0.2
    if (is_cv_fin and is_job_edu) or (is_cv_edu and is_job_fin):
        return -0.1
    return 0.0
