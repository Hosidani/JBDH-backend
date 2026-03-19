# apps/ai_services/views.py
"""
Compare CV to Job Ad V2: extract CV/job requirements + profession, compare with embeddings, return scores.
Mirrors .NET HomeController.CompareCvToJobAdV2.
"""
import logging
import os
from django.conf import settings
from rest_framework import status
from rest_framework.decorators import api_view, permission_classes, parser_classes
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from django.views.decorators.csrf import csrf_exempt
from django.http import StreamingHttpResponse
from apps.ai_services.services.cv_job_analysis_service import extract_cv_job_analysis
import json
from apps.ai_services.file_text_extraction_service import extract_text_from_file
from core.utils import vectorize_text
from apps.ai_services.minilm_embedding_service import cosine_similarity, compare_skills_with_embeddings_minilm, calculate_profession_score_with_embeddings_minilm
from apps.ai_services.cv_job_vector_service import CVJobVectorService
from apps.ai_services.cv_analysis_service import CVAnalysisService
from apps.ai_services.job_analysis_service import extract_job_requirements_and_profession

from rest_framework.parsers import MultiPartParser, FormParser
from rest_framework.decorators import renderer_classes
from rest_framework.renderers import BaseRenderer
from core.utils import vectorize_text
from apps.ai_services.minilm_embedding_service import cosine_similarity

class EventStreamRenderer(BaseRenderer):
    media_type = 'text/event-stream'
    format = 'event-stream'
    def render(self, data, accepted_media_type=None, renderer_context=None):
        return data

logger = logging.getLogger(__name__)



from django.http import HttpResponseNotAllowed

@csrf_exempt
def compare_cv_job_sse(request):
    if request.method != 'POST':
        return HttpResponseNotAllowed(['POST'])
    def event_stream():
        # 1. Progress 10%: Laddar CV...
        event1 = f"data: {json.dumps({'progress': 10, 'message': '🔄 Laddar CV...'})}\n\n"
        print("SSE event to FE:", event1)
        yield event1
        cv_file = request.FILES.get("CvPath")
        job_ad_text = (
            getattr(request, "data", {}).get("JobAdText")
            or request.POST.get("JobAdText")
            or ""
        ).strip()
        if not cv_file or not job_ad_text:
            error_event = f"data: {json.dumps({'progress': 100, 'error': 'CV och Jobbtext krävs.'})}\n\n"
            print("SSE event to FE:", error_event)
            yield error_event
            return

        # Spara och extrahera CV-text
        upload_dir = os.path.join(settings.MEDIA_ROOT, "uploads")
        os.makedirs(upload_dir, exist_ok=True)
        saved_name = cv_file.name
        saved_path = os.path.join(upload_dir, saved_name)
        with open(saved_path, "wb+") as dest:
            for chunk in cv_file.chunks():
                dest.write(chunk)
        cv_text = extract_text_from_file(saved_path)
        if not cv_text or not cv_text.strip():
            error_event = f"data: {json.dumps({'progress': 100, 'error': 'Kunde inte extrahera CV-text.'})}\n\n"
            print("SSE event to FE:", error_event)
            yield error_event
            return

        # 2. Progress 30%: Vektoranalys
        try:
            cv_vector = vectorize_text(cv_text)
            job_ad_vector = vectorize_text(job_ad_text)
            vector_similarity = round(cosine_similarity(cv_vector, job_ad_vector) * 100, 2)
            vector_result = {
                "similarityScore": vector_similarity,
                "cvVectorSize": len(cv_vector),
                "jobAdVectorSize": len(job_ad_vector),
            }
            vector_event = f"data: {json.dumps({'progress': 30, 'message': f'⚡ Vektoranalys klar: {vector_similarity}%', 'vectorResult': vector_result})}\n\n"
            print("SSE event to FE:", vector_event)
            yield vector_event
        except Exception as vector_compare_ex:
            logger.exception("On-the-fly CV vs JobAd vector compare failed: %s", vector_compare_ex)
            error_event = f"data: {json.dumps({'progress': 100, 'error': 'Vektoranalys misslyckades.'})}\n\n"
            print("SSE event to FE:", error_event)
            yield error_event
            return

        # 3. Progress 50%: DeepSeek analyserar...
        progress_event = f"data: {json.dumps({'progress': 50, 'message': '🧠 DeepSeek analyserar...'})}\n\n"
        print("SSE event to FE:", progress_event)
        yield progress_event

        # 4. Komplett analys (kommenterad för ny AI-template)
        # try:
        #     cv_service = CVAnalysisService()
        #     cv_extraction = cv_service.extract_cv_profession_and_requirements(cv_text)
        #     if not cv_extraction:
        #         logger.warning("CV extraction failed or returned null")
        #         error_event = f"data: {json.dumps({'progress': 100, 'error': 'CV extraction failed (AI unavailable eller invalid API key).'})}\n\n"
        #         print("SSE event to FE:", error_event)
        #         yield error_event
        #         return
        #     cv_profession = cv_extraction.profession
        #     cv_requirements = cv_extraction.requirements or []
        #     job_requirements, job_profession = extract_job_requirements_and_profession(job_ad_text)
        #     if len(job_requirements) > 100:
        #         logger.warning("Requirements count %s exceeds 100, using first 100", len(job_requirements))
        #         job_requirements = job_requirements[:100]
        #     if not job_requirements:
        #         error_event = f"data: {json.dumps({'progress': 100, 'error': 'Job requirements extraction failed or returned empty result.'})}\n\n"
        #         print("SSE event to FE:", error_event)
        #         yield error_event
        #         return
        #     match_percentage, results = compare_skills_with_embeddings_minilm(cv_requirements, job_requirements)
        #     profession_score = calculate_profession_score_with_embeddings_minilm(cv_profession, job_profession)
        #     profession_percentage = round(profession_score * 100, 2)
        #     total_match_score = (match_percentage / 100.0 * 0.3) + (profession_score * 0.7)
        #     total_match_percentage = round(total_match_score * 100, 2)
        #     def profession_to_dict(p):
        #         if not p:
        #             return None
        #         return {"main": p.main, "sub": p.sub, "specialty": p.specialty}
        #     response_payload = {
        #         "totalScore": total_match_percentage,
        #         "skillsScore": match_percentage,
        #         "professionScore": profession_percentage,
        #         "cvProfession": profession_to_dict(cv_profession),
        #         "jobProfession": profession_to_dict(job_profession),
        #         "matches": results,
        #         "totalRequirements": len(job_requirements),
        #         "vectorResult": vector_result,
        #     }
        #     final_event = f"data: {json.dumps({'progress': 100, 'message': '✅ Komplett analys klar!', 'result': response_payload})}\n\n"
        #     print("SSE event to FE:", final_event)
        #     yield final_event
        # except Exception as ex:
        #     logger.exception("SSE-analys misslyckades: %s", ex)
        #     error_event = f"data: {json.dumps({'progress': 100, 'error': 'Analys misslyckades.'})}\n\n"
        #     print("SSE event to FE:", error_event)
        #     yield error_event
        # Nytt steg 4: Kombinerad AI-analys med template 5

        result_json = extract_cv_job_analysis(cv_text, job_ad_text)
        if result_json:
            def join_profession_fields(prof):
                if not isinstance(prof, dict):
                    return ""
                return " ".join([prof.get(k, "") for k in ["main", "sub", "specialty"] if prof.get(k)])

            def join_skill_names(skills):
                if not isinstance(skills, list):
                    return ""
                return " ".join([s.get("name", "") for s in skills if isinstance(s, dict) and s.get("name")])



            # Profession vector similarity
            cv_profession_str = join_profession_fields(result_json.get("cv_profession", {}))
            job_profession_str = join_profession_fields(result_json.get("job_profession", {}))
            profession_vector_similarity = 0.0
            if cv_profession_str.strip() and job_profession_str.strip():
                profession_vector_similarity = round(
                    cosine_similarity(
                        vectorize_text(cv_profession_str),
                        vectorize_text(job_profession_str)
                    ) * 100, 2)

            # Skills/requirements vector similarity
            cv_skills_str = join_skill_names(result_json.get("cv_skills", []))
            job_requirements_str = join_skill_names(result_json.get("job_requirements", []))
            skills_vector_similarity = 0.0
            if cv_skills_str.strip() and job_requirements_str.strip():
                skills_vector_similarity = round(
                    cosine_similarity(
                        vectorize_text(cv_skills_str),
                        vectorize_text(job_requirements_str)
                    ) * 100, 2)

            frontend_payload = {
                "summary": result_json.get("summary", ""),
                "matching_skills": result_json.get("cv_skills", []),
                "missing_requirements": result_json.get("gaps", []),
                "relevant_experiences": result_json.get("relevant_experiences", []),
                "education_and_certifications": result_json.get("education_and_certifications", []),
                "strengths": result_json.get("strengths", []),
                "gaps": result_json.get("gaps", []),
                "profession_vector_similarity": profession_vector_similarity,
                "skills_vector_similarity": skills_vector_similarity,
            }
            final_event = f"data: {json.dumps({'progress': 100, 'message': '✅ AI-analys klar!', 'result': frontend_payload})}\n\n"
            print("SSE event to FE:", final_event)
            yield final_event
        else:
            fail_event = f"data: {json.dumps({'progress': 100, 'error': 'AI-analys misslyckades.'})}\n\n"
            print("SSE event to FE:", fail_event)
            yield fail_event

    print("Returning StreamingHttpResponse for SSE")
    response = StreamingHttpResponse(event_stream(), content_type='text/event-stream')
    response['Cache-Control'] = 'no-cache'
    response['Access-Control-Allow-Origin'] = '*'
    return response



@api_view(["POST"])
@permission_classes([IsAuthenticated])
@parser_classes([MultiPartParser, FormParser])
def compare_cv_to_job_ad_v2(request):
    """
    POST /Home/CompareCvToJobAdV2
    Body: multipart/form-data with CvPath (file), JobAdText (string). Optional: language (default English).
    Returns: totalScore, skillsScore, professionScore, cvProfession, jobProfession, matches, totalRequirements.
    """
    cv_file = request.FILES.get("CvPath")
    job_ad_text = (request.data.get("JobAdText") or request.POST.get("JobAdText") or "").strip()
    language = request.data.get("language") or request.POST.get("language") or "English"
    include_job_matches_raw = request.data.get("includeJobMatches") or request.POST.get("includeJobMatches") or "false"
    top_k_raw = request.data.get("topK") or request.POST.get("topK") or 5
    min_score_raw = request.data.get("minScore") or request.POST.get("minScore") or 0.2

    if not cv_file:
        return Response({"error": "CV file is required"}, status=status.HTTP_400_BAD_REQUEST)
    if not job_ad_text:
        return Response({"error": "Job ad text is required"}, status=status.HTTP_400_BAD_REQUEST)

    vector_result = {
        "similarityScore": 0.0,
        "cvVectorSize": 0,
        "jobAdVectorSize": 0,
    }

    try:
        include_job_matches = str(include_job_matches_raw).strip().lower() in {"1", "true", "yes", "y", "on"}
        try:
            top_k = int(top_k_raw)
        except (TypeError, ValueError):
            top_k = 5
        try:
            min_score = float(min_score_raw)
        except (TypeError, ValueError):
            min_score = 0.2

        top_k = max(1, min(top_k, 50))
        min_score = max(-1.0, min(min_score, 1.0))

        # 1. Save uploaded file and extract text
        upload_dir = os.path.join(settings.MEDIA_ROOT, "uploads")
        os.makedirs(upload_dir, exist_ok=True)
        saved_name = cv_file.name
        saved_path = os.path.join(upload_dir, saved_name)
        with open(saved_path, "wb+") as dest:
            for chunk in cv_file.chunks():
                dest.write(chunk)

        cv_text = extract_text_from_file(saved_path)
        if not cv_text or not cv_text.strip():
            return Response({"error": "Could not extract CV text"}, status=status.HTTP_400_BAD_REQUEST)

        # On-the-fly vector comparison (no DB persistence required)
        try:
            cv_vector = vectorize_text(cv_text)
            job_ad_vector = vectorize_text(job_ad_text)
            vector_similarity = round(cosine_similarity(cv_vector, job_ad_vector) * 100, 2)
            vector_result = {
                "similarityScore": vector_similarity,
                "cvVectorSize": len(cv_vector),
                "jobAdVectorSize": len(job_ad_vector),
            }
            # Skicka direkt till frontend
            return Response({
                "vectorResult": vector_result,
                "vectorOnly": True,
                "step": 1,
                "message": "Vektorjämförelse klar. Nästa steg: krav/profession."
            }, status=status.HTTP_200_OK)
        except Exception as vector_compare_ex:
            logger.exception("On-the-fly CV vs JobAd vector compare failed: %s", vector_compare_ex)

        # 2. Extract CV profession and requirements
        cv_service = CVAnalysisService()
        cv_extraction = cv_service.extract_cv_profession_and_requirements(cv_text)
        if not cv_extraction:
            logger.warning("CV extraction failed or returned null")
            return Response(
                {
                    "totalScore": 0.0,
                    "skillsScore": 0.0,
                    "professionScore": 0.0,
                    "cvProfession": None,
                    "jobProfession": None,
                    "matches": [],
                    "totalRequirements": 0,
                    "vectorResult": vector_result,
                    "vectorOnly": True,
                    "warning": "CV extraction failed (AI unavailable or invalid API key).",
                },
                status=status.HTTP_200_OK,
            )

        cv_profession = cv_extraction.profession
        cv_requirements = cv_extraction.requirements or []

        # 3. Extract job requirements and profession
        job_requirements, job_profession = extract_job_requirements_and_profession(job_ad_text)
        if len(job_requirements) > 100:
            logger.warning("Requirements count %s exceeds 100, using first 100", len(job_requirements))
            job_requirements = job_requirements[:100]
        if not job_requirements:
            return Response(
                {
                    "totalScore": 0.0,
                    "skillsScore": 0.0,
                    "professionScore": 0.0,
                    "cvProfession": None,
                    "jobProfession": None,
                    "matches": [],
                    "totalRequirements": 0,
                    "vectorResult": vector_result,
                    "vectorOnly": True,
                    "warning": "Job requirements extraction failed or returned empty result.",
                },
                status=status.HTTP_200_OK,
            )

        # 4. Compare skills with embeddings
        match_percentage, results = compare_skills_with_embeddings_minilm(cv_requirements, job_requirements)

        # 5. Profession score
        profession_score = calculate_profession_score_with_embeddings_minilm(cv_profession, job_profession)
        profession_percentage = round(profession_score * 100, 2)

        # 6. Total score: 30% skills + 70% profession
        total_match_score = (match_percentage / 100.0 * 0.3) + (profession_score * 0.7)
        total_match_percentage = round(total_match_score * 100, 2)

        # Serialize profession for JSON (camelCase for frontend)
        def profession_to_dict(p):
            if not p:
                return None
            return {"main": p.main, "sub": p.sub, "specialty": p.specialty}

        response_payload = {
            "totalScore": total_match_percentage,
            "skillsScore": match_percentage,
            "professionScore": profession_percentage,
            "cvProfession": profession_to_dict(cv_profession),
            "jobProfession": profession_to_dict(job_profession),
            "matches": results,
            "totalRequirements": len(job_requirements),
            "vectorResult": vector_result,
        }

        if include_job_matches:
            try:
                response_payload["jobVectorMatches"] = CVJobVectorService.find_best_matching_jobs(
                    cv_extraction=cv_extraction,
                    top_k=top_k,
                    min_score=min_score,
                )
            except Exception as vector_ex:
                logger.exception("CV→Job vector matching failed: %s", vector_ex)
                response_payload["jobVectorMatches"] = []

        return Response(response_payload, status=status.HTTP_200_OK)
    except Exception as ex:
        logger.exception("CompareCvToJobAdV2 failed: %s", ex)
        return Response(
            {
                "error": "Comparison failed",
                "detail": str(ex),
                "vectorResult": vector_result,
                "vectorOnly": True,
            },
            status=status.HTTP_400_BAD_REQUEST,
        )
