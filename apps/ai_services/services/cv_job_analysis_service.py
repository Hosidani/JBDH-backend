from apps.ai_services.infrastructure.ai_client import AIClient
from apps.ai_services.infrastructure.ai_message_templates import AIMessageTemplates
import json
import re
import logging

def extract_cv_job_analysis(cv_text: str, job_ad_text: str) -> dict | None:
    """
    Kombinerad analys av CV och jobbannons med template 2.
    Returnerar ett dict med analysresultat eller None vid fel.
    """
    logger = logging.getLogger(__name__)
    prompt = AIMessageTemplates.USER_MESSAGE_TEMPLATES[5].format(cv_text=cv_text, job_ad_text=job_ad_text)

    retry_suffix = (
        "\n\nIMPORTANT: Return ONLY valid and complete JSON. "
        "No markdown, no explanations, no code fences. "
        "If output is long, shorten list items but keep valid JSON."
    )

    attempts = [
        {"prompt": prompt, "max_tokens": 4000, "temperature": 0.4},
        {"prompt": prompt + retry_suffix, "max_tokens": 6000, "temperature": 0.2},
    ]

    for index, attempt in enumerate(attempts, start=1):
        logger.info("cv_job_analysis attempt %s/%s", index, len(attempts))
        ai_result = AIClient.call_deepseek(
            attempt["prompt"],
            max_tokens=attempt["max_tokens"],
            temperature=attempt["temperature"],
        )
        logger.info(f"DeepSeek full response: {ai_result!r}")

        finish_reason = ai_result.get("choices", [{}])[0].get("finish_reason", "")
        if finish_reason == "length":
            logger.warning("DeepSeek response truncated (finish_reason=length) on attempt %s", index)

        response_text = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
        if not response_text or not response_text.strip():
            logger.error("DeepSeek returned empty response on attempt %s.", index)
            continue

        clean = re.sub(r"```(json)?", "", response_text).strip()
        try:
            return json.loads(clean)
        except Exception as ex:
            logger.warning(f"JSON parse error on attempt {index}: {ex}")

    logger.error("cv_job_analysis failed after %s attempts", len(attempts))
    return None


def extract_field_analysis(
    cv_profession: str,
    cv_skills: str,
    job_profession: str,
    job_requirements: str,
) -> dict | None:
    """
    Template 6: field analysis — pure data, no compatibility judgement.
    Returns field_overlap_score, field_distance_note, match_evidence,
    critical_gaps, transferable_skills — or None on failure.
    """
    logger = logging.getLogger(__name__)
    prompt = AIMessageTemplates.USER_MESSAGE_TEMPLATES[6].format(
        cv_profession=cv_profession,
        cv_skills=cv_skills,
        job_profession=job_profession,
        job_requirements=job_requirements,
    )

    attempts = [
        {"prompt": prompt, "max_tokens": 1500, "temperature": 0.1},
        {"prompt": prompt, "max_tokens": 2000, "temperature": 0.0},
    ]

    for index, attempt in enumerate(attempts, start=1):
        logger.info("field_analysis attempt %s/%s", index, len(attempts))
        ai_result = AIClient.call_deepseek(
            attempt["prompt"],
            max_tokens=attempt["max_tokens"],
            temperature=attempt["temperature"],
        )
        response_text = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
        if not response_text or not response_text.strip():
            logger.warning("DeepSeek returned empty response on attempt %s", index)
            continue

        clean = re.sub(r"```(json)?", "", response_text).strip()
        try:
            return json.loads(clean)
        except Exception as ex:
            logger.warning("JSON parse error on attempt %s: %s", index, ex)

    logger.error("extract_field_analysis failed after %s attempts", len(attempts))
    return None