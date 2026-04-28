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
from apps.authentication.views import FeedbackView 
from apps.users import views
from drf_spectacular.views import SpectacularAPIView, SpectacularSwaggerView
from apps.users.views import analyze_cv
from apps.ai_services.views import compare_cv_job_sse
from apps.jobs.views import search_for_jobs, search_v2
from rest_framework import permissions

if settings.DEBUG:
    from drf_yasg.views import get_schema_view
    from drf_yasg import openapi

    schema_view = get_schema_view(
        openapi.Info(
            title="JBDH API",
            default_version='v1',
            description="API documentation",
        ),
        public=True,
        permission_classes=(permissions.AllowAny,),
    )

urlpatterns = [
    path('admin/', admin.site.urls),
    path('api/', include('apps.authentication.urls')),
    path('api/', include('apps.users.urls')),
    # Compare CV to Job SSE (frontend: POST /Home/CompareCvJobSSE)
    path('api/home/CompareCvJobSSE', compare_cv_job_sse, name='compare_cv_job_sse'),

    # Frontend-kompatibla alias (PascalCase)
    path('api/profile/getmyprofile/', views.get_my_profile, name='get-my-profile-alias'),
    path('api/profile/getprofile/<int:user_id>/', views.get_profile, name='get-profile-alias'),
    # Frontend-kompatibla alias (PascalCase) under / hanteras redan via include/app-url-maps
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
    path('api/feedback/', FeedbackView.as_view(), name='feedback'),
    path('api/', include('apps.ai_services.urls')),
]

if settings.DEBUG:
    urlpatterns += [
        path('swagger/', schema_view.with_ui('swagger', cache_timeout=0), name='schema-swagger-ui'),
        path('redoc/', schema_view.with_ui('redoc', cache_timeout=0), name='schema-redoc'),
    ]
