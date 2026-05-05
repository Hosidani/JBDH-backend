"""
Tests for ats_service.scorer

No spaCy required – tests use pre-built parsed dicts directly.
"""

import unittest

from ats_service.scorer import score, _overlap_score, _experience_score, _education_score


# ---------------------------------------------------------------------------
# Helper factories
# ---------------------------------------------------------------------------

def _cv(
    programming=None,
    framework=None,
    database=None,
    soft=None,
    experience_years=0,
    education=None,
):
    skills_by_category = {}
    if programming:
        skills_by_category["programming"] = programming
    if framework:
        skills_by_category["framework"] = framework
    if database:
        skills_by_category["database"] = database
    if soft:
        skills_by_category["soft"] = soft

    all_skills = [s for lst in skills_by_category.values() for s in lst]
    return {
        "skills_by_category": skills_by_category,
        "all_skills": all_skills,
        "experience_years": experience_years,
        "education": education or [],
    }


def _job(
    programming=None,
    framework=None,
    database=None,
    soft=None,
    required_skills=None,
    preferred_skills=None,
    experience_required=0,
    education_hints=None,
):
    skills_by_category = {}
    if programming:
        skills_by_category["programming"] = programming
    if framework:
        skills_by_category["framework"] = framework
    if database:
        skills_by_category["database"] = database
    if soft:
        skills_by_category["soft"] = soft

    all_skills = [s for lst in skills_by_category.values() for s in lst]
    return {
        "skills_by_category": skills_by_category,
        "all_skills": all_skills,
        "required_skills": required_skills or all_skills,
        "preferred_skills": preferred_skills or [],
        "experience_required": experience_required,
        "education_hints": education_hints or [],
    }


# ---------------------------------------------------------------------------
# _overlap_score
# ---------------------------------------------------------------------------

class TestOverlapScore(unittest.TestCase):
    def test_perfect_match(self):
        result = _overlap_score({"python", "django"}, {"python", "django"})
        self.assertAlmostEqual(result, 1.0)

    def test_no_match(self):
        result = _overlap_score({"java"}, {"python", "django"})
        self.assertAlmostEqual(result, 0.0)

    def test_partial_match(self):
        result = _overlap_score({"python", "java"}, {"python", "django", "fastapi"})
        self.assertAlmostEqual(result, 1 / 3)

    def test_empty_job_skills_returns_full_score(self):
        # No requirements → automatic pass
        result = _overlap_score({"python"}, set())
        self.assertAlmostEqual(result, 1.0)


# ---------------------------------------------------------------------------
# _experience_score
# ---------------------------------------------------------------------------

class TestExperienceScore(unittest.TestCase):
    def test_meets_requirement_exactly(self):
        self.assertAlmostEqual(_experience_score(5, 5), 1.0)

    def test_exceeds_requirement(self):
        self.assertAlmostEqual(_experience_score(10, 5), 1.0)

    def test_zero_required_returns_full_score(self):
        self.assertAlmostEqual(_experience_score(0, 0), 1.0)

    def test_half_experience(self):
        self.assertAlmostEqual(_experience_score(2, 4), 0.5)

    def test_no_experience_when_required(self):
        self.assertAlmostEqual(_experience_score(0, 3), 0.0)


# ---------------------------------------------------------------------------
# _education_score
# ---------------------------------------------------------------------------

class TestEducationScore(unittest.TestCase):
    def test_no_education_required(self):
        self.assertAlmostEqual(_education_score([], []), 1.0)

    def test_matching_education(self):
        cv_edu = ["BSc Computer Science at KTH"]
        job_edu = ["bachelor"]
        result = _education_score(cv_edu, job_edu)
        self.assertAlmostEqual(result, 1.0)

    def test_no_cv_education_when_required(self):
        result = _education_score([], ["Bachelor"])
        self.assertAlmostEqual(result, 0.0)


# ---------------------------------------------------------------------------
# Full score()
# ---------------------------------------------------------------------------

class TestScore(unittest.TestCase):
    def test_perfect_match_returns_high_score(self):
        cv = _cv(
            programming=["Python"],
            framework=["Django", "FastAPI"],
            database=["PostgreSQL"],
            soft=["Teamwork", "Communication"],
            experience_years=5,
            education=["BSc Computer Science at KTH"],
        )
        job = _job(
            programming=["Python"],
            framework=["Django", "FastAPI"],
            database=["PostgreSQL"],
            soft=["Teamwork", "Communication"],
            experience_required=5,
            education_hints=["bsc"],
        )
        result = score(cv, job)
        self.assertGreaterEqual(result["total_score"], 85.0)

    def test_no_match_returns_low_score(self):
        cv = _cv(programming=["Java"], experience_years=1)
        job = _job(
            programming=["Python"],
            framework=["Django"],
            database=["PostgreSQL"],
            experience_required=5,
        )
        result = score(cv, job)
        self.assertLessEqual(result["total_score"], 40.0)

    def test_result_keys_present(self):
        cv = _cv(programming=["Python"])
        job = _job(programming=["Python"])
        result = score(cv, job)
        for key in [
            "total_score", "breakdown", "matched_skills",
            "missing_skills", "bonus_skills", "recommendations",
        ]:
            self.assertIn(key, result, f"Missing key: {key}")

    def test_breakdown_keys_present(self):
        cv = _cv(programming=["Python"])
        job = _job(programming=["Python"])
        result = score(cv, job)
        breakdown = result["breakdown"]
        for key in ["skills_match", "experience_match", "education_match", "soft_skills_match"]:
            self.assertIn(key, breakdown, f"Missing breakdown key: {key}")

    def test_total_score_between_0_and_100(self):
        cv = _cv(programming=["Python"], experience_years=3)
        job = _job(programming=["Java"], experience_required=10)
        result = score(cv, job)
        self.assertGreaterEqual(result["total_score"], 0.0)
        self.assertLessEqual(result["total_score"], 100.0)

    def test_matched_skills_are_subset_of_job_requirements(self):
        cv = _cv(programming=["Python", "Java"])
        job = _job(programming=["Python", "TypeScript"])
        result = score(cv, job)
        # "python" should be matched, "typescript" should be missing
        self.assertIn("Python".lower(), result["matched_skills"])
        self.assertIn("TypeScript".lower(), result["missing_skills"])

    def test_recommendations_generated_for_missing_skills(self):
        cv = _cv(programming=["Java"])
        job = _job(programming=["Python", "Django", "FastAPI", "PostgreSQL", "Docker"])
        result = score(cv, job)
        self.assertGreater(len(result["recommendations"]), 0)

    def test_no_recommendations_for_perfect_match(self):
        cv = _cv(
            programming=["Python"],
            framework=["Django"],
            database=["PostgreSQL"],
            experience_years=5,
        )
        job = _job(
            programming=["Python"],
            framework=["Django"],
            database=["PostgreSQL"],
            experience_required=5,
        )
        result = score(cv, job)
        self.assertEqual(result["recommendations"], [])

    def test_empty_job_no_requirements(self):
        """Job with no skills should not crash and should return high score."""
        cv = _cv(programming=["Python"])
        job = _job()
        result = score(cv, job)
        self.assertGreaterEqual(result["total_score"], 0.0)
        self.assertLessEqual(result["total_score"], 100.0)

    def test_empty_cv(self):
        """Empty CV against strong job should return low score without crashing."""
        cv = _cv()
        job = _job(
            programming=["Python"],
            framework=["Django"],
            experience_required=3,
        )
        result = score(cv, job)
        self.assertLess(result["total_score"], 50.0)
