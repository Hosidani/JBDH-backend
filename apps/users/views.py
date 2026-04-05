from rest_framework import status
from rest_framework.decorators import api_view, permission_classes, parser_classes
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from rest_framework.parsers import MultiPartParser, FormParser
from django.shortcuts import get_object_or_404
from apps.users.models import User
from apps.ai_services.models import UserAIResponse
from .serializers import (
    PublicUserProfileSerializer,
    MyProfileSerializer,
    UserAIResponseSerializer,
)
from apps.ai_services.services.cv_analysis_service import CVAnalysisService
from apps.ai_services.services.user_ai_job_matching_service import UserAIJobMatchingService
try:
    from apps.ai_services.domain.exceptions import MissingRequiredFileError
except ImportError:
    from apps.ai_services.domain.exceptions import MissingRequiredFileError
from rest_framework.views import APIView
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from rest_framework import status
from .serializers import UserSettingsSerializer
from .serializers import ChangePasswordSerializer
from .models import Account
from .serializers import AccountSettingsSerializer
import logging
logger = logging.getLogger(__name__)
from apps.organisations.models import Organisation
from django.utils import timezone

@api_view(['GET'])
@permission_classes([IsAuthenticated])
def get_profile(request, user_id):
    """
    GET /profile/getprofile/<user_id>
    Get another user's profile (limited public data)
    """
    current_user = request.user
    
    if current_user.id == user_id:
        return Response(
            {"error": "Use getmyprofile endpoint for your own profile"},
            status=status.HTTP_400_BAD_REQUEST
        )
    
    target_user = get_object_or_404(User, id=user_id)
    serializer = PublicUserProfileSerializer(target_user)
    return Response(serializer.data, status=status.HTTP_200_OK)


@api_view(['GET'])
@permission_classes([IsAuthenticated])
def get_my_profile(request):
    """
    GET /profile/getmyprofile
    Get current user's complete profile with all data
    """
    user = request.user
    serializer = MyProfileSerializer(user)
    return Response(serializer.data, status=status.HTTP_200_OK)


@api_view(['POST'])
@permission_classes([IsAuthenticated])
@parser_classes([MultiPartParser, FormParser])

def analyze_cv(request):
    """POST /profile/analyzecv: Upload CV and run AI analysis via service layer."""
    logger.info("AnalyzeCV endpoint called")
    service = CVAnalysisService()
    result = service.analyze_uploaded_cv(
        user=request.user,
        cv_file=request.FILES.get('cv_file'),
        language_raw=request.data.get('language', 'English')
    )
    return Response(result)


@api_view(['GET'])
@permission_classes([IsAuthenticated])
def latest_ai_user_response(request):
    """
    GET /profile/latestaiuserresponse
    Get the latest AI analysis for current user
    """
    user = request.user
    ai_response = user.ai_responses.first()
    
    if not ai_response:
        return Response(
            {"error": "No AI analysis found for this user."},
            status=status.HTTP_404_NOT_FOUND
        )
    
    serializer = UserAIResponseSerializer(ai_response)
    return Response(serializer.data, status=status.HTTP_200_OK)


@api_view(['POST'])
@permission_classes([IsAuthenticated])
def vector_search_jobs(request):
    language = (request.data.get('language') or 'English').strip()
    top_k_raw = request.data.get('topK')
    min_score_raw = request.data.get('minScore')

    try:
        top_k = int(top_k_raw) if top_k_raw is not None else 5
    except (TypeError, ValueError):
        top_k = 5

    try:
        min_score = float(min_score_raw) if min_score_raw is not None else 0.2
    except (TypeError, ValueError):
        min_score = 0.2

    top_k = max(1, min(top_k, 20))
    min_score = max(-1.0, min(min_score, 1.0))

    result = UserAIJobMatchingService().create_vector_search_session(
        user=request.user,
        top_k=top_k,
        min_score=min_score,
        language=language,
    )

    if not result.get('success'):
        return Response({"error": result.get('error', 'Vector search failed')}, status=status.HTTP_400_BAD_REQUEST)

    return Response(result, status=status.HTTP_200_OK)


@api_view(['POST'])
@permission_classes([IsAuthenticated])
def ai_match_jobs(request):
    session_id = (request.data.get('sessionId') or '').strip()
    language = (request.data.get('language') or 'English').strip()
    job_id_raw = request.data.get('jobId')

    if not session_id:
        return Response({"error": "sessionId is required."}, status=status.HTTP_400_BAD_REQUEST)

    job_id = None
    if job_id_raw is not None:
        try:
            job_id = int(job_id_raw)
        except (TypeError, ValueError):
            return Response({"error": "jobId must be an integer."}, status=status.HTTP_400_BAD_REQUEST)

    result = UserAIJobMatchingService().ai_match_from_session(
        user=request.user,
        session_id=session_id,
        language=language,
        job_id=job_id,
    )

    if not result.get('success'):
        return Response({"error": result.get('error', 'AI matching failed')}, status=status.HTTP_400_BAD_REQUEST)

    return Response(result, status=status.HTTP_200_OK)


class UpdateUserSettingsView(APIView):
    permission_classes = [IsAuthenticated]

    def put(self, request):
        user = request.user
        serializer = UserSettingsSerializer(user, data=request.data)
        if serializer.is_valid():
            serializer.save()
            return Response({'message': 'Profile updated successfully.', 'profile': serializer.data}, status=status.HTTP_200_OK)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class ChangePasswordView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request):
        serializer = ChangePasswordSerializer(data=request.data)
        if not serializer.is_valid():
            return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)

        user = request.user
        current = serializer.validated_data['currentPassword']
        new = serializer.validated_data['newPassword']
        confirm = serializer.validated_data['confirmNewPassword']

        if not user.check_password(current):
            return Response({'error': 'Current password is incorrect.'}, status=status.HTTP_400_BAD_REQUEST)
        if len(new) < 6:
            return Response({'error': 'New password must be at least 6 characters.'}, status=status.HTTP_400_BAD_REQUEST)
        if new != confirm:
            return Response({'error': 'New password and confirmation do not match.'}, status=status.HTTP_400_BAD_REQUEST)

        user.set_password(new)
        user.save()
        return Response({'message': 'Password changed successfully.'}, status=status.HTTP_200_OK)




class DeleteMyUserView(APIView):
    permission_classes = [IsAuthenticated]

    def delete(self, request):
        user = request.user
        user.delete()
        return Response({'message': 'Your account has been deleted.'}, status=status.HTTP_200_OK)

class GetAccountSettingsView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        user = request.user
        try:
            account = Account.objects.get(user=user)
        except Account.DoesNotExist:
            return Response({'error': 'Account not found.'}, status=status.HTTP_404_NOT_FOUND)

        serializer = AccountSettingsSerializer(account)
        return Response(serializer.data, status=status.HTTP_200_OK)

class GetAvailableOrganisationsView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        user = request.user
        member_ids = set(user.organisations.values_list('id', flat=True))
        manager_ids = set(user.managed_organisations.values_list('id', flat=True))
        exclude_ids = member_ids | manager_ids
        available_orgs = Organisation.objects.exclude(id__in=exclude_ids)
        data = [{'id': org.id, 'name': org.name} for org in available_orgs]
        return Response(data)

class UpdateAccountSettingsView(APIView):
    permission_classes = [IsAuthenticated]

    def put(self, request):
        user = request.user
        account, created = Account.objects.get_or_create(user=user)
        serializer = AccountSettingsSerializer(account, data=request.data, partial=True)
        if serializer.is_valid():
            serializer.save()
            return Response({'message': 'Account updated', 'account': serializer.data})
        return Response(serializer.errors, status=400)

class TerminateAccountView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request):
        user = request.user
        try:
            account = Account.objects.get(user=user, is_terminated=False)
        except Account.DoesNotExist:
            return Response({'error': 'No active account found.'}, status=404)
        account.is_terminated = True
        account.terminated_at = timezone.now()
        account.save()
        return Response({'message': 'Account terminated'})


