"""
Tests for ats_service.skills_db

No spaCy required – pure Python unit tests.
"""

import unittest

from ats_service.skills_db import (
    CATEGORY_MAP,
    get_all_skills,
    PROGRAMMING_LANGUAGES,
    FRAMEWORKS,
    DATABASES,
    CLOUD_DEVOPS,
    SOFT_SKILLS,
)


class TestGetAllSkills(unittest.TestCase):
    def test_returns_non_empty_list(self):
        skills = get_all_skills()
        self.assertIsInstance(skills, list)
        self.assertGreater(len(skills), 50)

    def test_contains_known_programming_languages(self):
        skills = get_all_skills()
        for lang in ["Python", "JavaScript", "TypeScript", "Java"]:
            self.assertIn(lang, skills, f"Expected {lang!r} in skills list")

    def test_contains_known_frameworks(self):
        skills = get_all_skills()
        for fw in ["Django", "FastAPI", "React", "Docker"]:
            self.assertIn(fw, skills, f"Expected {fw!r} in skills list")

    def test_no_duplicates_within_category(self):
        """Each individual category list should have no duplicates."""
        for category_list, name in [
            (PROGRAMMING_LANGUAGES, "PROGRAMMING_LANGUAGES"),
            (FRAMEWORKS, "FRAMEWORKS"),
            (DATABASES, "DATABASES"),
            (CLOUD_DEVOPS, "CLOUD_DEVOPS"),
            (SOFT_SKILLS, "SOFT_SKILLS"),
        ]:
            self.assertEqual(
                len(category_list),
                len(set(category_list)),
                f"Duplicates found in {name}",
            )


class TestCategoryMap(unittest.TestCase):
    def test_map_is_non_empty(self):
        self.assertGreater(len(CATEGORY_MAP), 50)

    def test_python_maps_to_programming(self):
        self.assertEqual(CATEGORY_MAP.get("python"), "programming")

    def test_django_maps_to_framework(self):
        self.assertEqual(CATEGORY_MAP.get("django"), "framework")

    def test_postgresql_maps_to_database(self):
        self.assertEqual(CATEGORY_MAP.get("postgresql"), "database")

    def test_docker_maps_to_cloud_devops(self):
        self.assertEqual(CATEGORY_MAP.get("docker"), "cloud_devops")

    def test_all_keys_are_lowercase(self):
        for key in CATEGORY_MAP:
            self.assertEqual(
                key,
                key.lower(),
                f"Key {key!r} is not lowercase in CATEGORY_MAP",
            )

    def test_categories_are_known_values(self):
        valid_categories = {
            "programming", "framework", "database",
            "cloud_devops", "tools", "soft", "language", "education",
        }
        for key, value in CATEGORY_MAP.items():
            self.assertIn(value, valid_categories, f"Unknown category {value!r} for key {key!r}")
