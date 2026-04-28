"""
ATS Scorer – produces a 0–100 ATS score with per-category breakdown.

Weights (tunable via environment variables later):
  skills_match      40 %
  experience_match  25 %
  education_match   20 %
  soft_skills_match 15 %
"""

from __future__ import annotations

from typing import Dict, List, Set, Tuple


# ---------------------------------------------------------------------------
# Weight configuration
# ---------------------------------------------------------------------------
WEIGHTS = {
    "skills": 0.40,
    "experience": 0.25,
    "education": 0.20,
    "soft_skills": 0.15,
}

_TECH_CATEGORIES = {"programming", "framework", "database", "cloud_devops", "tools"}


def _jaccard(set_a: Set[str], set_b: Set[str]) -> float:
    """Jaccard similarity between two sets of lowercase strings."""
    if not set_a and not set_b:
        return 1.0
    union = set_a | set_b
    if not union:
        return 0.0
    return len(set_a & set_b) / len(union)


def _overlap_score(cv_skills: Set[str], job_skills: Set[str]) -> float:
    """
    What fraction of job_skills is covered by cv_skills?
    Returns 0–1.
    """
    if not job_skills:
        return 1.0  # job has no requirements → automatic pass
    return len(cv_skills & job_skills) / len(job_skills)


def _experience_score(cv_years: int, required_years: int) -> float:
    """Returns 0–1 based on how well cv_years meets required_years."""
    if required_years <= 0:
        return 1.0
    if cv_years >= required_years:
        return 1.0
    return round(cv_years / required_years, 4)


_DEGREE_SYNONYMS: dict = {
    "bachelor": ["bachelor", "bsc", "b.sc", "kandidat", "hogskole"],
    "master": ["master", "msc", "m.sc", "magister", "civilingenjor"],
    "phd": ["phd", "doctorate", "doktor", "fil.dr"],
}


def _education_score(cv_education: List[str], job_education: List[str]) -> float:
    """
    Keyword-overlap between CV education lines and job education hints,
    with synonym expansion for common degree terms.
    Returns 0–1.
    """
    if not job_education:
        return 1.0  # not required
    if not cv_education:
        return 0.0

    cv_text = " ".join(cv_education).lower()

    def _hint_matches(hint: str) -> bool:
        hint_lower = hint.lower()
        if hint_lower in cv_text:
            return True
        # Try synonym expansion
        for _canonical, synonyms in _DEGREE_SYNONYMS.items():
            if hint_lower in synonyms:
                return any(syn in cv_text for syn in synonyms)
        return False

    matches = sum(1 for hint in job_education if _hint_matches(hint))
    return min(1.0, matches / len(job_education))


def score(
    cv_parsed: Dict,
    job_parsed: Dict,
) -> Dict:
    """
    Compute ATS score between a parsed CV and a parsed job ad.

    Args:
        cv_parsed:  output of parser.parse_cv()
        job_parsed: output of parser.parse_job()

    Returns a dict with:
        total_score         (0–100 float)
        breakdown           dict of component scores (0–100)
        matched_skills      list of skills in both CV and job
        missing_skills      list of required job skills not in CV
        bonus_skills        CV skills not required but present
        recommendations     list of actionable strings
    """
    # --- Build skill sets (lowercase for comparison) ---
    cv_by_cat: Dict[str, List[str]] = cv_parsed.get("skills_by_category", {})
    job_by_cat: Dict[str, List[str]] = job_parsed.get("skills_by_category", {})

    cv_tech: Set[str] = {
        s.lower()
        for cat in _TECH_CATEGORIES
        for s in cv_by_cat.get(cat, [])
    }
    job_tech: Set[str] = {
        s.lower()
        for cat in _TECH_CATEGORIES
        for s in job_by_cat.get(cat, [])
    }

    cv_soft: Set[str] = {s.lower() for s in cv_by_cat.get("soft", [])}
    job_soft: Set[str] = {s.lower() for s in job_by_cat.get("soft", [])}

    # Required skills from job (all tech skills if no section split)
    job_required_lower: Set[str] = {s.lower() for s in job_parsed.get("required_skills", [])}
    if not job_required_lower:
        job_required_lower = job_tech

    cv_all_lower: Set[str] = {s.lower() for s in cv_parsed.get("all_skills", [])}

    # --- Component scores ---
    skills_raw = _overlap_score(cv_tech, job_tech)
    experience_raw = _experience_score(
        cv_parsed.get("experience_years", 0),
        job_parsed.get("experience_required", 0),
    )
    education_raw = _education_score(
        cv_parsed.get("education", []),
        job_parsed.get("education_hints", []),
    )
    soft_raw = _overlap_score(cv_soft, job_soft) if job_soft else 1.0

    # --- Weighted total ---
    total = (
        skills_raw * WEIGHTS["skills"]
        + experience_raw * WEIGHTS["experience"]
        + education_raw * WEIGHTS["education"]
        + soft_raw * WEIGHTS["soft_skills"]
    )
    total_pct = round(total * 100, 1)

    # --- Matched / missing / bonus ---
    matched: List[str] = sorted(
        s for s in job_required_lower if s in cv_all_lower
    )
    missing: List[str] = sorted(
        s for s in job_required_lower if s not in cv_all_lower
    )
    bonus: List[str] = sorted(
        s for s in cv_all_lower if s not in {s2 for s2 in job_parsed.get("all_skills", [])}
    )

    # --- Recommendations ---
    recommendations: List[str] = []
    if missing:
        top_missing = missing[:5]
        recommendations.append(
            f"Lägg till dessa saknade kompetenser i ditt CV: {', '.join(top_missing)}"
        )
    if experience_raw < 0.8:
        req = job_parsed.get("experience_required", 0)
        got = cv_parsed.get("experience_years", 0)
        recommendations.append(
            f"Jobbet kräver ~{req} år erfarenhet – ditt CV visar ~{got} år. "
            "Lyft fram fler tidsstämplade erfarenheter."
        )
    if education_raw < 0.5 and job_parsed.get("education_hints"):
        recommendations.append(
            "Utbildningskraven är inte tydligt matchade i ditt CV – visa relevanta examina."
        )
    if soft_raw < 0.5 and job_soft:
        top_soft = list(job_soft)[:3]
        recommendations.append(
            f"Jobbet nämner dessa mjuka kompetenser – lägg till dem om du har dem: "
            f"{', '.join(top_soft)}"
        )

    return {
        "total_score": total_pct,
        "breakdown": {
            "skills_match": round(skills_raw * 100, 1),
            "experience_match": round(experience_raw * 100, 1),
            "education_match": round(education_raw * 100, 1),
            "soft_skills_match": round(soft_raw * 100, 1),
        },
        "matched_skills": matched,
        "missing_skills": missing,
        "bonus_skills": bonus[:10],
        "recommendations": recommendations,
    }
