import os
from typing import Dict

import requests

ATS_URL = os.getenv("ATS_URL", "http://ats:8001")
_TIMEOUT = 30


def parse_cv_ats(text: str) -> Dict:
    """
    Call ATS service to extract structured data from CV text.
    Returns parsed dict or raises on HTTP error.
    """
    response = requests.post(
        f"{ATS_URL}/parse/cv",
        json={"text": text},
        timeout=_TIMEOUT,
    )
    response.raise_for_status()
    return response.json()


def parse_job_ats(text: str) -> Dict:
    """
    Call ATS service to extract structured requirements from a job ad.
    """
    response = requests.post(
        f"{ATS_URL}/parse/job",
        json={"text": text},
        timeout=_TIMEOUT,
    )
    response.raise_for_status()
    return response.json()


def score_cv_job_ats(cv_text: str, job_text: str) -> Dict:
    """
    Call ATS service to compute ATS score + breakdown.

    Returns:
        total_score       (0–100)
        breakdown         dict of component scores
        matched_skills    list
        missing_skills    list
        bonus_skills      list
        recommendations   list of Swedish strings
        cv_parsed         full CV parse result
        job_parsed        full job parse result
    """
    response = requests.post(
        f"{ATS_URL}/score",
        json={"cv_text": cv_text, "job_text": job_text},
        timeout=_TIMEOUT,
    )
    response.raise_for_status()
    return response.json()
