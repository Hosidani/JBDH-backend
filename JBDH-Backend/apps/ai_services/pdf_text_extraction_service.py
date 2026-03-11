# apps/ai_services/pdf_text_extraction_service.py
import PyPDF2

class PDFTextExtractionService:
    @staticmethod
    def extract_text_from_pdf(pdf_path):
        """
        Extracts text from a PDF file using PyPDF2.
        Returns the extracted text or raises an exception if extraction fails.
        """
        text = ""
        try:
            with open(pdf_path, "rb") as f:
                reader = PyPDF2.PdfReader(f)
                for page in reader.pages:
                    page_text = page.extract_text()
                    if page_text:
                        text += page_text + "\n"
        except Exception as e:
            raise RuntimeError(f"Failed to extract text from PDF: {e}")
        return text.strip()