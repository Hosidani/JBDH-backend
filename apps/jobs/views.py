from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework import status
from rest_framework.decorators import api_view, permission_classes
from django.db.models import Q
from django.http import JsonResponse
from django.views.decorators.http import require_GET
from apps.jobs.models import Job, JobApplication, NotInterestedJob, SavedJob
from apps.jobs.dto_mapper import build_job_details_dto, build_saved_job_dto
from apps.jobs.services.job_interaction_service import (
    InvalidInputError,
    JobInteractionService,
    NotInterestedAlreadyExistsError,
    ResourceNotFoundError,
    SavedJobAlreadyExistsError,
)

class JobDetails(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, id):
        user = request.user
        try:
            context = JobInteractionService.get_job_details_context(job_id=id, user_id=user.id)
        except ResourceNotFoundError as error:
            return Response({"detail": str(error)}, status=status.HTTP_404_NOT_FOUND)

        job = context["job"]
        saved_job = context["saved_job"]

        dto = build_job_details_dto(
            job=job,
            has_applied=context["has_applied"],
            is_saved=context["is_saved"],
            is_not_interested=context["is_not_interested"],
            notes=saved_job.notes if saved_job else None,
        )
        return Response(dto)

class SaveJob(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, job_id):
        user = request.user
        try:
            saved_job = JobInteractionService.save_job(user_id=user.id, job_id=job_id)
        except ResourceNotFoundError as error:
            return Response({"detail": str(error)}, status=status.HTTP_404_NOT_FOUND)
        except SavedJobAlreadyExistsError:
            return Response({"detail": "Job is already saved."}, status=status.HTTP_409_CONFLICT)

        return Response({"id": saved_job.id, "jobId": saved_job.job_id, "savedAt": saved_job.saved_at}, status=status.HTTP_201_CREATED)

    def put(self, request, job_id):
        user = request.user
        notes = request.data.get("notes")
        try:
            saved_job = JobInteractionService.update_saved_job_notes(user_id=user.id, job_id=job_id, notes=notes)
        except ResourceNotFoundError as error:
            return Response({"detail": str(error)}, status=status.HTTP_404_NOT_FOUND)
        except InvalidInputError as error:
            return Response({"detail": str(error)}, status=status.HTTP_400_BAD_REQUEST)

        return Response({
            "id": saved_job.id,
            "jobId": saved_job.job_id,
            "savedAt": saved_job.saved_at,
            "notes": saved_job.notes
        })

    def delete(self, request, job_id):
        user = request.user
        try:
            JobInteractionService.unsave_job(user_id=user.id, job_id=job_id)
        except ResourceNotFoundError as error:
            return Response({"detail": str(error)}, status=status.HTTP_404_NOT_FOUND)

        return Response(status=status.HTTP_204_NO_CONTENT)

class IsSaved(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, job_id):
        user = request.user
        is_saved = JobInteractionService.is_job_saved(user_id=user.id, job_id=job_id)
        return Response({"jobId": job_id, "isSaved": is_saved})

class GetSavedJobs(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        user = request.user
        saved_jobs = JobInteractionService.get_saved_jobs(user_id=user.id)
        result = []
        for saved_job in saved_jobs:
            job = saved_job.job
            has_applied = JobApplication.objects.filter(job_id=job.id, user_id=user.id).exists()
            is_not_interested = JobInteractionService.is_not_interested(user_id=user.id, job_id=job.id)
            result.append(
                build_saved_job_dto(
                    saved_job=saved_job,
                    has_applied=has_applied,
                    is_not_interested=is_not_interested,
                )
            )
        return Response(result)

class MarkNotInterested(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, job_id):
        user = request.user
        try:
            JobInteractionService.mark_not_interested(user_id=user.id, job_id=job_id)
        except ResourceNotFoundError as error:
            return Response({"detail": str(error)}, status=status.HTTP_404_NOT_FOUND)
        except NotInterestedAlreadyExistsError:
            return Response({"detail": "Job already marked as not interested."}, status=status.HTTP_409_CONFLICT)

        return Response({"success": True, "message": "Job marked as not interested"})

    def delete(self, request, job_id):
        user = request.user
        try:
            JobInteractionService.unmark_not_interested(user_id=user.id, job_id=job_id)
        except ResourceNotFoundError as error:
            return Response({"detail": str(error)}, status=status.HTTP_404_NOT_FOUND)

        return Response(status=status.HTTP_204_NO_CONTENT)


@api_view(["GET"])
@permission_classes([AllowAny])
def search_for_jobs(request):
    description = (request.query_params.get("description") or "").strip()
    location = (request.query_params.get("location") or "").strip()

    jobs = Job.objects.all()

    if description:
        jobs = jobs.filter(
            Q(title__icontains=description) |
            Q(description__icontains=description)
        )

    if location:
        jobs = jobs.filter(location__icontains=location)

    user = request.user
    user_id = user.id if getattr(user, "is_authenticated", False) else None

    if user_id is not None:
        not_interested_ids = set(
            NotInterestedJob.objects.filter(user_id=user_id).values_list("job_id", flat=True)
        )
        jobs = jobs.exclude(id__in=not_interested_ids)
        saved_ids = set(
            SavedJob.objects.filter(user_id=user_id, job_id__in=jobs.values_list("id", flat=True))
            .values_list("job_id", flat=True)
        )
    else:
        saved_ids = set()

    result = []
    for job in jobs:
        dto = build_job_details_dto(
            job=job,
            has_applied=False,
            is_saved=job.id in saved_ids,
            is_not_interested=False,
            notes=None,
        )
        result.append(dto)

    return Response(result)


@require_GET
def search_v2(request):
    description = request.GET.get('description', '')
    location = request.GET.get('location', '')
    work_area = request.GET.get('workArea', '')
    roles = request.GET.get('roles', '')
    competence = request.GET.get('competence', '')

    jobs = Job.objects.all()
    if description:
        jobs = jobs.filter(description__icontains=description)
    if location:
        jobs = jobs.filter(location__icontains=location)
    if work_area:
        jobs = jobs.filter(work_type__icontains=work_area)  # Anpassa om du har annat fält
    if roles:
        jobs = jobs.filter(employment_type__icontains=roles)  # Anpassa om du har annat fält
    if competence:
        jobs = jobs.filter(required_skills__icontains=competence)  # <-- ändrat här!

    job_list = []
    for job in jobs:
        job_list.append({
            "externalJobId": job.job_id,  # <-- rätt fält
            "jobId": job.id,
            "title": job.title,
            "description": job.description,
            "company": job.company,
            "location": job.location,
            "postedDate": job.posted_date.isoformat() if job.posted_date else None,
            "webpageUrl": job.webpage_url,
            "workType": job.work_type,
            "employmentType": job.employment_type,
            "applicationDeadline": job.application_deadline.isoformat() if job.application_deadline else None,
            "requiredSkills": build_job_details_dto(
                job=job,
                has_applied=False,
                is_saved=False,
                is_not_interested=False,
                notes=None,
            )["requiredSkills"],
            "requiredSoftSkills": [],
            "isSaved": False,
            "hasApplied": False,
            "isNotInterested": False,
            # Lägg till fler fält om frontend behöver
        })

    return JsonResponse(job_list, safe=False)
