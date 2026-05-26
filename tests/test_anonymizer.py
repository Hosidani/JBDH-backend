"""
tests/test_anonymizer.py

Unit tests for the CV anonymization pipeline.

Structure:
  TestAtsAnonymizer       – ats_service/anonymizer.py (NER + regex core)
                            Skipped if spaCy model is not installed.
  TestAtsAnonymizerRegex  – regex patterns only (no spaCy required)
  TestImageRedactor       – anonymization_service/image_redactor.py
  TestAnonymizerClient    – anonymization_service/anonymizer.py (HTTP wrapper, fully mocked)
"""

import io
import sys
import unittest
from unittest.mock import MagicMock, patch

# ---------------------------------------------------------------------------
# conftest: allow running without Django (mirrors conftest.py)
# ---------------------------------------------------------------------------
if "django" not in sys.modules:
    django_mock = MagicMock()
    django_mock.test.SimpleTestCase = unittest.TestCase
    sys.modules["django"] = django_mock
    sys.modules["django.test"] = django_mock.test

# ---------------------------------------------------------------------------
# spaCy guard – ats_service tests are skipped without the model
# ---------------------------------------------------------------------------
try:
    import spacy as _spacy
    _spacy.load("en_core_web_sm")
    SPACY_AVAILABLE = True
except (ImportError, OSError):
    SPACY_AVAILABLE = False

_SKIP_NER = "spaCy en_core_web_sm not installed – run inside the ats container"


# ===========================================================================
# 1. NER + regex core (ats_service/anonymizer.py) – requires spaCy
# ===========================================================================

@unittest.skipUnless(SPACY_AVAILABLE, _SKIP_NER)
class TestAtsAnonymizer(unittest.TestCase):
    """Tests that require the spaCy model to be present."""

    def _anonymize(self, text: str) -> dict:
        from ats_service.anonymizer import anonymize
        return anonymize(text)

    # --- PERSON (NER) ---

    def test_person_name_replaced(self):
        result = self._anonymize("My name is Anna Lindqvist and I am a developer.")
        self.assertNotIn("Anna", result["anonymized_text"])
        self.assertNotIn("Lindqvist", result["anonymized_text"])
        self.assertIn("[NAMN BORTTAGET]", result["anonymized_text"])

    def test_audit_log_contains_person(self):
        result = self._anonymize("Contact: Erik Svensson, senior engineer.")
        labels = [e["label"] for e in result["audit_log"]]
        self.assertIn("person", labels)

    def test_org_name_kept(self):
        """Employer names (ORG) should NOT be removed – they are legitimate CV data."""
        result = self._anonymize("I worked at Volvo Cars AB for 3 years.")
        self.assertIn("Volvo Cars AB", result["anonymized_text"])

    # --- Email ---

    def test_email_replaced(self):
        result = self._anonymize("Send mail to anna@gmail.com for details.")
        self.assertNotIn("anna@gmail.com", result["anonymized_text"])
        self.assertIn("[EMAIL BORTTAGET]", result["anonymized_text"])

    def test_multiple_emails_replaced(self):
        result = self._anonymize("primary: a@b.com, secondary: c@d.se")
        self.assertEqual(result["anonymized_text"].count("[EMAIL BORTTAGET]"), 2)

    # --- Phone ---

    def test_swedish_mobile_replaced(self):
        for phone in ["070-123 45 67", "0701234567", "+46701234567"]:
            with self.subTest(phone=phone):
                result = self._anonymize(f"Call me at {phone}.")
                self.assertNotIn(phone, result["anonymized_text"])
                self.assertIn("[TELEFON BORTTAGET]", result["anonymized_text"])

    def test_nordic_phone_replaced(self):
        result = self._anonymize("Norwegian contact: +47 91 23 45 67")
        self.assertIn("[TELEFON BORTTAGET]", result["anonymized_text"])

    # --- Personnummer ---

    def test_personnummer_replaced(self):
        for pnr in ["19901231-1234", "9012311234", "19900101 1234"]:
            with self.subTest(pnr=pnr):
                result = self._anonymize(f"SSN: {pnr}")
                self.assertNotIn(pnr, result["anonymized_text"])
                self.assertIn("[PERSONNUMMER BORTTAGET]", result["anonymized_text"])

    # --- LinkedIn / URL ---

    def test_linkedin_replaced(self):
        result = self._anonymize("Profile: linkedin.com/in/anna-lindqvist")
        self.assertNotIn("anna-lindqvist", result["anonymized_text"])
        self.assertIn("[PROFIL BORTTAGET]", result["anonymized_text"])

    def test_https_url_replaced(self):
        result = self._anonymize("See my portfolio at https://portfolio.example.com/anna")
        self.assertNotIn("portfolio.example.com", result["anonymized_text"])

    # --- Empty / edge cases ---

    def test_empty_string(self):
        result = self._anonymize("")
        self.assertEqual(result["anonymized_text"], "")
        self.assertEqual(result["total_replacements"], 0)

    def test_no_pii_unchanged(self):
        text = "Experienced Python developer with 5 years in backend systems."
        result = self._anonymize(text)
        self.assertEqual(result["anonymized_text"], text)
        self.assertEqual(result["total_replacements"], 0)

    def test_total_replacements_count(self):
        text = "Anna Svensson, anna@test.se, 070-111 22 33"
        result = self._anonymize(text)
        self.assertGreaterEqual(result["total_replacements"], 2)  # at least email + phone


# ===========================================================================
# 2. Regex patterns only – no spaCy required
# ===========================================================================

class TestAtsAnonymizerRegex(unittest.TestCase):
    """
    Tests that validate regex patterns directly.
    Monkey-patches _apply_ner to be a no-op so spaCy is not needed.
    """

    def _anonymize_regex_only(self, text: str) -> dict:
        from ats_service import anonymizer as mod
        # Temporarily disable NER
        original = mod._apply_ner
        mod._apply_ner = lambda t, a: t
        try:
            return mod.anonymize(text)
        finally:
            mod._apply_ner = original

    def test_email_pattern(self):
        result = self._anonymize_regex_only("user@example.com")
        self.assertIn("[EMAIL BORTTAGET]", result["anonymized_text"])

    def test_swedish_mobile_pattern(self):
        result = self._anonymize_regex_only("070-123 45 67")
        self.assertIn("[TELEFON BORTTAGET]", result["anonymized_text"])

    def test_international_phone_pattern(self):
        result = self._anonymize_regex_only("+44 7911 123456")
        self.assertIn("[TELEFON BORTTAGET]", result["anonymized_text"])

    def test_personnummer_pattern(self):
        result = self._anonymize_regex_only("19850612-4321")
        self.assertIn("[PERSONNUMMER BORTTAGET]", result["anonymized_text"])

    def test_linkedin_pattern(self):
        result = self._anonymize_regex_only("https://linkedin.com/in/johndoe")
        self.assertIn("[PROFIL BORTTAGET]", result["anonymized_text"])

    def test_github_pattern(self):
        result = self._anonymize_regex_only("github.com/johndoe")
        self.assertIn("[PROFIL BORTTAGET]", result["anonymized_text"])

    def test_audit_log_tracks_each_category(self):
        text = "anna@test.se and +46701234567"
        result = self._anonymize_regex_only(text)
        labels = {e["label"] for e in result["audit_log"]}
        self.assertIn("email", labels)
        self.assertIn("telefon", labels)

    def test_audit_log_count_increments(self):
        text = "a@b.com and c@d.se"
        result = self._anonymize_regex_only(text)
        email_entry = next(e for e in result["audit_log"] if e["label"] == "email")
        self.assertEqual(email_entry["count"], 2)


# ===========================================================================
# 3. Image redactor (anonymization_service/image_redactor.py)
# ===========================================================================

class TestImageRedactor(unittest.TestCase):

    def _make_minimal_pdf(self) -> bytes:
        """Create the smallest valid PDF (no images) using PyMuPDF."""
        try:
            import fitz
        except ImportError:
            self.skipTest("PyMuPDF not installed")
        doc = fitz.open()
        doc.new_page()
        return doc.tobytes()

    def test_pdf_without_images_returns_unchanged_bytes(self):
        from anonymization_service.image_redactor import redact_images_from_pdf
        pdf = self._make_minimal_pdf()
        result = redact_images_from_pdf(pdf)
        self.assertEqual(result.images_removed, 0)
        self.assertEqual(result.pages_affected, 0)
        # PDF bytes should still be valid
        self.assertTrue(result.pdf_bytes.startswith(b"%PDF"))

    def test_pdf_with_image_removes_it(self):
        """Insert a small PNG into a PDF and verify it gets redacted."""
        try:
            import fitz
        except ImportError:
            self.skipTest("PyMuPDF not installed")

        from anonymization_service.image_redactor import redact_images_from_pdf

        # Build a 1×1 white PNG in memory
        import struct, zlib
        def _make_1x1_png() -> bytes:
            sig = b'\x89PNG\r\n\x1a\n'
            ihdr_data = struct.pack('>IIBBBBB', 1, 1, 8, 2, 0, 0, 0)
            ihdr_crc = zlib.crc32(b'IHDR' + ihdr_data) & 0xffffffff
            ihdr = struct.pack('>I', 13) + b'IHDR' + ihdr_data + struct.pack('>I', ihdr_crc)
            raw = b'\x00\xff\xff\xff'
            compressed = zlib.compress(raw)
            idat_crc = zlib.crc32(b'IDAT' + compressed) & 0xffffffff
            idat = struct.pack('>I', len(compressed)) + b'IDAT' + compressed + struct.pack('>I', idat_crc)
            iend_crc = zlib.crc32(b'IEND') & 0xffffffff
            iend = struct.pack('>I', 0) + b'IEND' + struct.pack('>I', iend_crc)
            return sig + ihdr + idat + iend

        doc = fitz.open()
        page = doc.new_page(width=200, height=200)
        png_bytes = _make_1x1_png()
        page.insert_image(fitz.Rect(10, 10, 100, 100), stream=png_bytes)
        pdf_with_image = doc.tobytes()
        doc.close()

        result = redact_images_from_pdf(pdf_with_image)
        self.assertGreaterEqual(result.images_removed, 1)
        self.assertGreaterEqual(result.pages_affected, 1)
        self.assertTrue(result.pdf_bytes.startswith(b"%PDF"))

    def test_raises_on_non_pdf_bytes(self):
        from anonymization_service.image_redactor import redact_images_from_pdf
        with self.assertRaises(Exception):
            redact_images_from_pdf(b"this is not a pdf")


# ===========================================================================
# 4. Backend anonymizer HTTP wrapper (anonymization_service/anonymizer.py)
# ===========================================================================

class TestAnonymizerClient(unittest.TestCase):
    """Tests the Django-side anonymizer that calls ats via HTTP (fully mocked)."""

    def _call(self, text: str, mock_response: dict):
        with patch("anonymization_service.anonymizer.anonymize_cv_text", return_value=mock_response):
            from anonymization_service.anonymizer import anonymize_text
            return anonymize_text(text)

    def test_maps_ats_response_to_result(self):
        mock = {
            "anonymized_text": "Hello [NAMN BORTTAGET]",
            "audit_log": [{"label": "person", "source": "spacy", "count": 1}],
            "total_replacements": 1,
        }
        result = self._call("Hello Anna", mock)
        self.assertEqual(result.anonymized_text, "Hello [NAMN BORTTAGET]")
        self.assertEqual(len(result.audit_log), 1)
        self.assertEqual(result.audit_log[0].label, "person")

    def test_total_replacements_property(self):
        mock = {
            "anonymized_text": "[EMAIL BORTTAGET]",
            "audit_log": [
                {"label": "email", "source": "regex", "count": 2},
                {"label": "telefon", "source": "regex", "count": 1},
            ],
            "total_replacements": 3,
        }
        result = self._call("a@b.com c@d.se 070-000", mock)
        self.assertEqual(result.total_replacements, 3)

    def test_empty_text_skips_http_call(self):
        with patch("anonymization_service.anonymizer.anonymize_cv_text") as mock_http:
            from anonymization_service.anonymizer import anonymize_text
            result = anonymize_text("")
            mock_http.assert_not_called()
            self.assertEqual(result.anonymized_text, "")

    def test_fallback_on_http_error(self):
        with patch(
            "anonymization_service.anonymizer.anonymize_cv_text",
            side_effect=ConnectionError("ats unreachable"),
        ):
            from anonymization_service.anonymizer import anonymize_text
            result = anonymize_text("Anna Svensson, anna@test.se")
        # Should return original text, not raise
        self.assertIn("Anna", result.anonymized_text)
        self.assertEqual(result.audit_log, [])


if __name__ == "__main__":
    unittest.main()
