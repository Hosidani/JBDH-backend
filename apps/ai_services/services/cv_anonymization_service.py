"""
apps/ai_services/services/cv_anonymization_service.py

Django service layer that orchestrates full CV anonymization:
  1. Text extraction  (PDF / DOCX / TXT / raw text)
  2. Text anonymization via SpaCy NER + regex (anonymization_service/anonymizer.py)
  3. Image redaction  via PyMuPDF          (anonymization_service/image_redactor.py)

Returns a CVAnonymizationResult – the caller decides what to do with the bytes
and the audit log (e.g. stream back to the client, store the audit log for
GDPR records-of-processing, forward the sanitised PDF to an AI pipeline).
"""

from __future__ import annotations

import logging
import os
from dataclasses import dataclass, field
from typing import Optional

from anonymization_service.anonymizer import anonymize_text, AnonymizationResult
from anonymization_service.image_redactor import redact_images_from_pdf, RedactionResult
from apps.ai_services.services.file_text_extraction_service import extract_text_from_file

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Result type
# ---------------------------------------------------------------------------


@dataclass
class CVAnonymizationResult:
    """
    Everything the caller needs after anonymization.

    anonymized_text  : plain-text version of the CV with PII replaced.
    redacted_pdf     : sanitised PDF bytes (None if input was not a PDF).
    text_audit       : structured log from the text anonymizer.
    images_removed   : number of images redacted from the PDF (0 for non-PDF).
    pages_affected   : number of PDF pages that had at least one image removed.
    original_filename: original uploaded filename, preserved for the response.
    """

    anonymized_text: str
    redacted_pdf: Optional[bytes] = None
    text_audit: list = field(default_factory=list)
    images_removed: int = 0
    pages_affected: int = 0
    original_filename: str = ""

    def audit_summary(self) -> dict:
        """
        Serialisable audit summary for the API response and GDPR records.
        Originals are never included.
        """
        return {
            "text_replacements": [
                {
                    "label": e.label,
                    "count": e.count,
                    "source": e.source,
                    **({"confidence": e.confidence} if e.confidence is not None else {}),
                }
                for e in self.text_audit
            ],
            "images_removed": self.images_removed,
            "pages_affected": self.pages_affected,
            "total_pii_items": sum(e.count for e in self.text_audit) + self.images_removed,
        }


# ---------------------------------------------------------------------------
# Service
# ---------------------------------------------------------------------------


class CVAnonymizationService:
    """
    Stateless service – all methods are classmethods so callers don't need
    to instantiate.  Follows the same convention as PDFTextExtractionService.
    """

    @classmethod
    def anonymize_file(cls, file_path: str, original_filename: str = "") -> CVAnonymizationResult:
        """
        Anonymize a CV from a file on disk (PDF, DOCX, or TXT).

        Args:
            file_path:         Absolute path to the uploaded file.
            original_filename: Original name of the uploaded file (for the response).

        Returns:
            CVAnonymizationResult with anonymized text, optional PDF bytes,
            and audit information.
        """
        if not os.path.isfile(file_path):
            raise FileNotFoundError(f"CV file not found: {file_path}")

        ext = os.path.splitext(file_path)[1].lower()

        # --- Text extraction ---
        extracted_text = extract_text_from_file(file_path)

        # --- Text anonymization ---
        text_result: AnonymizationResult = anonymize_text(extracted_text)

        # --- Image redaction (PDF only) ---
        redacted_pdf: Optional[bytes] = None
        img_result: Optional[RedactionResult] = None

        if ext == ".pdf":
            with open(file_path, "rb") as fh:
                raw_pdf = fh.read()
            img_result = cls._redact_pdf_images(raw_pdf)
            redacted_pdf = img_result.pdf_bytes

        return CVAnonymizationResult(
            anonymized_text=text_result.anonymized_text,
            redacted_pdf=redacted_pdf,
            text_audit=text_result.audit_log,
            images_removed=img_result.images_removed if img_result else 0,
            pages_affected=img_result.pages_affected if img_result else 0,
            original_filename=original_filename or os.path.basename(file_path),
        )

    @classmethod
    def anonymize_uploaded_file(cls, django_file) -> CVAnonymizationResult:
        """
        Anonymize a CV from a Django InMemoryUploadedFile / TemporaryUploadedFile.

        Reads the file bytes directly – no temp file needed for DOCX/TXT.
        For PDF, image redaction is applied in-memory.

        Args:
            django_file: request.FILES['<field>'] object.

        Returns:
            CVAnonymizationResult
        """
        filename: str = getattr(django_file, "name", "")
        ext = os.path.splitext(filename)[1].lower()

        raw_bytes: bytes = django_file.read()

        # --- Text extraction from bytes ---
        extracted_text = cls._extract_text_from_bytes(raw_bytes, ext, filename)

        # --- Text anonymization ---
        text_result: AnonymizationResult = anonymize_text(extracted_text)

        # --- Image redaction (PDF only) ---
        redacted_pdf: Optional[bytes] = None
        img_result: Optional[RedactionResult] = None

        if ext == ".pdf":
            img_result = cls._redact_pdf_images(raw_bytes)
            redacted_pdf = img_result.pdf_bytes

        return CVAnonymizationResult(
            anonymized_text=text_result.anonymized_text,
            redacted_pdf=redacted_pdf,
            text_audit=text_result.audit_log,
            images_removed=img_result.images_removed if img_result else 0,
            pages_affected=img_result.pages_affected if img_result else 0,
            original_filename=filename,
        )

    @classmethod
    def anonymize_raw_text(cls, text: str) -> CVAnonymizationResult:
        """
        Anonymize plain text (no file involved – e.g. text already extracted
        and stored in DB, or pasted directly from a form field).
        """
        text_result: AnonymizationResult = anonymize_text(text)
        return CVAnonymizationResult(
            anonymized_text=text_result.anonymized_text,
            text_audit=text_result.audit_log,
        )

    # ------------------------------------------------------------------
    # Private helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _redact_pdf_images(pdf_bytes: bytes) -> RedactionResult:
        try:
            return redact_images_from_pdf(pdf_bytes)
        except RuntimeError as exc:
            logger.warning("Image redaction skipped: %s", exc)
            # Return a no-op result so the caller still gets the original bytes.
            return RedactionResult(
                pdf_bytes=pdf_bytes,
                images_removed=0,
                pages_affected=0,
            )

    @staticmethod
    def _extract_text_from_bytes(raw_bytes: bytes, ext: str, filename: str) -> str:
        """
        Extract text from raw file bytes without writing to disk.
        Falls back to disk-based extraction for formats that need it.
        """
        import tempfile

        if ext == ".txt":
            return raw_bytes.decode("utf-8", errors="ignore")

        # PDF and DOCX need a file path for the extraction utilities.
        # Write to a secure temp file, extract, then delete immediately.
        suffix = ext if ext else ".tmp"
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as tmp:
            tmp.write(raw_bytes)
            tmp_path = tmp.name

        try:
            return extract_text_from_file(tmp_path)
        finally:
            try:
                os.unlink(tmp_path)
            except OSError:
                logger.warning("Could not delete temp file: %s", tmp_path)
