import uuid
import os
from datetime import timedelta
from django.utils import timezone
from django.db import transaction
from django.conf import settings
from apps.users.models import (
    User, Account, UserRoleAssignment, EmailVerificationToken,
    UserRole, AccountType
)
from apps.authentication.services.email_service import EmailService
import logging

logger = logging.getLogger(__name__)


class RegistrationService:
    
    def __init__(self):
        self.email_service = EmailService()
    
    @transaction.atomic
    def register_user(self, validated_data, request):
        """
        Register a new user with account and send verification email.
        Returns: dict with success status and message or error
        """
        email = validated_data['email']
        password = validated_data['password']
        location = validated_data.get('location', '')
        searchable = validated_data.get('open', False)
        preferred_language = validated_data.get('preferred_language', 'en')
        selected_roles = validated_data.get('selected_roles', [])
        cv_file = validated_data.get('cv_file')
        
        # Determine primary role
        primary_role = UserRole.USER
        if selected_roles:
            first_role = selected_roles[0]
            if first_role in [UserRole.RECRUITER, UserRole.MANAGER]:
                primary_role = first_role
        
        # Create user
        user = User.objects.create_user(
            email=email,
            password=password,
            location=location,
            role=primary_role,
            searchable=searchable,
            developer_mode=False,
            is_active=True  # Aktiverad direkt för utveckling
        )
        
        # Add role assignment
        UserRoleAssignment.objects.create(
            user=user,
            role=primary_role
        )
        
        # Handle CV upload
        if cv_file:
            cv_path = self._save_cv_file(cv_file, user.id)
            user.cv_path = cv_path
            user.save(update_fields=['cv_path'])
        
        # Create default account
        Account.objects.create(
            user=user,
            account_name=user.name or user.email,
            account_email=user.email,
            preferred_language=preferred_language,
            account_type=AccountType.BASIC,
            accepted_terms_and_conditions=False
        )
        
        # Generate verification token
        token = str(uuid.uuid4())
        expiration = timezone.now() + timedelta(days=1)
        
        EmailVerificationToken.objects.create(
            token=token,
            user=user,
            expiration_date=expiration,
            is_used=False
        )
        
        # Send verification email
        email_result = self._send_verification_email(user, token, request)
        
        if not email_result['success']:
            logger.error(f"Failed to send verification email: {email_result['error']}")
            return {
                'success': False,
                'error': f"Registration succeeded, but failed to send verification email: {email_result['error']}"
            }
        
        logger.info(f"User registered successfully: {email}")
        
        # TODO: Trigger background AI CV analysis task (using Celery)
        # self._trigger_cv_analysis(user.id)
        
        return {
            'success': True,
            'message': 'Registration successful! Please check your email.',
            'user_id': user.id
        }
    
    def _save_cv_file(self, cv_file, user_id):
        """Save uploaded CV file and return path"""
        uploads_dir = os.path.join(settings.MEDIA_ROOT, 'uploads', 'cvs')
        os.makedirs(uploads_dir, exist_ok=True)
        
        # Generate safe filename
        file_extension = os.path.splitext(cv_file.name)[1]
        filename = f"cv_user_{user_id}_{uuid.uuid4().hex[:8]}{file_extension}"
        file_path = os.path.join(uploads_dir, filename)
        
        with open(file_path, 'wb+') as destination:
            for chunk in cv_file.chunks():
                destination.write(chunk)
        
        # Return relative path for storage
        return os.path.join('uploads', 'cvs', filename)
    
    def _send_verification_email(self, user, token, request):
        """Send email verification link"""
        try:
            # Build verification URL
            scheme = 'https' if request.is_secure() else 'http'
            host = request.get_host()
            verification_url = f"{scheme}://{host}/api/Register/VerifyEmail?token={token}"
            
            subject = "Verify your email"
            message = f"Please verify your email by clicking this link: {verification_url}"
            
            result = self.email_service.send_email(
                to_email=user.email,
                subject=subject,
                body=message
            )
            
            if result == "Success":
                return {'success': True}
            else:
                return {'success': False, 'error': result}
                
        except Exception as e:
            logger.exception("Exception sending verification email")
            return {'success': False, 'error': str(e)}
    
    def verify_email(self, token):
        """Verify user email with token"""
        try:
            verification_token = EmailVerificationToken.objects.select_related('user').get(
                token=token
            )
            
            if not verification_token.is_valid():
                return {
                    'success': False,
                    'error': 'Verification token is invalid or expired.'
                }
            
            # Activate user
            user = verification_token.user
            user.is_active = True
            user.save(update_fields=['is_active'])
            
            # Mark token as used
            verification_token.is_used = True
            verification_token.save(update_fields=['is_used'])
            
            logger.info(f"Email verified for user: {user.email}")
            
            return {
                'success': True,
                'message': 'Email verified successfully!'
            }
            
        except EmailVerificationToken.DoesNotExist:
            return {
                'success': False,
                'error': 'Invalid verification token.'
            }
    
    def resend_verification_email(self, email, request):
        """Resend verification email"""
        try:
            user = User.objects.get(email=email.lower())
            
            if user.is_active:
                return {
                    'success': False,
                    'error': 'Email is already verified.'
                }
            
            # Invalidate old tokens
            EmailVerificationToken.objects.filter(user=user, is_used=False).update(is_used=True)
            
            # Create new token
            token = str(uuid.uuid4())
            expiration = timezone.now() + timedelta(days=1)
            
            EmailVerificationToken.objects.create(
                token=token,
                user=user,
                expiration_date=expiration,
                is_used=False
            )
            
            # Send email
            email_result = self._send_verification_email(user, token, request)
            
            if not email_result['success']:
                return {
                    'success': False,
                    'error': f"Failed to resend verification email: {email_result['error']}"
                }
            
            return {
                'success': True,
                'message': 'Verification email resent successfully.'
            }
            
        except User.DoesNotExist:
            # Don't reveal if email exists
            return {
                'success': True,
                'message': 'If your email is in our system, a verification email has been sent.'
            }
