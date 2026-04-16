from django.contrib.auth import get_user_model
from django.contrib.auth.password_validation import validate_password
from django.core.exceptions import ValidationError

User = get_user_model()

class AuthService:
    """Business logic för authentication."""
    
    @staticmethod
    def change_password(user, current_password: str, new_password: str, confirm_password: str):
        """Kärnlogik - returnerar dict (success/error)."""
        
        # 1. Verifiera current password
        if not user.check_password(current_password):
            return {"success": False, "error": "Current password is incorrect."}
        
        # 2. Validera new password
        if new_password != confirm_password:
            return {"success": False, "error": "New password and confirmation do not match."}
        
        try:
            validate_password(new_password, user)  # Django's built-in validators!
        except ValidationError as e:
            return {"success": False, "error": list(e.messages)[0]}
        
        # 3. Uppdatera password
        user.set_password(new_password)
        user.save(update_fields=['password'])
        
        return {"success": True, "message": "Password changed successfully."}