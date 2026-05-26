"""
anonymization_service/anonymizer.py

Hybrid CV anonymizer – delegates NER + regex to the ats microservice via HTTP.

The ats container already runs spaCy (en_core_web_sm / sv_core_news_sm) for
ATS parsing, so we reuse those models at zero extra memory cost in the backend.

Pipeline:
  POST http://ats:8001/anonymize/text
      → spaCy NER  (PERSON → [NAMN BORTTAGET])
      → regex      (email, phone, URL, personnummer, address)
  Returns anonymized text + audit log (no originals stored – GDPR compliant).

AI Act: process is deterministic – same input → same output.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field

from core.ats_client import anonymize_cv_text

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Result types (kept compatible with cv_anonymization_service.py)
# ---------------------------------------------------------------------------


@dataclass
class AuditEntry:
    label: str
    count: int = 1
    confidence: float | None = None
    source: str = "regex"


@dataclass
class AnonymizationResult:
    anonymized_text: str
    audit_log: list[AuditEntry] = field(default_factory=list)

    @property
    def total_replacements(self) -> int:
        return sum(e.count for e in self.audit_log)


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


def anonymize_text(text: str) -> AnonymizationResult:
    """
    Anonymize free-form CV text by calling the ats microservice.

    Falls back gracefully if the ats service is unavailable – logs a warning
    and returns the original text so the caller is never blocked.
    """
    if not text or not text.strip():
        return AnonymizationResult(anonymized_text=text)

    try:
        result = anonymize_cv_text(text)
    except Exception as exc:
        logger.warning(
            "ATS anonymization service unreachable – returning original text. Error: %s", exc
        )
        return AnonymizationResult(anonymized_text=text)

    audit_log = [
        AuditEntry(
            label=entry.get("label", "unknown"),
            count=entry.get("count", 1),
            source=entry.get("source", "regex"),
        )
        for entry in result.get("audit_log", [])
    ]

    logger.info(
        "CV anonymization complete – %d replacement(s) across %d category/ies.",
        result.get("total_replacements", 0),
        len(audit_log),
    )

    return AnonymizationResult(
        anonymized_text=result.get("anonymized_text", text),
        audit_log=audit_log,
    )

