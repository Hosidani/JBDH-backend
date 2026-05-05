# apps/ai_services/file_text_extraction_service.py
"""
Extract text from uploaded CV files. Supports PDF and DOCX (mirrors .NET FileHelper + ManageIOService).
"""
import os
import logging
from typing import Optional
import re

from apps.ai_services.services.pdf_text_extraction_service import PDFTextExtractionService

logger = logging.getLogger(__name__)


def extract_text_from_file(file_path: str) -> str:
    """
    Extract text from a file. Supports .pdf and .docx.
    Returns extracted text or empty string on failure.
    """
    if not file_path or not os.path.isfile(file_path):
        logger.error("File not found: %s", file_path)
        return ""

    ext = os.path.splitext(file_path)[1].lower()
    try:
        if ext == ".pdf":
            raw = PDFTextExtractionService.extract_text_from_pdf(file_path)
            return _normalize_extracted_text(raw)
        if ext in (".docx", ".doc"):
            raw = _extract_text_from_docx(file_path)
            return _normalize_extracted_text(raw)
        logger.error("Unsupported file extension: %s. Only PDF and DOCX are supported.", ext)
        return ""
    except Exception as e:
        logger.error("Error extracting text from %s: %s", file_path, e)
        return ""


def _extract_text_from_docx(file_path: str) -> str:
    """Extract text from a DOCX file using python-docx."""
    try:
        from docx import Document
    except ImportError:
        logger.error("python-docx is not installed. Install with: pip install python-docx")
        return ""

    try:
        doc = Document(file_path)
        return "\n".join(p.text for p in doc.paragraphs if p.text).strip()
    except Exception as e:
        logger.error("DOCX extraction error: %s", e)
        return ""


def _normalize_extracted_text(text: str) -> str:
    # Collapse multiple spaces / tabs to a single space
    text = re.sub(r'[ \t]{2,}', ' ', text)
    # Remove single-char noise lines (e.g. stray bullet remnants)
    text = re.sub(r'^\s*[a-zA-ZåäöÅÄÖ]\s*$', '', text, flags=re.MULTILINE)
    # Collapse excessive blank lines
    text = re.sub(r'\n{3,}', '\n\n', text)
    return text.strip()
