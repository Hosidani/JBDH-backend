from typing import List, Optional

from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer


app = FastAPI()

MODEL_NAME = "sentence-transformers/all-MiniLM-L6-v2"
model = SentenceTransformer(MODEL_NAME)


class EmbedRequest(BaseModel):
    texts: List[str]
    context: Optional[str] = None


class EmbedResponse(BaseModel):
    embeddings: List[List[float]]


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "model": MODEL_NAME}


@app.post("/embed", response_model=EmbedResponse)
def embed(req: EmbedRequest) -> EmbedResponse:
    embeddings = model.encode(req.texts, convert_to_numpy=True).tolist()
    return EmbedResponse(embeddings=embeddings)

