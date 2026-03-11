import os

class AIConfig:
    DEEPSEEK_API_KEY = os.getenv("DEEPSEEK_API_KEY", "")
    DEEPSEEK_MODEL = os.getenv("DEEPSEEK_MODEL", "deepseek-chat")
    DEEPSEEK_URL = os.getenv("DEEPSEEK_URL", "https://api.deepseek.com/v1/chat/completions")

    GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "")
    GEMINI_EMBEDDING_MODEL = os.getenv("GEMINI_EMBEDDING_MODEL", "gemini-embedding-001")
    GEMINI_EMBEDDING_URL = os.getenv("GEMINI_EMBEDDING_URL", "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent")

    MAX_TOKENS = 1000
    TEMPERATURE = 0.7
    TOP_P = 1.0