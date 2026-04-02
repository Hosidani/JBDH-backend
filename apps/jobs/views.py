from rest_framework.views import APIView
import json
from core.utils import cosine_similarity
from rest_framework.response import Response
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework import status
from rest_framework.decorators import api_view, permission_classes
from django.db.models import Q
from django.http import JsonResponse
from django.views.decorators.http import require_GET
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


@require_GET
def search_v2(request):
    def get_param(name):
        return request.GET.get(name, '').strip()

    # DRY: Hämta alla sökparametrar
    params = {
        'description': get_param('description'),
        'location': get_param('location'),
        'workArea': get_param('workArea'),
        'roles': get_param('roles'),
        'competence': get_param('competence'),
    }


    # Skapa exakt 5 vektorer, en för varje sökfält (även om fältet är tomt)
    description_vector = vectorize_text(params['description']) if params['description'] else []
    location_vector = vectorize_text(params['location']) if params['location'] else []
    workarea_vector = vectorize_text(params['workArea']) if params['workArea'] else []
    roles_vector = vectorize_text(params['roles']) if params['roles'] else []
    competence_vector = vectorize_text(params['competence']) if params['competence'] else []

    # här vill jag räkna medelvärde av alla 5 vektorer för att skapa en sammanslagen sökvektor, det går bra att vara tomma vektorer i listan, de ska bara ignoreras i medelvärdesberäkningen
    all_vectors = [v for v in [description_vector, location_vector, workarea_vector, roles_vector, competence_vector] if v]
    if all_vectors:
        search_vector = [sum(x) / len(all_vectors) for x in zip(*all_vectors)]
    else:
        search_vector = []

    # hämta de närmaste jobben baserat på cosine similarity mellan sökvektorn och jobben i databasen med hjälp av pgvector
    jobs = Job.objects.all()
    if params['description']:
        jobs = jobs.filter(description__icontains=params['description'])
    if params['location']:
        jobs = jobs.filter(location__icontains=params['location'])
    if params['workArea']:
        jobs = jobs.filter(line_of_business__icontains=params['workArea'])
    if params['roles']:
        jobs = jobs.filter(employment_type__icontains=params['roles'])
    if params['competence']:
        jobs = jobs.filter(required_skills__icontains=params['competence'])

    vector_pairs = [
        (description_vector, 'description_vector'),
        (location_vector, 'location_vector'),
        (workarea_vector, 'line_of_business_vector'),
        (roles_vector, 'employment_type_vector'),
        (competence_vector, 'competence_vector'),
    ]

    job_list = []
    for job in jobs:
        scores = []
        for query_vector, field_name in vector_pairs:
            if not query_vector:
                continue

            job_vector = getattr(job, field_name, None) or []
            if job_vector:
                scores.append(cosine_similarity(query_vector, job_vector))

        similarity = (sum(scores) / len(scores)) if scores else 0.0
        job_list.append((similarity, job))
    job_list.sort(key=lambda x: x[0], reverse=True)  # sortera efter likhet
    job_list = [build_job_details_dto(
        job=job,
        has_applied=False,
        is_saved=False,
        is_not_interested=False,
        notes=None,
    ) for _, job in job_list[:20]]  # returnera top 20 resultat
    job_list = job_list if isinstance(job_list, list) else []

    #
    print("Jobbresultat:", json.dumps(job_list, ensure_ascii=False, indent=2, default=str))
    return JsonResponse({
        "results": job_list,
        "descriptionVector": description_vector,
        "locationVector": location_vector,
        "workAreaVector": workarea_vector,
        "rolesVector": roles_vector,
        "competenceVector": competence_vector,
        "searchVector": search_vector,
    }, safe=False)
