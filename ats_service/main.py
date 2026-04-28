from __future__ import annotations

from typing import Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from ats_service.parser import parse_cv, parse_job
from ats_service.scorer import score

app = FastAPI(title="ATS Service", version="1.0.0")

# ---------------------------------------------------------------------------
# Check that spaCy model loaded properly (detected at import time in parser.py)
# ---------------------------------------------------------------------------
try:
    import spacy
    spacy.load("en_core_web_sm")
    _MODEL_READY = True
    _MODEL_NAME = "en_core_web_sm"
except OSError:
    try:
        spacy.load("sv_core_news_sm")
        _MODEL_READY = True
        _MODEL_NAME = "sv_core_news_sm"
    except OSError:
        _MODEL_READY = False
        _MODEL_NAME = "none"


# ---------------------------------------------------------------------------
# Request / Response models
# ---------------------------------------------------------------------------

class ParseRequest(BaseModel):
    text: str


class ScoreRequest(BaseModel):
    cv_text: str
    job_text: str


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------

@app.get("/health")
def health() -> dict:
    return {
        "status": "ok" if _MODEL_READY else "degraded",
        "model": _MODEL_NAME,
    }


@app.post("/parse/cv")
def parse_cv_endpoint(req: ParseRequest) -> dict:
    if not req.text.strip():
        raise HTTPException(status_code=422, detail="CV text får inte vara tomt.")
    return parse_cv(req.text)


@app.post("/parse/job")
def parse_job_endpoint(req: ParseRequest) -> dict:
    if not req.text.strip():
        raise HTTPException(status_code=422, detail="Job text får inte vara tomt.")
    return parse_job(req.text)


@app.post("/score")
def score_endpoint(req: ScoreRequest) -> dict:
    if not req.cv_text.strip() or not req.job_text.strip():
        raise HTTPException(
            status_code=422,
            detail="Både cv_text och job_text krävs.",
        )
    cv_parsed = parse_cv(req.cv_text)
    job_parsed = parse_job(req.job_text)
    result = score(cv_parsed, job_parsed)
    result["cv_parsed"] = cv_parsed
    result["job_parsed"] = job_parsed
    return result
