"""
ATS Scorer – produces a 0–100 ATS score with per-category breakdown.

Weights (tunable via environment variables later):
  skills_match      40 %
  experience_match  25 %
  education_match   20 %
  soft_skills_match 15 %

When a component has no requirement in the job ad it is excluded from the
weighted average (its weight is redistributed to the remaining components).
This prevents "100 % auto-pass" for unspecified dimensions.
"""

from __future__ import annotations

import re
from typing import Dict, List, Optional, Set, Tuple

try:
    from langdetect import detect as _langdetect, LangDetectException as _LDE

    def _detect_lang(text: str) -> str:
        try:
            return _langdetect(text)
        except _LDE:
            return "en"
except ImportError:
    def _detect_lang(text: str) -> str:  # type: ignore[misc]
        return "unknown"


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
    Returns 0–1, or -1.0 as a sentinel when job_skills is empty
    (meaning spaCy detected no skills at all – data/parsing issue).
    """
    if not job_skills:
        return -1.0  # sentinel: "INSPECTION NEEDED" – not a real score
    return len(cv_skills & job_skills) / len(job_skills)


def _experience_score(cv_years: int, required_years: int) -> Optional[float]:
    """Returns 0–1, or None when the job ad specifies no experience requirement."""
    if required_years <= 0:
        return None  # not specified – excluded from weighted average
    if cv_years >= required_years:
        return 1.0
    return round(cv_years / required_years, 4)


_DEGREE_SYNONYMS: dict = {
    "bachelor": ["bachelor", "bsc", "b.sc", "kandidat", "hogskole"],
    "master": ["master", "msc", "m.sc", "magister", "civilingenjor"],
    "phd": ["phd", "doctorate", "doktor", "fil.dr"],
}


def _education_score(cv_education: List[str], job_education: List[str]) -> Optional[float]:
    """
    Keyword-overlap between CV education lines and job education hints,
    with synonym expansion for common degree terms.
    Returns 0–1, or None when the job ad specifies no education requirement.
    """
    if not job_education:
        return None  # not specified – excluded from weighted average
    if not cv_education:
        return 0.0

    cv_text = " ".join(cv_education).lower()

    def _hint_matches(hint: str) -> bool:
        hint_lower = hint.lower()
        if hint_lower in cv_text:
            return True
        for _canonical, synonyms in _DEGREE_SYNONYMS.items():
            if hint_lower in synonyms:
                return any(syn in cv_text for syn in synonyms)
        return False

    matches = sum(1 for hint in job_education if _hint_matches(hint))
    return min(1.0, matches / len(job_education))


_KEYWORD_STOP = {
    # Swedish
    "eller", "genom", "kommer", "också", "detta", "dessa", "vilka",
    "deras", "bland", "under", "efter", "innan", "varje", "alltid",
    "kunna", "minst",
    # English
    "where", "which", "their", "there", "about", "would", "should",
    "could", "might", "shall", "years", "have", "will", "within",
    "least", "other", "experience",
}


def _keyword_coverage(job_text: str, cv_text: str) -> Optional[float]:
    """
    Fallback signal when the skills DB has sparse or zero coverage for a domain.

    Extracts important words from the job text (alpha, ≥5 chars, not stopwords)
    and measures what fraction appear anywhere in the CV text.

    Returns 0–1, or None if job_text is empty / yields no tokens.
    """
    if not job_text:
        return None
    tokens = re.findall(r"[a-zA-ZåäöÅÄÖ]{5,}", job_text)
    important = [t.lower() for t in tokens if t.lower() not in _KEYWORD_STOP]
    if not important:
        return None
    cv_lower = cv_text.lower()
    hits = sum(1 for w in important if w in cv_lower)
    return round(hits / len(important), 4)


def score(
    cv_parsed: Dict,
    job_parsed: Dict,
    cv_text: str = "",
    job_text: str = "",
) -> Dict:
    """
    Compute ATS score between a parsed CV and a parsed job ad.

    Args:
        cv_parsed:  output of parser.parse_cv()
        job_parsed: output of parser.parse_job()
        cv_text:    original CV text (used for keyword fallback + debug)
        job_text:   original job-ad text (used for keyword fallback + debug)

    Returns a dict with:
        total_score         (0–100 float)
        breakdown           dict of component scores (0–100, null = not specified)
        keyword_coverage    raw keyword overlap (0–100, null = texts not provided)
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
    job_all_db: Set[str] = {s.lower() for s in job_parsed.get("all_skills", [])}

    # --- Non-skills components (computed first, needed even inside early exits) ---
    experience_raw: Optional[float] = _experience_score(
        cv_parsed.get("experience_years", 0),
        job_parsed.get("experience_required", 0),
    )
    education_raw: Optional[float] = _education_score(
        cv_parsed.get("education", []),
        job_parsed.get("education_hints", []),
    )
    soft_raw: Optional[float] = _overlap_score(cv_soft, job_soft) if job_soft else None
    if soft_raw == -1.0:
        soft_raw = None  # job specified no soft skills → exclude

    # --- Skills score: PhraseMatcher DB + keyword-coverage fallback ---
    #
    # Blending strategy:
    #   DB finds 0 tech skills  → widen to ALL DB categories
    #   Still 0 across ALL DB   → use keyword fallback 100 %
    #   DB finds 1-2 tech skills → blend: 60 % DB + 40 % keyword (sparse coverage)
    #   DB finds 3+ tech skills  → trust DB, no blending
    kw_raw: Optional[float] = (
        _keyword_coverage(job_text, cv_text) if (job_text and cv_text) else None
    )
    db_skills_raw = _overlap_score(cv_tech, job_tech)

    if db_skills_raw == -1.0:
        # Tech-only set empty → widen to ALL categories the DB found in the job
        db_skills_raw = _overlap_score(cv_all_lower, job_all_db)

    if db_skills_raw == -1.0:  # DB found zero skills across ALL categories
        if kw_raw is None:
            # Job text itself is empty – genuinely nothing to parse.
            return {
                "total_score": 0.0,
                "flag": "NO_JOB_SKILLS_DETECTED",
                "debug": {
                    "message": "spaCy hittade inga skills – kontrollera jobbtext/parsing",
                    "job_text_preview": job_text[:100],
                    "spacy_model": _detect_lang(job_text) if job_text else "unknown",
                    "recommendations": [
                        "1. Lägg till riktig jobbannons",
                        "2. Uppgradera spaCy patterns för skills",
                        "3. Manuell skill-input i DB",
                    ],
                },
                "partial_score": {
                    "experience": round(experience_raw * 100, 1) if experience_raw is not None else None,
                    "education": round(education_raw * 100, 1) if education_raw is not None else None,
                },
            }
        skills_raw: float = kw_raw  # keyword fallback is the only signal
    elif len(job_tech) <= 2 and kw_raw is not None:
        # Sparse DB coverage: blend to reduce over-reliance on 1-2 matched terms
        skills_raw = round(0.6 * db_skills_raw + 0.4 * kw_raw, 4)
    else:
        skills_raw = db_skills_raw  # DB has good coverage – trust it

    # --- Weighted average over only the components the job actually specifies ---
    components: Dict[str, tuple] = {"skills": (skills_raw, WEIGHTS["skills"])}
    if experience_raw is not None:
        components["experience"] = (experience_raw, WEIGHTS["experience"])
    if education_raw is not None:
        components["education"] = (education_raw, WEIGHTS["education"])
    if soft_raw is not None:
        components["soft_skills"] = (soft_raw, WEIGHTS["soft_skills"])

    total_weight = sum(w for _, w in components.values())
    total = sum(raw * w for raw, w in components.values()) / total_weight
    total_pct = round(total * 100, 1)

    # --- Matched / missing / bonus ---
    matched: List[str] = sorted(
        s for s in job_required_lower if s in cv_all_lower
    )
    missing: List[str] = sorted(
        s for s in job_required_lower if s not in cv_all_lower
    )
    bonus: List[str] = sorted(
        s for s in cv_all_lower if s not in job_all_db
    )

    # --- Recommendations ---
    recommendations: List[str] = []
    if missing:
        top_missing = missing[:5]
        recommendations.append(
            f"Lägg till dessa saknade kompetenser i ditt CV: {', '.join(top_missing)}"
        )
    if experience_raw is not None and experience_raw < 0.8:
        req = job_parsed.get("experience_required", 0)
        got = cv_parsed.get("experience_years", 0)
        recommendations.append(
            f"Jobbet kräver ~{req} år erfarenhet – ditt CV visar ~{got} år. "
            "Lyft fram fler tidsstämplade erfarenheter."
        )
    if education_raw is not None and education_raw < 0.5 and job_parsed.get("education_hints"):
        recommendations.append(
            "Utbildningskraven är inte tydligt matchade i ditt CV – visa relevanta examina."
        )
    if soft_raw is not None and soft_raw < 0.5 and job_soft:
        top_soft = list(job_soft)[:3]
        recommendations.append(
            f"Jobbet nämner dessa mjuka kompetenser – lägg till dem om du har dem: "
            f"{', '.join(top_soft)}"
        )
    if skills_raw < 0.2 and kw_raw is not None and kw_raw < 0.15:
        recommendations.append(
            "Varning: låg domänmatchning – CVt verkar inte matcha jobbets bransch/teknikstack."
        )

    return {
        "total_score": total_pct,
        "breakdown": {
            "skills_match": round(skills_raw * 100, 1),
            "keyword_coverage": round(kw_raw * 100, 1) if kw_raw is not None else None,
            "experience_match": round(experience_raw * 100, 1) if experience_raw is not None else None,
            "education_match": round(education_raw * 100, 1) if education_raw is not None else None,
            "soft_skills_match": round(soft_raw * 100, 1) if soft_raw is not None else None,
        },
        "matched_skills": matched,
        "missing_skills": missing,
        "bonus_skills": bonus[:10],
        "recommendations": recommendations,
    }
