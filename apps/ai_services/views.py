# apps/ai_services/views.py
import logging
from rest_framework import status
from rest_framework.decorators import api_view, permission_classes, parser_classes
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from django.http import StreamingHttpResponse, JsonResponse
from django.views.decorators.csrf import csrf_exempt
from core.middleware.jwt_required import jwt_required
from apps.ai_services.services.cv_job_sse_service import CVJobSSEService

from rest_framework.parsers import MultiPartParser, FormParser
from core.ats_client import score_cv_job_ats

# --- ATS Score Endpoint ---
from django.utils.encoding import smart_str
@api_view(['POST'])
@permission_classes([IsAuthenticated])
@parser_classes([MultiPartParser, FormParser])
def ats_score_view(request):
    """
    POST: Beräknar ATS-score mellan CV och jobbannons.
    Tar emot:
      - CvPath: fil (PDF, DOCX, TXT)
      - JobAdText: text
    Returnerar ATS-score och breakdown.
    """
    cv_file = request.FILES.get('CvPath')
    job_ad_text = request.data.get('JobAdText', '')
    if not cv_file or not job_ad_text.strip():
        return Response({'error': 'CV och jobbannons krävs.'}, status=400)

    # För framtida utökning: hantera olika filtyper
    try:
        cv_text = cv_file.read().decode('utf-8', errors='ignore')
    except Exception:
        return Response({'error': 'Kunde inte läsa CV-filen som text.'}, status=400)

    ats_result = score_cv_job_ats(cv_text, job_ad_text)
    return Response(ats_result)

logger = logging.getLogger(__name__)

@csrf_exempt
@jwt_required
def compare_cv_job_sse(request):
    if request.method != 'POST':
        return JsonResponse({'detail': 'Method not allowed'}, status=405)
    service = CVJobSSEService()
    response = StreamingHttpResponse(service.generate_events(request), content_type='text/event-stream')
    response['Cache-Control'] = 'no-cache'
    response['X-Accel-Buffering'] = 'no'
    return response


# ---------------------------------------------------------------------------
# External CV CRUD
# ---------------------------------------------------------------------------
from rest_framework.views import APIView
from .models import ExternalCV, ExternalJobAd
from .serializers import ExternalCVSerializer, ExternalJobAdSerializer
from django.shortcuts import get_object_or_404
from apps.ai_services.services.external_cv_job_ad_service import ExternalCVService, ExternalJobAdService


class ExternalCVListCreateView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        return Response(ExternalCVService.list(request.user))

    def post(self, request):
        data, errors = ExternalCVService.create(request.data, request.user)
        if errors:
            return Response(errors, status=status.HTTP_400_BAD_REQUEST)
        return Response(data, status=status.HTTP_201_CREATED)


class ExternalCVDetailView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, pk):
        return Response(ExternalCVService.get(pk, request.user))

    def put(self, request, pk):
        data, errors = ExternalCVService.update(pk, request.user, request.data)
        if errors:
            return Response(errors, status=status.HTTP_400_BAD_REQUEST)
        return Response(data)

    def patch(self, request, pk):
        data, errors = ExternalCVService.update(pk, request.user, request.data, partial=True)
        if errors:
            return Response(errors, status=status.HTTP_400_BAD_REQUEST)
        return Response(data)

    def delete(self, request, pk):
        ExternalCVService.delete(pk, request.user)
        return Response(status=status.HTTP_204_NO_CONTENT)


# ---------------------------------------------------------------------------
# External Job Ad CRUD
# ---------------------------------------------------------------------------

class ExternalJobAdListCreateView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        return Response(ExternalJobAdService.list(request.user))

    def post(self, request):
        data, errors = ExternalJobAdService.create(request.data, request.user)
        if errors:
            return Response(errors, status=status.HTTP_400_BAD_REQUEST)
        return Response(data, status=status.HTTP_201_CREATED)


class ExternalJobAdDetailView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, pk):
        return Response(ExternalJobAdService.get(pk, request.user))

    def put(self, request, pk):
        data, errors = ExternalJobAdService.update(pk, request.user, request.data)
        if errors:
            return Response(errors, status=status.HTTP_400_BAD_REQUEST)
        return Response(data)

    def patch(self, request, pk):
        data, errors = ExternalJobAdService.update(pk, request.user, request.data, partial=True)
        if errors:
            return Response(errors, status=status.HTTP_400_BAD_REQUEST)
        return Response(data)

    def delete(self, request, pk):
        ExternalJobAdService.delete(pk, request.user)
        return Response(status=status.HTTP_204_NO_CONTENT)


# ---------------------------------------------------------------------------
# Upload PDF → extract text → save as ExternalCV
# ---------------------------------------------------------------------------

class ExternalCVUploadPDFView(APIView):
    permission_classes = [IsAuthenticated]
    parser_classes = [MultiPartParser, FormParser]

    def post(self, request):
        uploaded = request.FILES.get('file')
        if not uploaded:
            return Response({'error': 'No file provided'}, status=status.HTTP_400_BAD_REQUEST)
        name = request.data.get('name', '')
        data, error = ExternalCVService.upload_pdf(uploaded, request.user, name=name)
        if error:
            return Response({'error': error}, status=status.HTTP_400_BAD_REQUEST)
        return Response(data, status=status.HTTP_201_CREATED)
