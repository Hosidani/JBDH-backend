import os
from typing import List, Optional

import requests


MINILM_URL = os.getenv("MINILM_URL", "http://minilm:8000")


def embed_texts(texts: List[str], context: Optional[str] = None) -> List[List[float]]:
    payload: dict = {"texts": texts}
    if context:
        payload["context"] = context

    response = requests.post(
        f"{MINILM_URL}/embed",
        json=payload,
        timeout=30,
    )
    response.raise_for_status()
    data = response.json()
    embeddings = data.get("embeddings") or []
    return embeddings

