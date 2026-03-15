# apps/users/urls.py
from django.urls import path
from rest_framework_simplejwt.views import TokenObtainPairView, TokenRefreshView
from . import views
from .views import ChangePasswordView, DeleteMyUserView, GetAccountSettingsView, GetAvailableOrganisationsView, UpdateAccountSettingsView, TerminateAccountView

app_name = 'users'

urlpatterns = [
    # JWT Authentication
    path('auth/login/', TokenObtainPairView.as_view(), name='token_obtain_pair'),
    path('auth/refresh/', TokenRefreshView.as_view(), name='token_refresh'),
    
    # Profile endpoints
    path('profile/getprofile/<int:user_id>/', views.get_profile, name='get-profile'),
    path('profile/getmyprofile', views.get_my_profile, name='get-my-profile'),
    path('profile/analyzecv/', views.analyze_cv, name='analyze-cv'),
    path('profile/vectorsearchjobs/', views.vector_search_jobs, name='vector-search-jobs'),
    path('profile/aimatchjobs/', views.ai_match_jobs, name='ai-match-jobs'),
    path('profile/latestaiuserresponse/', views.latest_ai_user_response, name='latest-ai-response'),
    path('settings/updateusersettings', views.UpdateUserSettingsView.as_view()),
    path('settings/changepassword', ChangePasswordView.as_view()),
    path('settings/deletemyuser', DeleteMyUserView.as_view()),
    path('settings/getaccountsettings', GetAccountSettingsView.as_view()),
    path('settings/getavailableorganisations', GetAvailableOrganisationsView.as_view()),
    path('settings/updateaccountsettings', UpdateAccountSettingsView.as_view()),
    path('settings/terminateaccount', TerminateAccountView.as_view()),
]