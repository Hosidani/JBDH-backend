import json
import logging
import os
import time
from typing import List, Tuple, Optional, Dict

import requests
from django.conf import settings
from django.db import transaction
from django.db.models import Q

from apps.jobs.models import Job
from apps.ai_services.models import JobAIResponse
from apps.ai_services.infrastructure.ai_message_templates import AIMessageTemplates

logger = logging.getLogger(__name__)


class JobAIAnalysisService:
    """
    Django version of .NET JobAIAnalysisService.
    Runs job-ad AI analysis and stores result in job_ai_responses.
    """

    def __init__(self):
        # Read AI provider configuration from Django settings (fallback to env vars).
        self.api_url = getattr(settings, "DEEPSEEK_URL", os.getenv("DEEPSEEK_URL", ""))
        self.api_key = getattr(settings, "DEEPSEEK_API_KEY", os.getenv("DEEPSEEK_API_KEY", ""))
        self.model = getattr(settings, "DEEPSEEK_MODEL", os.getenv("DEEPSEEK_MODEL", "deepseek-chat"))

    # TODo: In future, we want to support update of existing analysis (currently only missing/incomplete are processed). When a job is updated in the system, we want to be able to re-run the analysis and update the existing JobAIResponse row instead of creating a new one. This method should be flexible enough to support both "get candidates for new analysis" and "get candidates for update" modes.
    def get_all_job_urls_from_database(
        self,
        keyword: Optional[str] = None,
        max_jobs: Optional[int] = None,
        only_failed: bool = False,
        target_job_id: Optional[int] = None,
        missing_only: bool = True,
    ) -> List[Tuple[int, str, str, str]]:
        # Base queryset: jobs that have a usable URL.
        qs = (
            Job.objects
            .exclude(webpage_url__isnull=True)
            .exclude(webpage_url__exact="")
            .values_list("job_id", "webpage_url", "title", "description")
        )

        # Optional exact job selection for manual/frontend targeted analysis.
        if target_job_id is not None:
            qs = (
                Job.objects
                .exclude(webpage_url__isnull=True)
                .exclude(webpage_url__exact="")
                .filter(job_id=str(target_job_id))
                .values_list("job_id", "webpage_url", "title", "description")
            )

        # Optional title filter for flexible manual runs.
        if keyword:
            qs = (
                Job.objects
                .exclude(webpage_url__isnull=True)
                .exclude(webpage_url__exact="")
                .filter(
                    Q(title__icontains=keyword)
                    | Q(description__icontains=keyword)
                    | Q(required_skills__icontains=keyword)
                )
                .values_list("job_id", "webpage_url", "title", "description")
            )

        # Optional hard limit to cap processing time/API cost in manual tests.
        if max_jobs is not None:
            qs = qs[:max_jobs]

        # Optional retry mode: only process jobs that currently have FAILED status.
        failed_ids = None
        if only_failed:
            failed_ids = set(
                JobAIResponse.objects.filter(status=JobAIResponse.JobStatus.FAILED)
                .values_list("job_id", flat=True)
            )

        # Build existing analysis map to support missing-only filtering.
        analysis_map: Dict[int, Tuple[str, str]] = {}
        if missing_only:
            for jid, status, response_text in JobAIResponse.objects.values_list("job_id", "status", "response_text"):
                analysis_map[int(jid)] = (status or "", (response_text or "").strip())

        result: List[Tuple[int, str, str, str]] = []
        for job_id, url, title, description in qs:
            # Convert external job ids to integer key used by JobAIResponse.
            try:
                job_id_int = int(job_id)
            except (TypeError, ValueError):
                continue

            # Normalize URL and skip obviously invalid placeholders.
            u = (url or "").strip()
            if u.lower() in {"unknown url", "n/a"}:
                continue
            if not u.startswith("http"):
                continue

            # When only_failed is enabled, only keep jobs that are marked FAILED.
            if failed_ids is not None:
                try:
                    if job_id_int not in failed_ids:
                        continue
                except (TypeError, ValueError):
                    continue

            # Missing-only mode keeps rows without response or with incomplete/failed status.
            if missing_only:
                existing = analysis_map.get(job_id_int)
                if existing:
                    existing_status, existing_payload = existing
                    if (
                        existing_status == JobAIResponse.JobStatus.COMPLETED
                        and existing_payload
                        and existing_payload not in {"", "{}"}
                    ):
                        continue

            result.append((job_id_int, u, (title or "").strip(), (description or "").strip()))
        return result

    def _extract_inner_json(self, text: str) -> str:
        # DeepSeek may return fenced markdown; normalize to a JSON string.
        if not text:
            return "{}"
        t = text.strip()
        if "```json" in t:
            start = t.find("```json") + 7
            end = t.rfind("```")
            if end > start:
                t = t[start:end].strip()
        elif "```" in t:
            start = t.find("```") + 3
            end = t.rfind("```")
            if end > start:
                t = t[start:end].strip()

        try:
            json.loads(t)
            return t
        except Exception:
            return json.dumps({"raw": t}, ensure_ascii=False)

    def _build_job_ad_text(
        self,
        title: str,
        description: str,
        webpage_url: str,
        keyword: Optional[str] = None,
    ) -> str:
        parts = []
        if title:
            parts.append(f"Title: {title}")
        if description:
            parts.append(f"Description:\n{description}")
        if webpage_url:
            parts.append(f"Source URL: {webpage_url}")
        if keyword:
            parts.append(f"Focus keyword for this run: {keyword}")
        return "\n\n".join(parts).strip()

    def _call_ai_with_retry(self, job_ad_text: str, max_retries: int = 5) -> Optional[str]:
        # Validate provider configuration before attempting requests.
        if not self.api_url or not self.api_key:
            logger.error("DeepSeek config missing (DEEPSEEK_URL / DEEPSEEK_API_KEY).")
            return None

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
        }

        payload = {
            "model": self.model,
            "messages": [
                {
                    "role": "system",
                    "content": "You are an expert HR analyst. Return JSON only without markdown fences."
                },
                {
                    "role": "user",
                    "content": AIMessageTemplates.USER_MESSAGE_TEMPLATES[1].replace("{JobAdText}", job_ad_text),
                },
            ],
            "temperature": 0.2,
        }

        for attempt in range(1, max_retries + 1):
            try:
                # Execute request with timeout and retry on transient provider errors.
                resp = requests.post(self.api_url, headers=headers, json=payload, timeout=120)
                if resp.status_code in (429, 500, 502, 503):
                    raise requests.HTTPError(f"{resp.status_code} {resp.text[:200]}")
                resp.raise_for_status()

                data = resp.json()
                content = (
                    data.get("choices", [{}])[0]
                    .get("message", {})
                    .get("content", "")
                )
                return self._extract_inner_json(content)

            except Exception as ex:
                if attempt == max_retries:
                    logger.error("AI request failed after retries: %s", ex)
                    return None
                # Exponential backoff between retries.
                sleep_s = (2 ** attempt)
                logger.warning("Retry %s/%s in %ss: %s", attempt, max_retries, sleep_s, ex)
                time.sleep(sleep_s)

        return None

    @transaction.atomic
    def process_single_job(
        self,
        job_id: int,
        webpage_url: str,
        title: str,
        description: str,
        keyword: Optional[str] = None,
    ) -> str:
        # Ensure one persistent row per job_id (idempotent pattern).
        row, _ = JobAIResponse.objects.get_or_create(
            job_id=job_id,
            defaults={
                "response_text": "{}",
                "status": JobAIResponse.JobStatus.PENDING,
                "attempt_count": 0,
            },
        )

        # Skip if already completed with a non-empty payload. TODO: In future, we want to support update of existing analysis (currently only missing/incomplete are processed). When a job is updated in the system, we want to be able to re-run the analysis and update the existing JobAIResponse row instead of creating a new one. This check should be flexible enough to support both "get candidates for new analysis" and "get candidates for update" modes.
        if (
            row
            and row.status == JobAIResponse.JobStatus.COMPLETED
            and row.response_text
            and row.response_text.strip() not in {"", "{}"}
        ):
            return "skipped"

        # Mark processing state and reset error before new attempt.
        row.status = JobAIResponse.JobStatus.PROCESSING
        row.attempt_count += 1
        row.error_message = None
        if not row.response_text:
            row.response_text = "{}"
        row.save(update_fields=["status", "attempt_count", "error_message", "response_text", "updated_at"])

        # Execute AI call and persist final state.
        job_ad_text = self._build_job_ad_text(
            title=title,
            description=description,
            webpage_url=webpage_url,
            keyword=keyword,
        )
        if not job_ad_text:
            row.status = JobAIResponse.JobStatus.FAILED
            row.error_message = "Missing job text for AI analysis."
            row.save(update_fields=["status", "error_message", "updated_at"])
            return "failed"

        result = self._call_ai_with_retry(job_ad_text)
        if result:
            row.response_text = result
            row.status = JobAIResponse.JobStatus.COMPLETED
            row.error_message = None
            row.save(update_fields=["response_text", "status", "error_message", "updated_at"])
            return "processed"

        row.status = JobAIResponse.JobStatus.FAILED
        row.error_message = "AI request failed after retries. Check application logs for details."
        row.save(update_fields=["status", "error_message", "updated_at"])
        return "failed"

    def process_all_jobs_with_ai(
        self,
        batch_size: int = 10,
        delay_between_batches_sec: int = 3,
        keyword: Optional[str] = None,
        max_jobs: Optional[int] = None,
        only_failed: bool = False,
        target_job_id: Optional[int] = None,
        missing_only: bool = True,
    ):
        # Build processing candidate list according to command filters.
        jobs = self.get_all_job_urls_from_database(
            keyword=keyword,
            max_jobs=max_jobs,
            only_failed=only_failed,
            target_job_id=target_job_id,
            missing_only=missing_only,
        )
        total = len(jobs)
        logger.info(
            "JobAI start: %s jobs (keyword=%s, max_jobs=%s, only_failed=%s, target_job_id=%s, missing_only=%s)",
            total,
            keyword,
            max_jobs,
            only_failed,
            target_job_id,
            missing_only,
        )
        summary = {"processed": 0, "skipped": 0, "failed": 0}

        # Process in batches to keep run behavior predictable.
        for i in range(0, total, batch_size):
            batch = jobs[i:i + batch_size]
            logger.info("Batch %s-%s / %s", i + 1, min(i + batch_size, total), total)

            for job_id, url, title, description in batch:
                # Per-job execution and status accounting.
                status = self.process_single_job(
                    job_id=job_id,
                    webpage_url=url,
                    title=title,
                    description=description,
                    keyword=keyword,
                )
                if status in summary:
                    summary[status] += 1
                logger.info("Job %s => %s", job_id, status)
                time.sleep(0.5)

            if i + batch_size < total:
                time.sleep(delay_between_batches_sec)

        logger.info(
            "JobAI done. processed=%s skipped=%s failed=%s total=%s",
            summary["processed"],
            summary["skipped"],
            summary["failed"],
            total,
        )