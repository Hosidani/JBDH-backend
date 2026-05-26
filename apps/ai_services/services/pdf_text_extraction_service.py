# apps/ai_services/pdf_text_extraction_service.py
import logging

logger = logging.getLogger(__name__)


class PDFTextExtractionService:
    @staticmethod
    def extract_text_from_pdf(pdf_path: str) -> str:
        """
        Extract text from PDF. Tries both PyMuPDF and pdfplumber, then picks
        the result with more spaces (proxy for better word-boundary detection).
        Falls back to whichever produced >50 chars if only one succeeds.
        """
        mupdf_text = PDFTextExtractionService._try_pymupdf(pdf_path)
        plumber_text = PDFTextExtractionService._try_pdfplumber(pdf_path)

        mupdf_ok = bool(mupdf_text and len(mupdf_text.strip()) > 50)
        plumber_ok = bool(plumber_text and len(plumber_text.strip()) > 50)

        if mupdf_ok and plumber_ok:
            # Pick the one with more spaces – better word-boundary reconstruction
            if plumber_text.count(' ') >= mupdf_text.count(' '):
                logger.debug("PDF extraction: pdfplumber chosen (more spaces)")
                return plumber_text.strip()
            logger.debug("PDF extraction: PyMuPDF chosen (more spaces)")
            return mupdf_text.strip()

        if plumber_ok:
            logger.debug("PDF extraction: pdfplumber chosen (PyMuPDF failed)")
            return plumber_text.strip()

        if mupdf_ok:
            logger.debug("PDF extraction: PyMuPDF chosen (pdfplumber failed)")
            return mupdf_text.strip()

        raise RuntimeError(f"Failed to extract usable text from PDF: {pdf_path}")

    @staticmethod
    def _try_pymupdf(pdf_path: str) -> str:
        try:
            import fitz  # pymupdf
            doc = fitz.open(pdf_path)
            pages = []
            for page in doc:
                # sort=True reads blocks in visual order (top→bottom, left→right).
                # This captures sidebar text (name, email, phone) that content-stream
                # order misses for two-column CV layouts.
                blocks = page.get_text("blocks", sort=True)
                text_blocks = [
                    b[4].strip() for b in blocks
                    if b[6] == 0 and b[4].strip()  # type 0 = text, skip image blocks
                ]
                pages.append("\n".join(text_blocks))
            doc.close()
            return "\n".join(pages)
        except Exception as e:
            logger.warning("PyMuPDF extraction failed: %s", e)
            return ""

    @staticmethod
    def _try_pdfplumber(pdf_path: str) -> str:
        try:
            import pdfplumber
            with pdfplumber.open(pdf_path) as pdf:
                # x_tolerance: max x-gap between chars before inserting a space
                # 2 is tighter than the default 3 to catch closely-spaced words
                pages = [
                    page.extract_text(x_tolerance=2, y_tolerance=3) or ""
                    for page in pdf.pages
                ]
            return "\n".join(pages)
        except Exception as e:
            logger.warning("pdfplumber extraction failed: %s", e)
            return ""