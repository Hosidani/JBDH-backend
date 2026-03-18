from apps.ai_services.ai_client import AIClient
from apps.ai_services.ai_message_templates import AIMessageTemplates
import json
import re
import logging

def extract_cv_job_analysis(cv_text: str, job_ad_text: str) -> dict | None:
    """
    Kombinerad analys av CV och jobbannons med template 2.
    Returnerar ett dict med analysresultat eller None vid fel.
    """
    prompt = AIMessageTemplates.USER_MESSAGE_TEMPLATES[2].format(JobAdText=job_ad_text, CVText=cv_text)
    ai_result = AIClient.call_deepseek(prompt)
    logger = logging.getLogger(__name__)
    logger.info(f"DeepSeek full response: {ai_result!r}")
    response_text = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
    if not response_text or not response_text.strip():
        logger.error("DeepSeek returned empty response.")
        return None
    clean = re.sub(r"```(json)?", "", response_text).strip()
    try:
        return json.loads(clean)
    except Exception as ex:
        logger.error(f"JSON parse error: {ex}")
        return None