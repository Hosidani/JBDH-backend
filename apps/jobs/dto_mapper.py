import json
from typing import Any

from apps.jobs.models import Job, SavedJob


def _to_skill_list(value: Any) -> list[str]:
    if value is None:
        return []

    if isinstance(value, list):
        return [str(item).strip() for item in value if str(item).strip()]

    text = str(value).strip()
    if not text:
        return []

    if text.startswith("[") and text.endswith("]"):
        try:
            parsed = json.loads(text)
            if isinstance(parsed, list):
                return [str(item).strip() for item in parsed if str(item).strip()]
        except (json.JSONDecodeError, TypeError):
            pass

    normalized = text.replace("\n", ",").replace(";", ",")
    return [item.strip() for item in normalized.split(",") if item.strip()]


def build_job_details_dto(
    *,
    job: Job,
    has_applied: bool,
    is_saved: bool,
    is_not_interested: bool,
    notes: str | None,
) -> dict:
    required_skills = _to_skill_list(job.required_skills)

    return {
        "jobId": job.id,
        "title": job.title,
        "description": job.description,
        "company": job.company,
        "location": job.location,
        "postedDate": job.posted_date,
        "webpageUrl": job.webpage_url,
        "workType": job.work_type,
        "employmentType": job.employment_type,
        "externalJobId": job.job_id,
        "applicationDeadline": job.application_deadline,
        "requiredSkills": required_skills,
        "requiredSoftSkills": [],
        "isSaved": is_saved,
        "hasApplied": has_applied,
        "isNotInterested": is_not_interested,
        "notes": notes,
        "savedJobNotes": notes,
    }


def build_saved_job_dto(*, saved_job: SavedJob, has_applied: bool, is_not_interested: bool) -> dict:
    return build_job_details_dto(
        job=saved_job.job,
        has_applied=has_applied,
        is_saved=True,
        is_not_interested=is_not_interested,
        notes=saved_job.notes,
    )
