"""
ATS Scorer – produces a 0–100 ATS score with per-category breakdown.

Base weights:
  skills_match      75 %  (base; +5 % bonus when > 50 % and education not required)
  experience_match  15 %  (base; +5 % bonus when > 50 % and education not required)
  soft_skills_match 10 %
  education_match   10 %  (only included when job explicitly requires education)

keyword_coverage is NOT a weighted component – it is baked into skills_raw as a
fallback when the skills DB has sparse coverage, and shown in breakdown for debugging.

When a component has no requirement in the job ad it is excluded from the
weighted average (weight redistributed to remaining components via normalisation).
"""

from __future__ import annotations

import re
from typing import Dict, List, Optional, Set, Tuple

from ats_service.skills_db import CANONICAL_DISPLAY

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
#
# Weights are dynamic: education_match is only included when the job ad
# specifies education requirements (weight redistributed automatically).
# keyword_coverage is NOT a weighted component – it is used as a fallback
# signal inside skills_raw and shown in breakdown for debugging only.
# ---------------------------------------------------------------------------
_W_SKILLS      = 0.75   # base; +0.05 bonus when skills_raw > 0.5 and no edu required
_W_EXPERIENCE  = 0.15   # base; +0.05 bonus when exp_raw   > 0.5 and no edu required
_W_SOFT_SKILLS = 0.10
_W_EDUCATION   = 0.10   # only added when job explicitly requires education

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
    ratio = cv_years / required_years
    return round(ratio ** 0.5, 4)  # concave curve: softer penalty for near-misses


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
        hint_words = [w for w in re.split(r'\W+', hint.lower()) if len(w) > 3]
        if not hint_words:
            return False
        if any(w in cv_text for w in hint_words):
            return True
        for _canonical, synonyms in _DEGREE_SYNONYMS.items():
            if any(w in synonyms for w in hint_words):
                return any(syn in cv_text for syn in synonyms)
        return False

    matches = sum(1 for hint in job_education if _hint_matches(hint))
    return min(1.0, matches / len(job_education))


_KEYWORD_STOP = {
    # Swedish 5+ char function words
    "eller", "genom", "kommer", "också", "detta", "dessa", "vilka",
    "deras", "bland", "under", "efter", "innan", "varje", "alltid",
    "kunna", "minst",
    # Swedish 4-char function words (needed after threshold lowered to {4,})
    "från", "till", "även", "alla", "hela", "vara", "utan", "inga",
    "dock", "samt", "dels", "inom", "ovan", "just", "goda", "dina",
    "sina", "ditt", "sitt", "vars", "hade", "bara", "alls", "enda",
    "vart", "åren", "kvar", "slut", "igen", "resp",
    # English 5+ char function words
    "where", "which", "their", "there", "about", "would", "should",
    "could", "might", "shall", "years", "have", "will", "within",
    "least", "other", "experience",
    # English 4-char function words
    "that", "this", "with", "from", "also", "both", "some", "when",
    "then", "than", "they", "them", "what", "your", "more", "over",
    "been", "were", "does", "each", "only", "same", "such", "into",
    "upon", "have", "your", "very", "just",
}


def _soft_skill_text_fallback(job_soft: Set[str], cv_text: str) -> float:
    """
    Word-level fallback for soft skill matching when spaCy detected zero
    soft skills in the CV (common with PDF concatenation artifacts).

    Splits each job soft skill phrase into words (≥4 chars) and checks
    whether any appear anywhere in the raw CV text (substring search).
    Returns 0–1.
    """
    if not cv_text:
        return 0.0
    cv_lower = cv_text.lower()
    hits = 0
    for skill in job_soft:
        words = [w for w in re.split(r'\W+', skill.lower()) if len(w) >= 4]
        if words and any(w in cv_lower for w in words):
            hits += 1
    return hits / len(job_soft)


def _keyword_coverage(job_text: str, cv_text: str) -> Optional[float]:
    """
    Fallback signal when the skills DB has sparse or zero coverage for a domain.

    Extracts important words from the job text (alpha, ≥5 chars, not stopwords)
    and measures what fraction appear anywhere in the CV text.

    Returns 0–1, or None if job_text is empty / yields no tokens.
    """
    if not job_text:
        return None
    tokens = re.findall(r"[a-zA-ZåäöÅÄÖ]{4,}", job_text)
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
    soft_raw: Optional[float]
    if job_soft:
        if cv_soft:
            soft_raw = _overlap_score(cv_soft, job_soft)
            if soft_raw == -1.0:
                soft_raw = None
        elif cv_text:
            # CV has zero detected soft skills – common PDF concatenation artifact.
            # Fall back to word-level search in raw CV text.
            soft_raw = _soft_skill_text_fallback(job_soft, cv_text)
        else:
            soft_raw = None  # no CV text – exclude rather than penalise with 0
    else:
        soft_raw = None

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
        # Sparse tech DB coverage: for non-tech roles (product owner, analyst etc.)
        # the job ad contains few programming/framework keywords. Blend more
        # aggressively toward keyword_coverage so the score reflects domain overlap.
        skills_raw = round(0.4 * db_skills_raw + 0.6 * kw_raw, 4)
    else:
        skills_raw = db_skills_raw  # DB has good coverage – trust it

    # --- Weighted average over only the components the job actually specifies ---
    # skills + experience + soft_skills = 1.0 base.
    # education adds 0.10 on top when required; total_weight normalises.
    # keyword_coverage stays as debug/fallback only – not a weighted component.
    #
    # When education is NOT required: the freed 0.10 is redistributed:
    #   +0.05 to skills     if skills_raw   > 0.5
    #   +0.05 to experience if experience_raw > 0.5 (and experience is present)
    # Any unallocated portion stays unspent (normalisation handles the rest).
    edu_required = education_raw is not None
    w_skills = _W_SKILLS
    w_experience = _W_EXPERIENCE

    if not edu_required:
        if skills_raw > 0.5:
            w_skills += 0.05
        if experience_raw is not None and experience_raw > 0.5:
            w_experience += 0.05

    components: Dict[str, tuple] = {"skills": (skills_raw, w_skills)}
    if experience_raw is not None:
        components["experience"] = (experience_raw, w_experience)
    if education_raw is not None:
        components["education"] = (education_raw, _W_EDUCATION)
    if soft_raw is not None:
        components["soft_skills"] = (soft_raw, _W_SOFT_SKILLS)

    total_weight = sum(w for _, w in components.values())
    total = sum(raw * w for raw, w in components.values()) / total_weight
    total_pct = round(total * 100, 1)

    # --- Matched / missing / bonus ---
    # matched = tech skills that appear in BOTH cv and job.
    # Domain/certification overlaps are also included because they are
    # meaningful for job matching. Soft and language skills are excluded
    # to avoid noisy output (e.g. "svenska", "noggrannhet" in matched list).
    _DOMAIN_CATS = {"domain", "certification"}
    cv_domain: Set[str] = {
        s.lower() for cat in _DOMAIN_CATS for s in cv_by_cat.get(cat, [])
    }
    job_domain: Set[str] = {
        s.lower() for cat in _DOMAIN_CATS for s in job_by_cat.get(cat, [])
    }
    matched_lower: Set[str] = (cv_tech & job_tech) | (cv_domain & job_domain)

    def _display(s: str) -> str:
        return CANONICAL_DISPLAY.get(s.lower(), s)

    matched: List[str] = sorted(_display(s) for s in matched_lower)

    # missing = required job skills not found by PhraseMatcher in the CV.
    # Exception: soft skills – if the raw CV text contains the word(s) we
    # don't mark it missing, because PhraseMatcher often fails on PDF-extracted
    # Swedish text with ligatures or unusual unicode (e.g. "självständigt").
    _cv_text_lower = cv_text.lower() if cv_text else ""
    _soft_cat: Set[str] = {s.lower() for s in job_by_cat.get("soft", [])}

    def _missed_by_text_fallback(skill_lower: str) -> bool:
        """True if the skill is a soft skill found via raw-text word search."""
        if skill_lower not in _soft_cat:
            return False
        words = [w for w in re.split(r'\W+', skill_lower) if len(w) >= 4]
        return bool(words) and any(w in _cv_text_lower for w in words)

    missing: List[str] = sorted(
        _display(s)
        for s in job_required_lower
        if s not in cv_all_lower and not _missed_by_text_fallback(s)
    )
    # bonus = tech/domain CV skills not mentioned in the job ad at all
    bonus: List[str] = sorted(
        _display(s) for s in (cv_tech | cv_domain) if s not in job_all_db
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
