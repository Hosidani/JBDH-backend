"""
ATS Parser – extracts structured information from CV and job-ad text.

Uses spaCy PhraseMatcher against the skills_db vocabulary so no large
model is required – en_core_web_sm (12 MB) is sufficient.
"""

from __future__ import annotations

import re
from typing import Dict, List, Set

import spacy
from langdetect import detect, LangDetectException
from spacy.matcher import PhraseMatcher

from ats_service.skills_db import CATEGORY_MAP, get_all_skills

# ---------------------------------------------------------------------------
# Load English and Swedish spaCy models once at module level.
# Fallback to blank models if downloads are missing so the container still
# starts and returns a useful error via /health.
# ---------------------------------------------------------------------------
try:
    _nlp_en = spacy.load("en_core_web_sm")
except OSError:
    _nlp_en = spacy.blank("en")

try:
    _nlp_sv = spacy.load("sv_core_news_sm")
except OSError:
    _nlp_sv = spacy.blank("sv")

# ---------------------------------------------------------------------------
# Build a PhraseMatcher for each model (done once at import time).
# ---------------------------------------------------------------------------
_all_skills = get_all_skills()

_matcher_en = PhraseMatcher(_nlp_en.vocab, attr="LOWER")
_matcher_en.add("SKILL", list(_nlp_en.pipe(_all_skills)))

_matcher_sv = PhraseMatcher(_nlp_sv.vocab, attr="LOWER")
_matcher_sv.add("SKILL", list(_nlp_sv.pipe(_all_skills)))


def _get_nlp_and_matcher(text: str):
    """Detect language and return the matching (nlp, matcher) pair."""
    try:
        lang = detect(text)
    except LangDetectException:
        lang = "en"
    if lang == "sv":
        return _nlp_sv, _matcher_sv
    return _nlp_en, _matcher_en


# ---------------------------------------------------------------------------
# Regex helpers
# ---------------------------------------------------------------------------
_YEAR_RE = re.compile(r"\b(?:19|20)\d{2}\b")
_DURATION_RE = re.compile(
    r"\b(\d+)\s*(?:\+\s*)?(?:år|years?|yrs?|år)\b", re.IGNORECASE
)
_EXP_SECTION_RE = re.compile(
    r"(?:erfarenhet|experience|arbetslivserfarenhet)[^\n]*\n(.*?)(?:\n{2,}|\Z)",
    re.IGNORECASE | re.DOTALL,
)


def _extract_skills_from_doc(doc, matcher) -> Dict[str, List[str]]:
    """Run PhraseMatcher and return skills grouped by category."""
    matches = matcher(doc)
    seen: Set[str] = set()
    by_category: Dict[str, List[str]] = {}

    for _, start, end in matches:
        span_text = doc[start:end].text
        key = span_text.lower()
        if key in seen:
            continue
        seen.add(key)
        category = CATEGORY_MAP.get(key, "other")
        by_category.setdefault(category, []).append(span_text)

    return by_category


def _estimate_experience_years(text: str) -> int:
    """
    Rough heuristic: find explicit 'X years' or count unique calendar years
    in the text (each unique year pair ≈ 1 year of experience).
    """
    # Direct mention: "5 years", "5+ år"
    match = _DURATION_RE.search(text)
    if match:
        return int(match.group(1))

    # Count unique years mentioned → rough tenure estimate
    years_found = sorted(set(int(y) for y in _YEAR_RE.findall(text)))
    if len(years_found) >= 2:
        span = years_found[-1] - years_found[0]
        # Only trust the span if there are at least 2 distinct years and span is plausible
        if 1 <= span <= 50:
            return span

    return 0


def _extract_education(text: str) -> List[str]:
    """Pull lines that likely describe education."""
    edu_keywords = re.compile(
        r"\b(bachelor|master|phd|msc|bsc|civilingenjör|kandidat|magister"
        r"|doktor|högskoleing|university|universitet|college|institute|institutet"
        r"|kth|chalmers|lith|lth)\b",
        re.IGNORECASE,
    )
    results: List[str] = []
    for line in text.splitlines():
        if edu_keywords.search(line):
            cleaned = line.strip()
            if cleaned and cleaned not in results:
                results.append(cleaned)
    return results[:5]  # cap at 5 lines


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def parse_cv(text: str) -> Dict:
    """
    Extract structured data from CV text.

    Returns:
        skills_by_category: dict of category → [skill, ...]
        all_skills:          flat list of all matched skills
        experience_years:    estimated years of experience (int)
        education:           list of education-related lines
    """
    nlp, matcher = _get_nlp_and_matcher(text)
    doc = nlp(text)
    skills_by_category = _extract_skills_from_doc(doc, matcher)
    all_skills: List[str] = [
        skill
        for category, skills in skills_by_category.items()
        for skill in skills
    ]
    return {
        "skills_by_category": skills_by_category,
        "all_skills": all_skills,
        "experience_years": _estimate_experience_years(text),
        "education": _extract_education(text),
    }


def parse_job(text: str) -> Dict:
    """
    Extract structured requirements from a job-ad text.

    Returns:
        required_skills:     skills found in "required / krav" sections
        preferred_skills:    skills found in "preferred / merit" sections
        all_skills:          all matched skills (union)
        experience_required: estimated minimum years mentioned
        education_hints:     education-related lines
    """
    nlp, matcher = _get_nlp_and_matcher(text)
    doc = nlp(text)
    skills_by_category = _extract_skills_from_doc(doc, matcher)
    all_skills: List[str] = [
        skill
        for skills in skills_by_category.values()
        for skill in skills
    ]

    # Try to split required vs preferred by looking for section headers
    required_skills: List[str] = []
    preferred_skills: List[str] = []

    req_pattern = re.compile(
        r"(?:krav|required|requirements|must.have|vi söker)[^\n]*\n(.*?)(?=\n{2,}|merit|preferred|nice.to.have|\Z)",
        re.IGNORECASE | re.DOTALL,
    )
    merit_pattern = re.compile(
        r"(?:meriterande|merit|preferred|nice.to.have|bonus)[^\n]*\n(.*?)(?=\n{2,}|\Z)",
        re.IGNORECASE | re.DOTALL,
    )

    req_match = req_pattern.search(text)
    merit_match = merit_pattern.search(text)

    if req_match:
        req_doc = nlp(req_match.group(1))
        req_skills_map = _extract_skills_from_doc(req_doc, matcher)
        required_skills = [s for skills in req_skills_map.values() for s in skills]

    if merit_match:
        merit_doc = nlp(merit_match.group(1))
        merit_skills_map = _extract_skills_from_doc(merit_doc, matcher)
        preferred_skills = [s for skills in merit_skills_map.values() for s in skills]

    # If section splitting didn't work, treat all skills as required
    if not required_skills and not preferred_skills:
        required_skills = all_skills

    return {
        "required_skills": required_skills,
        "preferred_skills": preferred_skills,
        "all_skills": all_skills,
        "skills_by_category": skills_by_category,
        "experience_required": _estimate_experience_years(text),
        "education_hints": _extract_education(text),
    }
