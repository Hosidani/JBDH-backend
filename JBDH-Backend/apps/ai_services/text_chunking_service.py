# apps/ai_services/text_chunking_service.py
"""
Chunk job ad text for RAG-style extraction (mirrors .NET TextChunkingService).
"""

import logging
from typing import List

logger = logging.getLogger(__name__)


def chunk_text(text: str, max_words: int = 200, overlap: int = 40) -> List[str]:
    if not text or not text.strip():
        return []
    words = text.split()
    chunks = []
    i = 0
    while i < len(words):
        end = min(i + max_words, len(words))
        chunks.append(" ".join(words[i:end]))
        if end >= len(words):
            break
        i += max_words - overlap
    return chunks


def chunk_job_ad(job_ad_text: str, max_words: int = 150, overlap: int = 50) -> List[str]:
    """Chunk job ad for requirement extraction (smaller chunks, more overlap)."""
    if not job_ad_text or not job_ad_text.strip():
        logger.warning("ChunkJobAd received empty job ad text.")
        return []
    return chunk_text(job_ad_text, max_words=max_words, overlap=overlap)
