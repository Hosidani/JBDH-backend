from typing import Iterable, Optional

from apps.jobs.models import Job, JobApplication, NotInterestedJob, SavedJob


class ResourceNotFoundError(Exception):
    pass


class InvalidInputError(Exception):
    pass


class SavedJobAlreadyExistsError(Exception):
    pass


class NotInterestedAlreadyExistsError(Exception):
    pass


class JobInteractionService:
    @staticmethod
    def get_job(job_id: int) -> Job:
        job = Job.objects.filter(pk=job_id).first()
        if job is None:
            raise ResourceNotFoundError("Job not found.")
        return job

    @staticmethod
    def get_job_details_context(job_id: int, user_id: int) -> dict:
        job = JobInteractionService.get_job(job_id)
        has_applied = JobApplication.objects.filter(job_id=job_id, user_id=user_id).exists()
        saved_job = SavedJob.objects.filter(user_id=user_id, job_id=job_id).first()
        is_saved = saved_job is not None
        is_not_interested = NotInterestedJob.objects.filter(user_id=user_id, job_id=job_id).exists()

        return {
            "job": job,
            "has_applied": has_applied,
            "saved_job": saved_job,
            "is_saved": is_saved,
            "is_not_interested": is_not_interested,
        }

    @staticmethod
    def save_job(user_id: int, job_id: int) -> SavedJob:
        JobInteractionService.get_job(job_id)

        if SavedJob.objects.filter(user_id=user_id, job_id=job_id).exists():
            raise SavedJobAlreadyExistsError("Job is already saved.")

        return SavedJob.objects.create(user_id=user_id, job_id=job_id)

    @staticmethod
    def update_saved_job_notes(user_id: int, job_id: int, notes: Optional[str]) -> SavedJob:
        saved_job = SavedJob.objects.filter(user_id=user_id, job_id=job_id).first()
        if saved_job is None:
            raise ResourceNotFoundError("Saved job not found.")

        if notes is not None and not isinstance(notes, str):
            raise InvalidInputError("Notes must be a string.")

        if isinstance(notes, str) and len(notes) > 1000:
            raise InvalidInputError("Notes cannot exceed 1000 characters.")

        saved_job.notes = notes
        saved_job.save(update_fields=["notes"])
        return saved_job

    @staticmethod
    def unsave_job(user_id: int, job_id: int) -> None:
        saved_job = SavedJob.objects.filter(user_id=user_id, job_id=job_id).first()
        if saved_job is None:
            raise ResourceNotFoundError("Saved job not found.")

        saved_job.delete()

    @staticmethod
    def is_job_saved(user_id: int, job_id: int) -> bool:
        return SavedJob.objects.filter(user_id=user_id, job_id=job_id).exists()

    @staticmethod
    def is_not_interested(user_id: int, job_id: int) -> bool:
        return NotInterestedJob.objects.filter(user_id=user_id, job_id=job_id).exists()

    @staticmethod
    def get_saved_jobs(user_id: int) -> Iterable[SavedJob]:
        return (
            SavedJob.objects.filter(user_id=user_id)
            .select_related("job")
            .order_by("-saved_at")
        )

    @staticmethod
    def mark_not_interested(user_id: int, job_id: int) -> NotInterestedJob:
        JobInteractionService.get_job(job_id)

        if NotInterestedJob.objects.filter(user_id=user_id, job_id=job_id).exists():
            raise NotInterestedAlreadyExistsError("Job already marked as not interested.")

        return NotInterestedJob.objects.create(user_id=user_id, job_id=job_id)

    @staticmethod
    def unmark_not_interested(user_id: int, job_id: int) -> None:
        not_interested = NotInterestedJob.objects.filter(user_id=user_id, job_id=job_id).first()
        if not_interested is None:
            raise ResourceNotFoundError("Job not marked as not interested.")

        not_interested.delete()
