from rest_framework.views import APIView
import json
from functools import lru_cache, reduce
from rest_framework.response import Response
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework import status
from rest_framework.decorators import api_view, permission_classes
from django.db.models import Q, F, Value, FloatField, ExpressionWrapper
from django.db.models.functions import Coalesce
from django.http import JsonResponse
from django.views.decorators.http import require_GET
from pgvector.django import CosineDistance
from core.utils import vectorize_text
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


@lru_cache(maxsize=512)
def _cached_query_embedding(text: str) -> tuple[float, ...]:
    vector = vectorize_text(text, context="job") or []
    return tuple(vector)


@require_GET
def search_v2(request):
    def get_param(name):
        return request.GET.get(name, '').strip()

    params = {
        'description': get_param('description'),
        'location': get_param('location'),
        'workArea': get_param('workArea'),
        'roles': get_param('roles'),
        'competence': get_param('competence'),
    }

    jobs = Job.objects.all()

    field_mapping = {
        'description': 'description_vector',
        'location': 'location_vector',
        'workArea': 'line_of_business_vector',
        'roles': 'title_vector',
        'competence': 'competence_vector',
    }
    text_field_mapping = {
        'description': ['description', 'title'],
        'location': ['location'],
        'workArea': ['line_of_business', 'title', 'description'],
        'roles': ['title', 'description'],
        'competence': ['required_skills', 'description'],
    }

    similarity_aliases = []
    similarity_threshold = 0.3
    query_vectors = {
        'description': [],
        'location': [],
        'workArea': [],
        'roles': [],
        'competence': [],
    }

    for param_key, db_vector_field in field_mapping.items():
        query_text = params.get(param_key, '')
        if not query_text:
            continue

        db_text_fields = text_field_mapping[param_key]
        text_query = Q()
        for field_name in db_text_fields:
            text_query |= Q(**{f"{field_name}__icontains": query_text})

        query_vector = list(_cached_query_embedding(query_text))
        if not query_vector:
            jobs = jobs.filter(text_query)
            continue

        query_vectors[param_key] = query_vector

        distance_alias = f"{param_key}_distance"
        similarity_alias = f"{param_key}_similarity"

        jobs = jobs.annotate(**{distance_alias: CosineDistance(db_vector_field, query_vector)})
        jobs = jobs.annotate(
            **{
                similarity_alias: ExpressionWrapper(
                    Value(1.0) - F(distance_alias),
                    output_field=FloatField(),
                )
            }
        )
        jobs = jobs.filter(
            Q(**{f"{similarity_alias}__gt": similarity_threshold})
            | text_query
        )
        similarity_aliases.append(similarity_alias)

    if similarity_aliases:
        similarity_values = [Coalesce(F(alias), Value(0.0)) for alias in similarity_aliases]
        total_similarity_expr = reduce(
            lambda left, right: left + right,
            similarity_values[1:],
            similarity_values[0],
        )
        jobs = jobs.annotate(
            overall_similarity=ExpressionWrapper(
                total_similarity_expr / Value(float(len(similarity_aliases))),
                output_field=FloatField(),
            )
        ).order_by('-overall_similarity')
    else:
        jobs = jobs.order_by('-posted_date')

    job_list = [
        build_job_details_dto(
            job=job,
            has_applied=False,
            is_saved=False,
            is_not_interested=False,
            notes=None,
        )
        for job in jobs[:20]
    ]

    all_vectors = [v for v in query_vectors.values() if v]
    if all_vectors:
        search_vector = [sum(x) / len(all_vectors) for x in zip(*all_vectors)]
    else:
        search_vector = []

    print("Jobbresultat:", json.dumps(job_list, ensure_ascii=False, indent=2, default=str))
    return JsonResponse({
        "results": job_list,
        "descriptionVector": query_vectors['description'],
        "locationVector": query_vectors['location'],
        "workAreaVector": query_vectors['workArea'],
        "rolesVector": query_vectors['roles'],
        "competenceVector": query_vectors['competence'],
        "searchVector": search_vector,
    }, safe=False)
