# apps/ai_services/job_analysis_service.py
"""
Extract job requirements and profession from job ad text (chunked, deduplicated).
Mirrors .NET JobAIAnalysisService.ExtractJobRequirementsAndProfessionAsync.
"""
import logging
import re
from typing import List, Tuple, Optional

from apps.ai_services.ai_client import AIClient
from apps.ai_services.ai_message_templates import AIMessageTemplates
from apps.ai_services.ai_models import RequirementExtractionResponse, RequirementItem, Profession
from apps.ai_services.text_chunking_service import chunk_job_ad
from apps.ai_services.cv_analysis_service import _repair_truncated_requirement_json

logger = logging.getLogger(__name__)

COMPARABLE_CATEGORIES = {"skill", "experience", "language", "degree", "certification"}


def extract_json_from_code_block(text: str) -> str:
    if not text or "```json" not in text:
        return (text or "").strip()
    start = text.find("```json") + 7
    end = text.find("```", start)
    if end > start:
        return text[start:end].strip()
    return text.strip()


def extract_job_requirements_and_profession(job_ad_text: str) -> Tuple[List[RequirementItem], Optional[Profession]]:
    """
    Chunk job ad, call AI per chunk, merge and deduplicate requirements, filter by category.
    Returns (requirements, job_profession).
    """
    if not job_ad_text or not job_ad_text.strip():
        logger.warning("Job ad text is empty or null")
        return [], None

    chunks = chunk_job_ad(job_ad_text)
    requirements: List[RequirementItem] = []
    job_profession: Optional[Profession] = None

    for chunk in chunks:
        prompt = AIMessageTemplates.USER_MESSAGE_TEMPLATES[8].format(JobAdText=chunk)
        try:
            ai_result = AIClient.call_deepseek(prompt)
            response_text = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
            if not response_text:
                continue
            clean = extract_json_from_code_block(response_text)
            clean = re.sub(r"```(json)?", "", clean).strip()
            try:
                extracted = RequirementExtractionResponse.from_json(clean)
            except Exception:
                repaired = _repair_truncated_requirement_json(clean)
                try:
                    extracted = RequirementExtractionResponse.from_json(repaired) if repaired else None
                except Exception:
                    extracted = None
            if extracted and extracted.requirements:
                requirements.extend(extracted.requirements)
            if job_profession is None and extracted and extracted.profession:
                job_profession = extracted.profession
        except Exception as e:
            logger.warning("Job ad chunk parsing failed: %s", e)

    if not requirements:
        logger.warning("No requirements extracted from job ad")
        return [], job_profession

    # Deduplicate by name (lowercase)
    seen = set()
    deduped = []
    for r in requirements:
        key = (r.name or "").strip().lower()
        if key and key not in seen:
            seen.add(key)
            deduped.append(r)
    requirements = deduped

    # Filter only comparable categories
    requirements = [r for r in requirements if (r.category or "").strip().lower() in COMPARABLE_CATEGORIES]

    return requirements, job_profession
