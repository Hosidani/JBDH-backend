from django.urls import path
from .views import (
    ExternalCVListCreateView,
    ExternalCVDetailView,
    ExternalJobAdListCreateView,
    ExternalJobAdDetailView,
    ExternalCVUploadPDFView,
    ats_score_view,
)

urlpatterns = [
    # External CVs
    path('external-cvs/', ExternalCVListCreateView.as_view(), name='external-cv-list-create'),
    path('external-cvs/<int:pk>/', ExternalCVDetailView.as_view(), name='external-cv-detail'),
    path('external-cvs/upload-pdf/', ExternalCVUploadPDFView.as_view(), name='external-cv-upload-pdf'),

    # External Job Ads
    path('external-job-ads/', ExternalJobAdListCreateView.as_view(), name='external-job-ad-list-create'),
    path('external-job-ads/<int:pk>/', ExternalJobAdDetailView.as_view(), name='external-job-ad-detail'),

    # ATS Score
    path('ats/score', ats_score_view, name='ats-score'),
]
