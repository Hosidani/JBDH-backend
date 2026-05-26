"""
ats_service/anonymizer.py

Text anonymization using spaCy NER + regex patterns.
Called by the /anonymize/text endpoint in main.py.

Reuses the already-loaded spaCy models from parser.py (no extra memory cost).
"""

from __future__ import annotations

import re
from typing import List

from ats_service.parser import _nlp_en, _nlp_sv, _get_nlp_and_matcher

# ---------------------------------------------------------------------------
# PII Regex patterns
# ---------------------------------------------------------------------------

_PATTERNS: list[tuple[str, re.Pattern, str]] = [
    # Most specific first to avoid partial matches
    ("personnummer", re.compile(
        r"\b(?:19|20)?\d{6}[\s\-]?\d{4}\b"
    ), "[PERSONNUMMER BORTTAGET]"),

    ("email", re.compile(
        r"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        re.IGNORECASE,
    ), "[EMAIL BORTTAGET]"),

    ("linkedin", re.compile(
        r"(?:https?://)?(?:www\.)?linkedin\.com/in/[a-zA-Z0-9\-_%]+/?",
        re.IGNORECASE,
    ), "[PROFIL BORTTAGET]"),

    ("github", re.compile(
        r"(?:https?://)?(?:www\.)?github\.com/[a-zA-Z0-9\-_%]+/?",
        re.IGNORECASE,
    ), "[PROFIL BORTTAGET]"),

    ("url", re.compile(
        r"https?://[^\s\"'<>]+",
        re.IGNORECASE,
    ), "[URL BORTTAGET]"),

    ("telefon", re.compile(
        r"""(?:
            (?:\+46|0046|0)7[0-9](?:[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2})
            |(?:\+46|0046|0)[1-9][0-9]{0,2}(?:[\s\-]?\d{2,3}){2,3}
            |\+(?:47|45|358|49|44|33|31|34|39|41|43|32|351|353|48|36|420|421)
              [\s\-]?\(?\d{1,4}\)?(?:[\s\-]?\d{2,4}){2,4}
            |\+[1-9]\d{0,2}(?:[\s\-]?\d){6,12}
        )""",
        re.VERBOSE,
    ), "[TELEFON BORTTAGET]"),

    ("gatuadress", re.compile(
        r"\b[A-ZÅÄÖ][a-zåäö]+(?:[\s\-][a-zåäö]+){0,3}"
        r"(?:s?gatan|gränd|vägen|väg|allén|stigen|torget|platsen|leden|backen|"
        r"ringen|boulevard|avenue|street|road|lane|st\.|ave\.|rd\.)"
        r"[\s,]*\d{1,4}[\s]?[A-Za-z]?(?:[\s,]+\d{3}\s?\d{2})?",
        re.IGNORECASE,
    ), "[ADRESS BORTTAGET]"),
]

_PERSON_REPLACEMENT = "[NAMN BORTTAGET]"
_PERSON_LABEL = "person"

# Matches 2–4 Title Case words on their own line (CV name header).
# Catches non-Nordic names like "Hossein Yazdani" that spaCy small models miss.
_HEADER_NAME_RE = re.compile(
    r"^([A-ZÅÄÖ\u00C0-\u017E][a-zåäöé\-\u00C0-\u017E]+"
    r"(?:[ ][A-ZÅÄÖ\u00C0-\u017E][a-zåäöé\-\u00C0-\u017E]+){1,3})\s*$",
    re.MULTILINE,
)

# Invisible Unicode characters commonly embedded by PDF generators.
_INVISIBLE_CHARS = re.compile(r'[\u00AD\u200B\u200C\u200D\u2060\uFEFF]')


# ---------------------------------------------------------------------------
# NER pass – PERSON entities only
# ---------------------------------------------------------------------------

def _apply_ner(text: str, audit: list[dict]) -> str:
    """Replace PERSON entities detected by spaCy with [NAMN BORTTAGET]."""
    nlp, _ = _get_nlp_and_matcher(text)
    doc = nlp(text)

    replacements = [
        (ent.start_char, ent.end_char)
        for ent in doc.ents
        if ent.label_ == "PERSON"
    ]
    if not replacements:
        return text

    # Replace right-to-left to preserve character offsets
    replacements.sort(key=lambda x: x[0], reverse=True)
    chars = list(text)
    for start, end in replacements:
        chars[start:end] = list(_PERSON_REPLACEMENT)
        _merge_audit(audit, _PERSON_LABEL, "spacy")

    return "".join(chars)


# ---------------------------------------------------------------------------
# Header name pass – catches names spaCy misses (non-Nordic, unusual names)
# ---------------------------------------------------------------------------

def _apply_header_name(text: str, audit: list[dict]) -> str:
    """Detect person name from the CV header (first 400 chars) and replace
    ALL occurrences in the full text.  Handles non-Nordic names that spaCy misses."""
    # Strip invisible Unicode chars (U+200B zero-width space, BOM, soft-hyphen, etc.)
    # only in the header slice used for matching – PDF generators embed these freely.
    header = _INVISIBLE_CHARS.sub('', text[:400])
    match = _HEADER_NAME_RE.search(header)
    if not match:
        return text
    name = match.group(1)
    text = re.sub(re.escape(name), "[NAMN BORTTAGET]", text, flags=re.IGNORECASE)
    _merge_audit(audit, "person_header", "regex")
    return text


# ---------------------------------------------------------------------------
# Regex pass
# ---------------------------------------------------------------------------

def _apply_regex(text: str, audit: list[dict]) -> str:
    for label, pattern, replacement in _PATTERNS:
        matches = pattern.findall(text)
        if matches:
            text = pattern.sub(replacement, text)
            for _ in matches:
                _merge_audit(audit, label, "regex")
    return text


# ---------------------------------------------------------------------------
# Audit helpers
# ---------------------------------------------------------------------------

def _merge_audit(audit: list[dict], label: str, source: str) -> None:
    for entry in audit:
        if entry["label"] == label and entry["source"] == source:
            entry["count"] += 1
            return
    audit.append({"label": label, "source": source, "count": 1})


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def anonymize(text: str) -> dict:
    """
    Anonymize CV text.  Returns:
      {
        "anonymized_text": str,
        "audit_log": [{"label": str, "source": str, "count": int}],
        "total_replacements": int,
      }
    Originals are never stored.
    """
    if not text or not text.strip():
        return {"anonymized_text": text, "audit_log": [], "total_replacements": 0}

    audit: list[dict] = []
    cleaned = _apply_header_name(text, audit)
    cleaned = _apply_ner(cleaned, audit)
    cleaned = _apply_regex(cleaned, audit)

    return {
        "anonymized_text": cleaned,
        "audit_log": audit,
        "total_replacements": sum(e["count"] for e in audit),
    }
