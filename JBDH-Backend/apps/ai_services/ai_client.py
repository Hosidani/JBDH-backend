import requests
from .ai_config import AIConfig


class AIClientError(RuntimeError):
    def __init__(self, status_code: int, message: str):
        super().__init__(message)
        self.status_code = status_code

class AIClient:
    @staticmethod
    def _extract_safe_error_message(status_code, error_body):
        if status_code == 401:
            return "DeepSeek authentication failed. Check DEEPSEEK_API_KEY."

        if isinstance(error_body, dict):
            message = (
                error_body.get("error", {}).get("message")
                or error_body.get("message")
                or "DeepSeek request failed"
            )
            return str(message)

        if isinstance(error_body, str) and error_body.strip():
            return error_body[:300]

        return "DeepSeek request failed"

    @staticmethod
    def call_deepseek(prompt, language="English", max_tokens=None, temperature=None):
        if not AIConfig.DEEPSEEK_API_KEY:
            raise AIClientError(401, "DeepSeek authentication failed. Check DEEPSEEK_API_KEY.")

        headers = {
            "Authorization": f"Bearer {AIConfig.DEEPSEEK_API_KEY}",
            "Content-Type": "application/json",
        }
        payload = {
            "model": AIConfig.DEEPSEEK_MODEL,
            "messages": [{"role": "user", "content": prompt}],
            "max_tokens": max_tokens if max_tokens is not None else AIConfig.MAX_TOKENS,
            "temperature": temperature if temperature is not None else AIConfig.TEMPERATURE,
            "top_p": AIConfig.TOP_P,
        }
        response = requests.post(AIConfig.DEEPSEEK_URL, json=payload, headers=headers, timeout=90)

        if response.status_code >= 400:
            try:
                error_body = response.json()
            except Exception:
                error_body = response.text

            safe_message = AIClient._extract_safe_error_message(response.status_code, error_body)
            raise AIClientError(response.status_code, safe_message)

        return response.json()

    @staticmethod
    def call_gemini_embedding(text):
        headers = {"Authorization": f"Bearer {AIConfig.GEMINI_API_KEY}"}
        payload = {"model": AIConfig.GEMINI_EMBEDDING_MODEL, "input": text}
        response = requests.post(AIConfig.GEMINI_EMBEDDING_URL, json=payload, headers=headers)
        return response.json()