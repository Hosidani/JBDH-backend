from rest_framework.views import APIView
from drf_spectacular.utils import extend_schema
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import AllowAny, IsAuthenticated
from django.contrib.auth import login, logout
from django.middleware.csrf import get_token
from apps.authentication.serializers import (
    RegisterUserSerializer,
    LoginSerializer,
    UserSerializer
)
from apps.authentication.services.registration_service import RegistrationService
from apps.authentication.services.login_service import LoginService
import logging
from django.views.decorators.csrf import csrf_exempt
from django.utils.decorators import method_decorator

logger = logging.getLogger(__name__)

@method_decorator(csrf_exempt, name='dispatch')
class RegisterUserView(APIView):
    """
    POST /api/Register/RegisterUser
    Register a new user account
    """
    permission_classes = [AllowAny]
    
    @extend_schema(
        request=RegisterUserSerializer,
        responses={201: UserSerializer}
    )
    def post(self, request):
        logger.info(f"Raw request data: {request.data}")
        
        # FIX: Explicit mappning för ALLA kända CamelCase fält
        normalized_data = {
            'email': request.data.get('Email') or request.data.get('email') or request.data.get('email'),
	    'password': request.data.get('Password') or request.data.get('password'),
  	    'confirm_password': (
            request.data.get('ConfirmPassword') or 
            request.data.get('confirmPassword') or 
            request.data.get('confirm_password')  # ← LÄGG TILL!
        ),
           'location': request.data.get('Location') or request.data.get('location', ''),
           'preferred_language': request.data.get('PreferredLanguage') or request.data.get('preferred_language', 'sv'),
           'open': str(request.data.get('Open') or request.data.get('open', 'false')).lower() == 'true',
           'selected_roles': request.data.get('SelectedRoles') or request.data.get('selected_roles', [])
        }
        
        # Ta bort None-värden
        normalized_data = {k: v for k, v in normalized_data.items() if v is not None}
        
        logger.info(f"Normalized data: {normalized_data}")
        
        serializer = RegisterUserSerializer(data=normalized_data)
        
        if serializer.is_valid():
            registration_service = RegistrationService()
            result = registration_service.register_user(serializer.validated_data, request)
            
            if result['success']:
                return Response({'message': result['message']}, status=status.HTTP_200_OK)
            else:
                return Response({'error': result['error']}, status=status.HTTP_409_CONFLICT)
        
        logger.error(f"Validation errors: {serializer.errors}")
        return Response({
            'error': 'Invalid registration data.',
            'details': serializer.errors,
            'received': normalized_data  # Debug-info
        }, status=status.HTTP_400_BAD_REQUEST)


class LoginView(APIView):
    """
    POST /api/Login
    Authenticate user and create session
    """
    permission_classes = [AllowAny]
    
    @extend_schema(
        request=LoginSerializer,
        responses={200: UserSerializer}
    )
    def post(self, request):
        serializer = LoginSerializer(data=request.data)
        
        if not serializer.is_valid():
            return Response(
                {'error': 'Invalid login data.', 'details': serializer.errors},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        email = serializer.validated_data['email']
        password = serializer.validated_data['password']
        
        login_service = LoginService()
        result = login_service.validate_user(email, password)
        
        if not result['success']:
            return Response(
                {'error': result['error']},
                status=status.HTTP_401_UNAUTHORIZED
            )
        
        # Create session
        user = result['user']
        login(request, user)
        
        # Sätt CSRF token cookie
        get_token(request)  # <-- Lägg till denna rad
        
        logger.info(f"User {email} logged in successfully")
        
        return Response(
            {'message': 'Login successful'},
            status=status.HTTP_200_OK
        )


class LogoutView(APIView):
    """
    GET/POST /api/Logout
    Logout current user
    """
    permission_classes = [IsAuthenticated]
    
    def get(self, request):
        return self._logout(request)
    
    def post(self, request):
        return self._logout(request)
    
    def _logout(self, request):
        logout(request)
        return Response(
            {'message': 'Logged out successfully'},
            status=status.HTTP_200_OK
        )


class VerifyEmailView(APIView):
    """
    GET /api/Register/VerifyEmail?token=<token>
    Verify user email with token
    """
    permission_classes = [AllowAny]
    
    def get(self, request):
        token = request.query_params.get('token')
        
        if not token:
            return Response(
                {'error': 'Verification token is required.'},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        registration_service = RegistrationService()
        result = registration_service.verify_email(token)
        
        if not result['success']:
            return Response(
                {'error': result['error']},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        return Response(
            {'message': result['message']},
            status=status.HTTP_200_OK
        )


class ResendVerificationEmailView(APIView):
    """
    POST /api/Register/ResendVerificationEmail
    Resend verification email
    """
    permission_classes = [AllowAny]
    
    def post(self, request):
        email = request.data.get('email')
        
        if not email:
            return Response(
                {'error': 'Email is required.'},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        registration_service = RegistrationService()
        result = registration_service.resend_verification_email(email, request)
        
        if not result['success']:
            return Response(
                {'error': result['error']},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        return Response(
            {'message': result['message']},
            status=status.HTTP_200_OK
        )
