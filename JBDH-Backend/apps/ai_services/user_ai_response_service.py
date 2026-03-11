# apps/ai_services/user_ai_response_service.py
from apps.ai_services.models import UserAIResponse
from django.db.models import Q
import json

class UserAIResponseService:
    def get_all_user_ai_responses(self, user):
        """Return all AI responses for a user, newest first"""
        return UserAIResponse.objects.filter(user=user).order_by('-created_at')

    def get_latest_user_ai_response(self, user):
        """Return latest AI response for a user"""
        return UserAIResponse.objects.filter(user=user).order_by('-created_at').first()

    def save_user_ai_response(self, user, response_text):
        """Save a new AI response for a user"""
        ai_response = UserAIResponse(user=user, response_text=response_text)
        ai_response.save()
        return ai_response

    def get_all_user_ai_response_dtos(self, user):
        """Return all AI responses as dicts with parsed analysis"""
        responses = self.get_all_user_ai_responses(user)
        dtos = []
        for r in responses:
            analysis = None
            raw = r.response_text or ""
            try:
                if raw:
                    # Clean code block markers if needed (implement if required)
                    json_to_parse = raw
                    # Unescape if double-encoded
                    if (json_to_parse.startswith('"') and json_to_parse.endswith('"')) or '\\n' in json_to_parse or '\\"' in json_to_parse:
                        json_to_parse = json.loads(json_to_parse)
                    if json_to_parse:
                        analysis = json.loads(json_to_parse)
            except Exception:
                pass
            dtos.append({
                "id": r.id,
                "user_id": r.user.id,
                "response_text": raw,
                "created_at": r.created_at,
                "analysis": analysis
            })
        return dtos