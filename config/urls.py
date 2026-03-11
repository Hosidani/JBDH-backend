"""
URL configuration for config project.

The `urlpatterns` list routes URLs to views. For more information please see:
    https://docs.djangoproject.com/en/5.2/topics/http/urls/
Examples:
Function views
    1. Add an import:  from my_app import views
    2. Add a URL to urlpatterns:  path('', views.home, name='home')
Class-based views
    1. Add an import:  from other_app.views import Home
    2. Add a URL to urlpatterns:  path('', Home.as_view(), name='home')
Including another URLconf
    1. Import the include() function: from django.urls import include, path
    2. Add a URL to urlpatterns:  path('blog/', include('blog.urls'))
"""
from django.contrib import admin
from django.urls import path, include
from django.conf import settings
from django.conf.urls.static import static
from apps.users import views
from apps.authentication.views import RegisterUserView, LoginView, LogoutView
from drf_spectacular.views import SpectacularAPIView, SpectacularSwaggerView
from apps.users.views import analyze_cv
from apps.ai_services.views import compare_cv_to_job_ad_v2
from apps.jobs.views import search_for_jobs, search_v2

urlpatterns = [
    path('admin/', admin.site.urls),
    path('api/', include('apps.authentication.urls')),
    path('api/', include('apps.users.urls')),
    # Compare CV to Job Ad V2 (frontend: POST /api/Home/CompareCvToJobAdV2)
    path('api/Home/CompareCvToJobAdV2', compare_cv_to_job_ad_v2, name='compare-cv-to-job-ad-v2'),

    # Frontend-kompatibla alias (PascalCase)
    path('api/profile/getmyprofile/', views.get_my_profile, name='get-my-profile-alias'),
    path('api/profile/getprofile/<int:user_id>/', views.get_profile, name='get-profile-alias'),
    # Frontend-kompatibla alias UTAN /api/ prefix
    path('Register/RegisterUser', RegisterUserView.as_view(), name='register-no-api'),
    path('login', LoginView.as_view(), name='login-no-api'),
    path('logout', LogoutView.as_view(), name='logout-no-api'),
    path('profile/analyzecv/', analyze_cv, name='analyze-cv'),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)

urlpatterns += [
    path('api/schema/', SpectacularAPIView.as_view(), name='schema'),
    path('api/doc/', SpectacularSwaggerView.as_view(url_name='schema'), name='swagger-ui'),
]

urlpatterns += [
    path('', include('apps.organisations.urls')),
    path('api/Jobs/', include('apps.jobs.urls')),
    path('api/jobs/', include('apps.jobs.urls')),
    path('api/Search/SearchForJobs', search_for_jobs, name='search_for_jobs'),
    path('api/Search/SearchV2', search_v2, name='search_v2'),
]
