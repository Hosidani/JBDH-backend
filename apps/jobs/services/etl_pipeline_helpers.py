"""
ETL Pipeline Helpers
====================
Alla hjälpfunktioner för ETL-pipelinen samlade på ett ställe.
etl_pipeline_service.py importerar härifrån och håller sig tunn.

Grupper:
  - Progress         : get_progress, save_progress
  - File / Download  : get_latest_timestamp_from_file, download_snapshot_or_stream_to_file
  - Data normalisation: normalize_json, parse_aware_datetime, _label_or_empty
  - Skills           : extract_skill_text, extract_skill_text_from_stored_value
  - Line of business : extract_line_of_business_from_raw
  - Vectors          : to_vector_or_none, build_vectors
  - Remote detection : detect_remote
  - Validation       : validate_raw_job
  - Transform        : transform_single_job
  - DB helpers       : flush_raw_batch, build_job_objects
  - Backfill         : backfill_missing_line_of_business, backfill_missing_job_vectors
"""

import json
import ijson
import logging
import os
import requests
from collections import defaultdict
from datetime import datetime
from decimal import Decimal

from django.db.models import Q
from django.utils import timezone

from apps.jobs.models import Job, RawJob
from core.utils import vectorize_job_data, vectorize_text

logger = logging.getLogger(__name__)

PROGRESS_FILE = "etl_progress.json"
SNAPSHOT_URL = "https://jobstream.api.jobtechdev.se/snapshot"
STREAM_URL = "https://jobstream.api.jobtechdev.se/stream"

JOB_UPDATE_FIELDS = [
    "title", "description", "company", "location", "posted_date", "webpage_url",
    "work_type", "employment_type", "line_of_business", "application_deadline",
    "required_skills", "text_vector", "description_vector", "location_vector",
    "line_of_business_vector", "title_vector", "competence_vector",
]

REMOTE_KEYWORDS = [
    "remote", "distans", "distansarbete", "på distans", "work from home", "hemifrån",
]


# ─── Progress ────────────────────────────────────────────────────────────────

def get_progress() -> dict:
    if os.path.exists(PROGRESS_FILE):
        with open(PROGRESS_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    return {"processed": 0, "total": 0, "last_batch": 0}


def save_progress(processed: int, total: int, last_batch: int) -> None:
    with open(PROGRESS_FILE, "w", encoding="utf-8") as f:
        json.dump(
            {"processed": processed, "total": total, "last_batch": last_batch},
            f, ensure_ascii=False, indent=2,
        )


# ─── File / Download ──────────────────────────────────────────────────────────

def get_latest_timestamp_from_file(json_file: str) -> str | None:
    if not os.path.exists(json_file):
        return None
    latest_ts = None
    try:
        with open(json_file, "rb") as f:
            for job in ijson.items(f, "item"):
                ts = job.get("timestamp") or job.get("publication_date")
                if not ts:
                    continue
                if isinstance(ts, int) or (isinstance(ts, str) and ts.isdigit()):
                    normalized_ts = datetime.fromtimestamp(int(ts) / 1000.0).strftime('%Y-%m-%dT%H:%M:%S')
                elif isinstance(ts, str):
                    normalized_ts = ts[:19]
                else:
                    continue
                if latest_ts is None or normalized_ts > latest_ts:
                    latest_ts = normalized_ts
    except Exception:
        logger.exception("Kunde inte läsa senaste timestamp från %s", json_file)
        return None
    return latest_ts


def download_snapshot_or_stream_to_file(
    filename: str,
    source: str = "snapshot",
    date: str | None = None,
    max_jobs: int | None = None,
) -> int:
    progress = get_progress()
    if (
        source == "snapshot"
        and progress.get("total", 0) > 0
        and os.path.exists(filename)
        and RawJob.objects.exists()
    ):
        logger.info(
            "⏭️ Skippar download - %s/%s redan processade",
            progress["processed"], progress["total"],
        )
        return progress.get("total", 0)

    url = SNAPSHOT_URL if source == "snapshot" else STREAM_URL
    if source == "stream" and date:
        url += f"?date={date}"

    logger.info("🔎 Hämtar källa: %s | URL: %s | Fil: %s", source.upper(), url, filename)
    print(f"📥 Laddar {source} till {filename}...")

    response = requests.get(url, timeout=1800, stream=True)
    response.raise_for_status()

    if not max_jobs:
        with open(filename, "wb") as f:
            for chunk in response.iter_content(chunk_size=1024 * 1024):
                if chunk:
                    f.write(chunk)
        total = 0
        with open(filename, "rb") as f:
            for _ in ijson.items(f, "item"):
                total += 1
        logger.info("✅ %s sparad till %s (%s jobb)", source.upper(), filename, total)
        print(f"✅ {source} sparad till {filename} ({total} jobb)")
        return total

    data = response.json()[:max_jobs]
    with open(filename, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    logger.info("✅ %s sparad till %s (%s jobb)", source.upper(), filename, len(data))
    print(f"✅ {source} sparad till {filename} ({len(data)} jobb)")
    return len(data)


# ─── Data normalisation ───────────────────────────────────────────────────────

def normalize_json(value):
    if isinstance(value, Decimal):
        return int(value) if value % 1 == 0 else float(value)
    if isinstance(value, dict):
        return {k: normalize_json(v) for k, v in value.items()}
    if isinstance(value, list):
        return [normalize_json(v) for v in value]
    return value


def parse_aware_datetime(value) -> datetime | None:
    if not value:
        return None
    try:
        dt = datetime.fromisoformat(str(value).replace('Z', '+00:00'))
        return timezone.make_aware(dt) if dt.tzinfo is None else dt
    except Exception:
        return None


def _label_or_empty(value) -> str:
    if isinstance(value, dict):
        return (value.get("label") or "").strip()
    if isinstance(value, str):
        return value.strip()
    return ""


# ─── Skills ───────────────────────────────────────────────────────────────────

def extract_skill_text(raw_data: dict) -> str:
    def _extract(items):
        if not isinstance(items, list):
            return []
        values = []
        for item in items:
            if isinstance(item, str):
                text = item.strip()
                if text:
                    values.append(text)
                continue
            if isinstance(item, dict):
                for key in ("label", "name", "skill", "value"):
                    v = item.get(key)
                    if isinstance(v, str) and v.strip():
                        values.append(v.strip())
                        break
        return values

    needs = _extract(raw_data.get("needs", []))
    requirements = _extract(raw_data.get("requirements", []))
    return " ".join(needs + requirements).strip()


def extract_skill_text_from_stored_value(required_skills) -> str:
    text = str(required_skills or "").strip()
    if not text:
        return ""
    try:
        parsed = json.loads(text)
    except Exception:
        return text
    if not isinstance(parsed, dict):
        return text
    return extract_skill_text(parsed)


# ─── Line of business ─────────────────────────────────────────────────────────

def extract_line_of_business_from_raw(raw_data: dict) -> str:
    return (
        _label_or_empty(raw_data.get("occupation_field"))
        or _label_or_empty(raw_data.get("occupation_group"))
        or _label_or_empty(raw_data.get("occupation"))
        or ""
    )


# ─── Vectors ──────────────────────────────────────────────────────────────────

def to_vector_or_none(text):
    normalized = " ".join(str(text or "").split())
    if not normalized:
        return None
    return vectorize_text(normalized, context="job") or None


def build_vectors(job_data: dict, raw_data: dict) -> dict:
    skill_text = extract_skill_text(raw_data)
    return {
        "text_vector": vectorize_job_data(job_data),
        "description_vector": to_vector_or_none(job_data.get("description")),
        "location_vector": to_vector_or_none(job_data.get("location")),
        "line_of_business_vector": to_vector_or_none(job_data.get("line_of_business")),
        "title_vector": to_vector_or_none(job_data.get("title")),
        "competence_vector": to_vector_or_none(skill_text),
    }


# ─── Remote detection ─────────────────────────────────────────────────────────

def detect_remote(raw_data: dict) -> bool:
    desc = raw_data.get("description") or {}
    full_text = " ".join(filter(None, [
        raw_data.get("headline", ""),
        desc.get("text", ""),
        desc.get("conditions", ""),
        desc.get("requirements", ""),
    ])).lower()
    return any(kw in full_text for kw in REMOTE_KEYWORDS)


# ─── Validation ───────────────────────────────────────────────────────────────

def validate_raw_job(raw_data: dict, job_id: str, seen_ids: set) -> str | None:
    """Returnerar skip-orsak som sträng, eller None om jobbet är OK."""
    required_fields = ["headline", "employer", "publication_date"]
    missing = [f for f in required_fields if not raw_data.get(f)]
    if missing:
        return f"missing_required_fields:{','.join(missing)}"
    if not isinstance(raw_data.get("publication_date"), str):
        return "bad_publication_date_type"
    if job_id in seen_ids:
        return "duplicate_in_batch"
    description = (raw_data.get("description") or {}).get("text", "")
    if not description or len(description.strip()) < 20:
        return "short_or_missing_description"
    if not (raw_data.get("workplace_address") or {}).get("municipality"):
        return "missing_location"
    try:
        datetime.fromisoformat(raw_data["publication_date"].replace('Z', '+00:00'))
    except Exception:
        return "bad_publication_date_format"
    return None


# ─── Transform ────────────────────────────────────────────────────────────────

def transform_single_job(raw_data: dict) -> dict:
    """Omvandlar ett råjobb-dict till ett Job-dict klart för databasen."""
    line_of_business = (
        _label_or_empty(raw_data.get("occupation_field"))
        or _label_or_empty(raw_data.get("occupation_group"))
        or _label_or_empty(raw_data.get("occupation"))
        or ""
    )
    base_work_type = _label_or_empty(raw_data.get("working_hours_type"))
    work_type = f"{base_work_type} | Distans".strip(" |") if detect_remote(raw_data) else base_work_type

    return {
        "title": raw_data.get("headline", ""),
        "description": (raw_data.get("description") or {}).get("text", ""),
        "company": (raw_data.get("employer") or {}).get("name", ""),
        "location": (raw_data.get("workplace_address") or {}).get("municipality", ""),
        "posted_date": parse_aware_datetime(raw_data.get("publication_date")),
        "webpage_url": raw_data.get("webpage_url"),
        "work_type": work_type,
        "employment_type": _label_or_empty(raw_data.get("employment_type")),
        "line_of_business": line_of_business,
        "application_deadline": parse_aware_datetime(raw_data.get("application_deadline")),
        "required_skills": json.dumps({
            "needs": raw_data.get("needs", []),
            "requirements": raw_data.get("requirements", []),
        }),
    }


# ─── DB helpers ───────────────────────────────────────────────────────────────

def flush_raw_batch(batch: list) -> int:
    """Skriver batchen till RawJob och rensar listan. Returnerar antal sparade."""
    if not batch:
        return 0
    RawJob.objects.bulk_create(
        batch,
        update_conflicts=True,
        update_fields=["raw_data"],
        unique_fields=["job_id"],
    )
    count = len(batch)
    batch.clear()
    return count


def build_job_objects(transformed_jobs: list) -> list:
    """Konverterar transformerade dicts till Job-instanser redo för bulk_create."""
    objects = []
    for job_data in transformed_jobs:
        job_data = dict(job_data)
        job_id = job_data.pop("job_id")
        objects.append(Job(job_id=job_id, **job_data))
    return objects


# ─── Backfill ─────────────────────────────────────────────────────────────────

def backfill_missing_line_of_business(limit: int = 500) -> int:
    jobs = list(
        Job.objects.filter(Q(line_of_business__isnull=True) | Q(line_of_business=""))
        .order_by("id")[:limit]
    )
    if not jobs:
        logger.info("🏷️ line_of_business backfill | nothing missing")
        return 0

    job_ids = [job.job_id for job in jobs]
    raw_map = {
        row["job_id"]: row["raw_data"]
        for row in RawJob.objects.filter(job_id__in=job_ids).values("job_id", "raw_data")
    }

    updated = 0
    for job in jobs:
        raw_data = raw_map.get(job.job_id) or {}
        lob = extract_line_of_business_from_raw(raw_data)
        if not lob:
            continue
        job.line_of_business = lob
        if job.line_of_business_vector is None:
            job.line_of_business_vector = to_vector_or_none(lob)
            job.save(update_fields=["line_of_business", "line_of_business_vector"])
        else:
            job.save(update_fields=["line_of_business"])
        updated += 1

    logger.info("🏷️ line_of_business backfill | scanned=%s updated=%s", len(jobs), updated)
    return updated


def backfill_missing_job_vectors(limit: int = 300) -> int:
    missing_filter = (
        (Q(description__isnull=False) & ~Q(description='') & Q(description_vector__isnull=True))
        | (Q(location__isnull=False) & ~Q(location='') & Q(location_vector__isnull=True))
        | (Q(line_of_business__isnull=False) & ~Q(line_of_business='') & Q(line_of_business_vector__isnull=True))
        | (Q(title__isnull=False) & ~Q(title='') & Q(title_vector__isnull=True))
        | (Q(required_skills__isnull=False) & Q(competence_vector__isnull=True))
    )
    jobs = list(Job.objects.filter(missing_filter).order_by("id")[:limit])
    if not jobs:
        logger.info("🧩 vector backfill | nothing missing")
        return 0

    update_fields = [
        "description_vector", "location_vector", "line_of_business_vector",
        "title_vector", "competence_vector",
    ]
    updated = 0
    for job in jobs:
        skill_text = extract_skill_text_from_stored_value(job.required_skills)
        changed = False
        if job.description_vector is None:
            job.description_vector = to_vector_or_none(job.description)
            changed = True
        if job.location_vector is None:
            job.location_vector = to_vector_or_none(job.location)
            changed = True
        if job.line_of_business_vector is None:
            job.line_of_business_vector = to_vector_or_none(job.line_of_business)
            changed = True
        if job.title_vector is None:
            job.title_vector = to_vector_or_none(job.title)
            changed = True
        if job.competence_vector is None:
            job.competence_vector = to_vector_or_none(skill_text)
            changed = True
        if changed:
            job.save(update_fields=update_fields)
            updated += 1

    logger.info("🧩 vector backfill | scanned=%s updated=%s", len(jobs), updated)
    return updated
