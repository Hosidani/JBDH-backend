from django.shortcuts import get_object_or_404
from ..serializers import UserAIResponseSerializer, UserSettingsSerializer
from apps.ai_services.services.cv_analysis_service import CVAnalysisService
from core.exceptions import ForbiddenError, ProfileNotFoundError
from django.core.exceptions import ValidationError

class ProfileService:
    """Alla profile-relaterade operationer"""
    
    @staticmethod
    def get_public_profile(current_user, target_user_id, serializer_class):
        """Hämta ANNAN users publika profil"""
        if current_user.id == target_user_id:
            raise ForbiddenError("Använd get_my_profile för egen profil")
        
        target_user = get_object_or_404(current_user.__class__, id=target_user_id)
        serializer = serializer_class(target_user)
        return serializer.data
    
    @staticmethod
    def get_my_profile(user, serializer_class):
        """Hämta EGEN fullständiga profil"""
        serializer = serializer_class(user)
        return serializer.data
    
    @staticmethod
    def get_latest_ai_response(user):
        """latest_ai_user_response/"""
        ai_response = user.ai_responses.first()
        if not ai_response:
            raise ProfileNotFoundError("Ingen AI-analys hittad")
        return UserAIResponseSerializer(ai_response).data
    
    @staticmethod
    def update_user_settings(user, data):
        """Uppdatera user settings/profile."""
        serializer = UserSettingsSerializer(user, data=data, partial=True)
        if serializer.is_valid():
            serializer.save()
            return {
                'message': 'Profile updated successfully.', 
                'profile': serializer.data
            }
        raise ValidationError(serializer.errors)
    
    @staticmethod
    def analyze_cv(user, cv_file, language='English'):
        service = CVAnalysisService()
        return service.analyze_uploaded_cv(user, cv_file, language)