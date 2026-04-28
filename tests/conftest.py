"""
conftest.py for the ats container.

The ats container has no Django installed. This file mocks the django.test
module so that test files using SimpleTestCase work with stdlib unittest.TestCase
without any changes.
"""

import sys
import unittest
from unittest.mock import MagicMock

if "django" not in sys.modules:
    django_mock = MagicMock()
    django_mock.test.SimpleTestCase = unittest.TestCase
    sys.modules["django"] = django_mock
    sys.modules["django.test"] = django_mock.test
