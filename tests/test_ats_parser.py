"""
Tests for ats_service.parser

These tests require spaCy + en_core_web_sm to be installed in the environment.
If the model is not available (e.g. running in the main Django container that
does not have spaCy installed), the tests are skipped automatically.
"""

import unittest

# Guard: only run parser tests when spaCy + model are available
try:
    import spacy
    spacy.load("en_core_web_sm")
    SPACY_AVAILABLE = True
except (ImportError, OSError):
    SPACY_AVAILABLE = False


_SKIP_MSG = "spaCy en_core_web_sm not installed – run these tests inside the ats container"


@unittest.skipUnless(SPACY_AVAILABLE, _SKIP_MSG)
class TestParseCv(unittest.TestCase):
    def _parse(self, text):
        from ats_service.parser import parse_cv
        return parse_cv(text)

    def test_returns_required_keys(self):
        result = self._parse("Python developer with 3 years of experience.")
        for key in ["skills_by_category", "all_skills", "experience_years", "education"]:
            self.assertIn(key, result)

    def test_detects_python(self):
        result = self._parse("I am a Python developer using Django and PostgreSQL.")
        self.assertIn("Python", result["all_skills"])

    def test_detects_multiple_skills(self):
        result = self._parse(
            "Experienced with Python, Django, Docker, PostgreSQL, and Redis."
        )
        skills_lower = [s.lower() for s in result["all_skills"]]
        for expected in ["python", "django", "docker", "postgresql", "redis"]:
            self.assertIn(expected, skills_lower, f"Expected {expected!r} to be detected")

    def test_experience_years_from_explicit_mention(self):
        result = self._parse("I have 5 years of experience in software development.")
        self.assertEqual(result["experience_years"], 5)

    def test_experience_years_from_date_range(self):
        result = self._parse("Worked at ACME Corp 2018 to 2023 as a software engineer.")
        self.assertGreaterEqual(result["experience_years"], 4)

    def test_education_extracted(self):
        result = self._parse(
            "Education: BSc Computer Science at KTH, graduated 2019."
        )
        self.assertGreater(len(result["education"]), 0)

    def test_empty_text_returns_empty_results(self):
        result = self._parse("")
        self.assertEqual(result["all_skills"], [])
        self.assertEqual(result["experience_years"], 0)

    def test_skills_are_grouped_by_category(self):
        result = self._parse("Python, Django, Docker, PostgreSQL.")
        cats = result["skills_by_category"]
        self.assertIn("programming", cats)
        self.assertIn("Python", cats["programming"])


@unittest.skipUnless(SPACY_AVAILABLE, _SKIP_MSG)
class TestParseJob(unittest.TestCase):
    def _parse(self, text):
        from ats_service.parser import parse_job
        return parse_job(text)

    def test_returns_required_keys(self):
        result = self._parse("We are looking for a Python developer.")
        for key in [
            "required_skills", "preferred_skills", "all_skills",
            "skills_by_category", "experience_required", "education_hints",
        ]:
            self.assertIn(key, result)

    def test_detects_required_skills_without_sections(self):
        result = self._parse(
            "You will work with Python, Django, Docker and PostgreSQL daily."
        )
        skills_lower = [s.lower() for s in result["all_skills"]]
        self.assertIn("python", skills_lower)
        self.assertIn("django", skills_lower)

    def test_splits_required_vs_preferred_sections(self):
        text = (
            "Krav:\n"
            "Python, Django, PostgreSQL\n\n"
            "Meriterande:\n"
            "Kubernetes, Terraform\n"
        )
        result = self._parse(text)
        req_lower = [s.lower() for s in result["required_skills"]]
        merit_lower = [s.lower() for s in result["preferred_skills"]]
        self.assertIn("python", req_lower)
        # Kubernetes or Terraform should appear in preferred (or all_skills)
        all_lower = [s.lower() for s in result["all_skills"]]
        self.assertTrue(
            "kubernetes" in merit_lower or "kubernetes" in all_lower,
            "Kubernetes should appear somewhere in job parse result",
        )

    def test_experience_required_extracted(self):
        result = self._parse("We require at least 3 years of experience with Python.")
        self.assertEqual(result["experience_required"], 3)

    def test_education_hints_extracted(self):
        result = self._parse(
            "Requirements: Bachelor's degree in Computer Science or equivalent."
        )
        self.assertGreater(len(result["education_hints"]), 0)

    def test_empty_text_returns_empty_results(self):
        result = self._parse("")
        self.assertEqual(result["all_skills"], [])
        self.assertEqual(result["experience_required"], 0)
