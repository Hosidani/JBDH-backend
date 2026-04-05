from typing import Optional
from apps.ai_services.domain.ai_models import Profession
import math
from typing import List, Tuple, Any
from apps.ai_services.infrastructure.ai_config import AIConfig
from apps.ai_services.domain.ai_models import RequirementItem


def calculate_profession_score_with_embeddings_minilm(
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
            cv_main_vec = vectorize_text(cv_main)
            job_main_vec = vectorize_text(job_main)
            main_score = cosine_similarity(cv_main_vec, job_main_vec)

    cv_sub = (cv_profession.sub or "").strip()
    job_sub = (job_profession.sub or "").strip()
    if cv_sub and job_sub:
        if cv_sub.lower() == job_sub.lower():
            sub_score = 1.0
        else:
            cv_sub_vec = vectorize_text(cv_sub)
            job_sub_vec = vectorize_text(job_sub)
            sub_score = cosine_similarity(cv_sub_vec, job_sub_vec)
    else:
        sub_score = main_score

    cv_spec = (cv_profession.specialty or "").strip()
    job_spec = (job_profession.specialty or "").strip()
    if cv_spec and job_spec:
        if cv_spec.lower() == job_spec.lower():
            specialty_score = 1.0
        else:
            cv_spec_vec = vectorize_text(cv_spec)
            job_spec_vec = vectorize_text(job_spec)
            specialty_score = cosine_similarity(cv_spec_vec, job_spec_vec)
    else:
        specialty_score = sub_score

    total = main_score * 0.5 + sub_score * 0.3 + specialty_score * 0.2
    domain_bonus = _get_profession_domain_bonus_minilm(cv_main, job_main)
    adjusted = max(0.0, min(1.0, total + domain_bonus))
    return adjusted

def _get_profession_domain_bonus_minilm(cv_main: Optional[str], job_main: Optional[str]) -> float:
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
from core.utils import vectorize_text
def cosine_similarity(a: list[float], b: list[float]) -> float:
    if not a or not b or len(a) != len(b):
        return 0.0
    dot = sum(x * y for x, y in zip(a, b))
    ma = math.sqrt(sum(x * x for x in a))
    mb = math.sqrt(sum(x * x for x in b))
    if ma == 0 or mb == 0:
        return 0.0
    return dot / (ma * mb)

def compare_skills_with_embeddings_minilm(
    cv_requirements: List[RequirementItem],
    job_requirements: List[RequirementItem],
) -> Tuple[float, List[Any]]:
    """
    Jämför skills med MiniLM-embeddings via vectorize_text/embed_texts.
    Returnerar (skills_match_percentage, lista med match-detaljer).
    """
    if not job_requirements:
        return 0.0, []

    cv_texts = [r.name or "" for r in cv_requirements]
    job_texts = [r.name or "" for r in job_requirements]
    cv_vectors = [vectorize_text(t) for t in cv_texts] if cv_texts else []
    job_vectors = [vectorize_text(t) for t in job_texts]

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