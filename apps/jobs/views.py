from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework import status
from rest_framework.decorators import api_view, permission_classes
from django.http import JsonResponse
from django.views.decorators.http import require_GET
from apps.jobs.services.search_params import JobSearchParams
from apps.jobs.services.semantic_job_service import SemanticJobService
from apps.jobs.services.job_interaction_service import (JobInteractionService)

class JobDetails(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, id):
        dto = JobInteractionService.get_job_details_dto(job_id=id, user_id=request.user.id)
        return Response(dto)

class SaveJob(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, job_id):
        user = request.user
        saved_job = JobInteractionService.save_job(user_id=user.id, job_id=job_id)
        return Response({"id": saved_job.id, "jobId": saved_job.job_id, "savedAt": saved_job.saved_at}, status=status.HTTP_201_CREATED)

    def put(self, request, job_id):
        user = request.user
        notes = request.data.get("notes")
        saved_job = JobInteractionService.update_saved_job_notes(user_id=user.id, job_id=job_id, notes=notes)
        return Response({
            "id": saved_job.id,
            "jobId": saved_job.job_id,
            "savedAt": saved_job.saved_at,
            "notes": saved_job.notes
        })

    def delete(self, request, job_id):
        user = request.user
        JobInteractionService.unsave_job(user_id=user.id, job_id=job_id)
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
        result = JobInteractionService.get_saved_jobs_with_context(user.id)
        return Response(result)

class MarkNotInterested(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, job_id):
        user = request.user
        JobInteractionService.mark_not_interested(user_id=user.id, job_id=job_id)
        return Response({"success": True, "message": "Job marked as not interested"})

    def delete(self, request, job_id):
        user = request.user
        JobInteractionService.unmark_not_interested(user_id=user.id, job_id=job_id)
        return Response(status=status.HTTP_204_NO_CONTENT)


@api_view(["GET"])
@permission_classes([AllowAny])
def search_for_jobs(request):
    description = (request.query_params.get("description") or "").strip()
    location = (request.query_params.get("location") or "").strip()
    results = SemanticJobService.basic_search(
        description=description,
        location=location,
        user=request.user
    )
    return Response(results)


@require_GET
def search_v2(request):
    params = JobSearchParams.from_request(request)
    jobs, total_matches = SemanticJobService.multi_field_search(params.dict())
    results = SemanticJobService.serialize_jobs(jobs)
    return JsonResponse({
        "results": results,           # 20 jobb
        "total": total_matches,       # 85360!
        "offset": params.offset,
        "limit": params.limit,
        "has_more": params.offset + params.limit < total_matches
    }, safe=False)
