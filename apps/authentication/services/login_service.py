from django.contrib.auth import authenticate
from apps.users.models import User
import logging

logger = logging.getLogger(__name__)


class LoginService:
    
    def validate_user(self, email, password):
        """
        Validate user credentials.
        Returns: dict with success status and message or error
        """
        if not email or not password:
            return {
                'success': False,
                'error': 'Email and password are required.'
            }
        
        # Authenticate user
        user = authenticate(username=email.lower(), password=password)
        
        if user is None:
            logger.warning(f"Failed login attempt for email: {email}")
            return {
                'success': False,
                'error': 'Invalid email or password.'
            }
        
        if not user.is_active:
            return {
                'success': False,
                'error': 'Please verify your email before logging in.'
            }
        
        if user.is_banned:
            return {
                'success': False,
                'error': 'Your account has been banned.'
            }
        
        logger.info(f"User logged in successfully: {email}")
        
        return {
            'success': True,
            'user': user
        }