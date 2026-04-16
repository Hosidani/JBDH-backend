# Clean imports (1x varje)
from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import IsAuthenticated
from rest_framework.decorators import api_view, permission_classes, parser_classes
from rest_framework.parsers import MultiPartParser, FormParser

# Services
from .services.ai_job_service import AIJobService
from .services.ai_match_jobs_service import AIMatchService
from .services.account_service import AccountService
from .services.profile_service import ProfileService
from .services.auth_service import AuthService 

# Models/Serializers
from .models import Account
from .serializers import (
    PublicUserProfileSerializer, MyProfileSerializer,
    ChangePasswordSerializer, AccountSettingsSerializer
)

from django.core.exceptions import ValidationError
import logging
logger = logging.getLogger(__name__)


@api_view(['GET'])
@permission_classes([IsAuthenticated])
def get_profile(request, user_id):
    """
    GET /profile/getprofile/<user_id>
    Get another user's profile (limited public data)
    """
    data = ProfileService.get_public_profile(
        current_user=request.user,
        target_user_id=user_id,
        serializer_class=PublicUserProfileSerializer
    )
    return Response(data)


@api_view(['GET'])
@permission_classes([IsAuthenticated])
def get_my_profile(request):
    """
    GET /profile/getmyprofile
    Get current user's complete profile with all data
    """
    data = ProfileService.get_my_profile(request.user, MyProfileSerializer)
    return Response(data)


@api_view(['POST'])
@permission_classes([IsAuthenticated])
@parser_classes([MultiPartParser, FormParser])

def analyze_cv(request):
    """POST /profile/analyzecv: Upload CV and run AI analysis via service layer."""
    result = ProfileService.analyze_cv(
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
    data = ProfileService.get_latest_ai_response(request.user)
    return Response(data)


@api_view(['POST'])
@permission_classes([IsAuthenticated])
def vector_search_jobs(request):
    """POST /jobs/vectorsearch"""

    result = AIJobService.vector_search_jobs(
        user=request.user,
        language=request.data.get('language'),
        top_k=request.data.get('topK'),
        min_score=request.data.get('minScore')
    )
    return Response(result)


@api_view(['POST'])
@permission_classes([IsAuthenticated])
def ai_match_jobs(request):
    
    result = AIMatchService.ai_match_jobs(
        user=request.user,
        session_id=request.data.get("sessionId"),
        language=request.data.get("language", "English"),
        job_id=request.data.get("jobId"),
    )
    return Response(result)


class UpdateUserSettingsView(APIView):
    permission_classes = [IsAuthenticated]
        
    def put(self, request):
        result = ProfileService.update_user_settings(
            user=request.user, 
            data=request.data
        )
        return Response(result)

class ChangePasswordView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request):
        serializer = ChangePasswordSerializer(data=request.data)
        if not serializer.is_valid():
            return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)

        result = AuthService.change_password(
            user=request.user,
            current_password=serializer.validated_data['currentPassword'],
            new_password=serializer.validated_data['newPassword'],
            confirm_password=serializer.validated_data['confirmNewPassword']
        )
        
        if result["success"]:
            return Response(result, status=status.HTTP_200_OK)
        return Response({"error": result["error"]}, status=status.HTTP_400_BAD_REQUEST)


class DeleteMyUserView(APIView):
    permission_classes = [IsAuthenticated]
    
    def delete(self, request):
        result = AccountService.delete_user_account(request.user)
        return Response(result, status=status.HTTP_200_OK)

class GetAccountSettingsView(APIView):
    permission_classes = [IsAuthenticated]
    
    def get(self, request):
        try:
            account = AccountService.get_account_settings(request.user)
            serializer = AccountSettingsSerializer(account)
            return Response(serializer.data)
        except Account.DoesNotExist:
            return Response({'error': 'Account not found.'}, status=status.HTTP_404_NOT_FOUND)

class GetAvailableOrganisationsView(APIView):
    permission_classes = [IsAuthenticated]
    
    def get(self, request):
        data = AccountService.get_available_organisations(request.user)
        return Response(data)

class UpdateAccountSettingsView(APIView):
    permission_classes = [IsAuthenticated]
    
    def put(self, request):
        try:
            result = AccountService.update_account_settings(
                request.user, request.data, partial=True
            )
            return Response(result)
        except ValidationError as e:
            return Response(e.detail, status=status.HTTP_400_BAD_REQUEST)

class TerminateAccountView(APIView):
    permission_classes = [IsAuthenticated]
    
    def post(self, request):
        result = AccountService.terminate_account(request.user)
        return Response(result)