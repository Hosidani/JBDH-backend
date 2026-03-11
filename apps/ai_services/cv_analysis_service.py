# apps/ai_services/cv_analysis_service.py

import logging
import json
import re
from apps.ai_services.ai_client import AIClient
from apps.ai_services.ai_message_templates import AIMessageTemplates
from apps.ai_services.user_ai_response_service import UserAIResponseService
from apps.ai_services.ai_models import CVAnalysisResult, RequirementExtractionResponse

logger = logging.getLogger("CVAnalysisService")


def _repair_truncated_requirement_json(raw: str) -> str | None:
    """
    When the AI response is truncated (e.g. mid-string like "name": "Moq),
    try to cut at the last complete requirement object and close the JSON.
    Returns repaired string or None if repair not possible.
    """
    # Last complete requirement ends with "importance": N followed by newline and }
    match = list(re.finditer(r'"importance"\s*:\s*\d+\s*\}\s*,?\s*', raw))
    if not match:
        return None
    last = match[-1]
    # Truncate after this requirement's closing brace
    cut = raw[: last.end()].rstrip()
    # Remove trailing comma if present so JSON is valid
    if cut.rstrip().endswith(","):
        cut = cut.rstrip()[:-1]
    # Close requirements array and root object
    if '"requirements"' in cut or '"Requirements"' in cut:
        cut += "\n    ]\n}"
    else:
        cut += "\n}"
    return cut


def extract_json_from_code_block(text):
    """Extract JSON from code block if present."""
    if "```json" in text:
        start = text.find("```json") + 7
        end = text.find("```", start)
        if end > start:
            return text[start:end].strip()
    return text.strip()

class CVAnalysisService:
    @staticmethod
    def _attempt_repair_json(raw: str) -> str | None:
        if not raw or not raw.strip():
            return None

        def _close_json(candidate: str) -> str | None:
            stack = []
            in_string = False
            escape = False

            for ch in candidate:
                if in_string:
                    if escape:
                        escape = False
                    elif ch == "\\":
                        escape = True
                    elif ch == '"':
                        in_string = False
                else:
                    if ch == '"':
                        in_string = True
                    elif ch in "[{":
                        stack.append(ch)
                    elif ch == "]":
                        if not stack or stack[-1] != "[":
                            return None
                        stack.pop()
                    elif ch == "}":
                        if not stack or stack[-1] != "{":
                            return None
                        stack.pop()

            suffix = ""
            if in_string:
                suffix += '"'

            while stack:
                opener = stack.pop()
                suffix += "]" if opener == "[" else "}"

            repaired = (candidate + suffix).strip()
            repaired = re.sub(r",\s*([}\]])", r"\1", repaired)
            return repaired

        text = raw.strip()
        max_trim = min(len(text), 2500)
        for trim in range(0, max_trim + 1):
            candidate = text[: len(text) - trim].rstrip()
            if not candidate:
                break

            repaired = _close_json(candidate)
            if not repaired:
                continue

            try:
                json.loads(repaired)
                return repaired
            except Exception:
                continue

        return None

    def _request_and_parse_cv_analysis(self, prompt, language):
        retry_prompt_suffix = (
            "\n\nIMPORTANT: Return ONLY valid and complete JSON with top-level keys English and Swedish. "
            "No markdown, no explanations, no code fences. "
            "If output gets long, shorten list items but keep valid JSON."
        )

        attempts = [
            {"prompt": prompt, "max_tokens": 1400, "temperature": 0.4},
            {"prompt": prompt + retry_prompt_suffix, "max_tokens": 2400, "temperature": 0.2},
        ]

        last_error = None
        last_cleaned_response = ""

        for index, attempt in enumerate(attempts, start=1):
            logger.info("CV analysis attempt %s/%s", index, len(attempts))

            ai_result = AIClient.call_deepseek(
                attempt["prompt"],
                language=language,
                max_tokens=attempt["max_tokens"],
                temperature=attempt["temperature"],
            )

            response_text = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
            if not response_text or not response_text.strip():
                last_error = ValueError("AI returned empty response")
                continue

            cleaned_response = extract_json_from_code_block(response_text)
            cleaned_response = re.sub(r'```(json)?', '', cleaned_response).strip()
            last_cleaned_response = cleaned_response

            try:
                analysis_result = CVAnalysisResult.from_json(cleaned_response)
                return cleaned_response, analysis_result
            except Exception as ex:
                repaired = self._attempt_repair_json(cleaned_response)
                if repaired:
                    try:
                        analysis_result = CVAnalysisResult.from_json(repaired)
                        logger.info("CV analysis JSON repaired successfully on attempt %s", index)
                        return repaired, analysis_result
                    except Exception:
                        pass

                last_error = ex
                logger.warning("CV analysis parse failed on attempt %s: %s", index, ex)

        if last_error:
            raise ValueError(f"Failed to deserialize AI response: {last_error}")

        raise ValueError("AI response is not valid JSON or is empty")

    def analyze_and_save_cv(self, user, cv_text, language):
        logger.info("analyze_and_save_cv started")
        logger.info(f"Language argument received in analyze_and_save_cv: {language} (type: {type(language)})")
        # Log template BEFORE formatting
        template = AIMessageTemplates.USER_MESSAGE_TEMPLATES[0]
        logger.info(f"Using template[0] (truncated): {template[:300]}")
        try:
            prompt = template.format(CVText=cv_text, Language=language)
            logger.info(f"Prompt SUCCESSFULLY formatted (truncated): {prompt[:500]}")
        except KeyError as e:
            logger.error(f"KeyError in .format(): {e}")
            logger.error(f"Available keys in template: {self._find_placeholders(template)}")
            raise  # Re-raise for full stack trace
        logger.info("Arguments to .format(): %s", {"CVText": cv_text, "Language": language})
        cleaned_response, analysis_result = self._request_and_parse_cv_analysis(prompt, language)
        logger.info(f"Cleaned AI response (before save): {cleaned_response[:1000]}")
        
        # Save only validated JSON response.
        saved_response = None
        try:
            saved_response = UserAIResponseService().save_user_ai_response(user, cleaned_response)
            logger.info(f"Saved AI response with ID: {saved_response.id}")
        except Exception as save_ex:
            logger.error(f"Failed to save AI response: {save_ex}")
        
        if analysis_result:
            logger.info(f"AI CVAnalysisResult for user {getattr(user, 'id', None)}: "
                        f"BestJobTitlesForUser: {analysis_result.best_job_titles_for_user}; "
                        f"HardSkills: {analysis_result.hard_skills}; "
                        f"SoftSkills: {analysis_result.soft_skills}; "
                        f"Experiences: {analysis_result.experiences}; "
                        f"EducationAndCertifications: {analysis_result.education_and_certifications}")
        else:
            logger.error("Deserialized CVAnalysisResult is null.")
            raise ValueError("Deserialized CVAnalysisResult is null")

        return {
            "success": True,
            "savedResponseId": getattr(saved_response, "id", None),
        }

    def _find_placeholders(self, template_str):
        """Hitta alla placeholders i en format-sträng."""
        import re
        return re.findall(r'\{([^}]+)\}', template_str)

    def extract_cv_profession_and_requirements(self, cv_text):
        if not cv_text or not cv_text.strip():
            logger.warning("CV text is empty or null")
            return None

        # Template 7 uses {JobAdText} placeholder for the text to analyze (CV or job)
        prompt = AIMessageTemplates.USER_MESSAGE_TEMPLATES[7].format(JobAdText=cv_text)
        ai_result = AIClient.call_deepseek(prompt)
        response_text = ai_result.get("choices", [{}])[0].get("message", {}).get("content", "")
        logger.info(f"CV extraction response: {response_text}")

        clean_response = extract_json_from_code_block(response_text)
        clean_response = re.sub(r'```(json)?', '', clean_response).strip()
        logger.info(f"Cleaned extraction response: {clean_response}")

        cv_extracted = None
        try:
            cv_extracted = RequirementExtractionResponse.from_json(clean_response)
        except (json.JSONDecodeError, Exception) as ex:
            logger.warning(f"CV extraction parse failed (possibly truncated): {ex}, attempting repair")
            repaired = _repair_truncated_requirement_json(clean_response)
            if repaired:
                try:
                    cv_extracted = RequirementExtractionResponse.from_json(repaired)
                    logger.info("Repaired truncated CV extraction JSON successfully")
                except Exception as ex2:
                    logger.error(f"CV extraction repair also failed: {ex2}")
                    return None
            else:
                logger.error(f"CV extraction threw exception and could not repair: {ex}")
                return None

        if cv_extracted:
            logger.info(f"Profession: {cv_extracted.profession}")
            logger.info(f"Requirements: {cv_extracted.requirements}")
        else:
            logger.warning("CV extraction failed or returned empty result.")
        return cv_extracted