# apps/ai_services/file_text_extraction_service.py
"""
Extract text from uploaded CV files. Supports PDF and DOCX (mirrors .NET FileHelper + ManageIOService).
"""
import os
import logging
from typing import Optional

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
            return PDFTextExtractionService.extract_text_from_pdf(file_path)
        if ext in (".docx", ".doc"):
            return _extract_text_from_docx(file_path)
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
