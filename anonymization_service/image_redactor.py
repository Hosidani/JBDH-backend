"""
anonymization_service/image_redactor.py

Removes / redacts all raster images embedded in a PDF using PyMuPDF (fitz).

GDPR rationale: profile photos are biometric data (special-category PII under
Art. 9 GDPR) and must not be forwarded to employers or AI models.

Redaction strategy:
  - Each image bounding box is covered with a filled black rectangle.
  - The PDF is then «deflated» (garbage-collected) so the original image
    stream is permanently removed from the file – not just hidden.
  - Returns the sanitised PDF as bytes together with an image count so the
    caller can include it in the audit log.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class RedactionResult:
    pdf_bytes: bytes          # Sanitised PDF content
    images_removed: int       # Total images redacted across all pages
    pages_affected: int       # Number of pages that contained at least one image


def redact_images_from_pdf(pdf_bytes: bytes) -> RedactionResult:
    """
    Accept raw PDF bytes, redact every embedded image, and return the
    sanitised PDF bytes together with statistics for the audit log.

    Raises RuntimeError if PyMuPDF is not installed.
    """
    try:
        import fitz  # pymupdf
    except ImportError as exc:
        raise RuntimeError(
            "PyMuPDF (pymupdf) is required for image redaction. "
            "Install it with: pip install pymupdf"
        ) from exc

    doc = fitz.open(stream=pdf_bytes, filetype="pdf")

    total_images = 0
    pages_affected = 0

    for page in doc:
        image_list = page.get_images(full=True)
        if not image_list:
            continue

        page_had_image = False

        for img_info in image_list:
            xref = img_info[0]  # cross-reference number of the image object

            # get_image_rects returns a list of Rect objects for this xref
            rects = page.get_image_rects(xref)
            if not rects:
                # Fallback: search by image name if rect lookup fails
                img_name = img_info[7]  # name field
                rects = page.get_image_rects(img_name) if img_name else []

            for rect in rects:
                # Draw a filled black rectangle over the image area
                page.draw_rect(rect, color=(0, 0, 0), fill=(0, 0, 0))
                total_images += 1
                page_had_image = True

        if page_had_image:
            pages_affected += 1

        # Apply redaction annotations if any were added (belt-and-suspenders)
        page.apply_redactions()

    if total_images == 0:
        logger.info("PDF image redaction: no images found – returning original bytes.")
        doc.close()
        return RedactionResult(
            pdf_bytes=pdf_bytes,
            images_removed=0,
            pages_affected=0,
        )

    # garbage=4  → remove unreferenced objects (purges original image streams)
    # deflate=True → recompress streams to reduce file size
    sanitised = doc.tobytes(garbage=4, deflate=True)
    doc.close()

    logger.info(
        "PDF image redaction: %d image(s) removed across %d page(s).",
        total_images,
        pages_affected,
    )

    return RedactionResult(
        pdf_bytes=sanitised,
        images_removed=total_images,
        pages_affected=pages_affected,
    )
