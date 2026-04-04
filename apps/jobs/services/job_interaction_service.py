from typing import Iterable, Optional
from typing import List, Dict
from apps.users.models import User
from apps.jobs.dto_mapper import build_saved_job_dto, build_job_details_dto
from apps.jobs.models import JobApplication
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
    def get_job_details_dto(job_id: int, user_id: int) -> dict:
        # Återanvänd din befintliga context-metod
        context = JobInteractionService.get_job_details_context(job_id, user_id)
        
        # Bygg DTO:n här inne istället för i Viewn
        return build_job_details_dto(
            job=context["job"],
            has_applied=context["has_applied"],
            is_saved=context["is_saved"],
            is_not_interested=context["is_not_interested"],
            notes=context["saved_job"].notes if context["saved_job"] else None,
        )

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

    @staticmethod
    def get_saved_jobs_with_context(user_id: int) -> List[Dict]:
        """Hämtar sparade jobb MED applied/not_interested context"""
        saved_jobs = JobInteractionService.get_saved_jobs(user_id=user_id)
        result = []
        user = User.objects.get(id=user_id)
        
        for saved_job in saved_jobs:
            job = saved_job.job
            has_applied = JobApplication.objects.filter(job=job, user=user).exists()
            is_not_interested = JobInteractionService.is_not_interested(user_id, job.id)
            result.append(
                build_saved_job_dto(
                    saved_job=saved_job,
                    has_applied=has_applied,
                    is_not_interested=is_not_interested,
                )
            )
        return result
    
    @staticmethod
    def get_user_interaction_context(user_id: Optional[int], job_ids: List[int]) -> dict:
        """Hämtar bulk-data om sparade och dolda jobb för en lista med ID:n"""
        if not user_id:
            return {"saved_ids": set(), "not_interested_ids": set()}

        saved_ids = set(
            SavedJob.objects.filter(user_id=user_id, job_id__in=job_ids)
            .values_list("job_id", flat=True)
        )
        not_interested_ids = set(
            NotInterestedJob.objects.filter(user_id=user_id)
            .values_list("job_id", flat=True)
        )
        return {
            "saved_ids": saved_ids,
            "not_interested_ids": not_interested_ids
        }

