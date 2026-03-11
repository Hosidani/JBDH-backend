from django.urls import path
from apps.authentication.views import (
    RegisterUserView,
    LoginView,
    LogoutView,
    VerifyEmailView,
    ResendVerificationEmailView
)

app_name = 'authentication'

urlpatterns = [
    # Match .NET API routes exactly
    path('Register/RegisterUser', RegisterUserView.as_view(), name='register-user'),
    path('Login', LoginView.as_view(), name='login'),
    path('Logout', LogoutView.as_view(), name='logout'),
    path('Register/VerifyEmail', VerifyEmailView.as_view(), name='verify-email'),
    path('Register/ResendVerificationEmail', ResendVerificationEmailView.as_view(), name='resend-verification'),
]