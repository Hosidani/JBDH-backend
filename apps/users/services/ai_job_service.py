from rest_framework import status
from apps.ai_services.services.user_ai_job_matching_service import UserAIJobMatchingService
from rest_framework.exceptions import ValidationError

class AIJobService:
    """AI Job matching & vector search"""
    
    @staticmethod
    def vector_search_jobs(user, language, top_k, min_score):
        """Validering + session skapande"""
        
        # Input validation (från view)
        top_k = int(top_k) if top_k else 5
        min_score = float(min_score) if min_score else 0.2
        
        top_k = max(1, min(top_k, 20))
        min_score = max(-1.0, min(min_score, 1.0))
        language = (language or 'English').strip()
        
        # Kall din befintliga service
        result = UserAIJobMatchingService().create_vector_search_session(
            user=user,
            top_k=top_k,
            min_score=min_score,
            language=language,
        )
        
        if not result.get('success'):
            raise ValidationError(result.get('error', 'Vector search failed'))
        
        return result