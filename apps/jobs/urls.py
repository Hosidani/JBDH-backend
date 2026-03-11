from django.urls import path
from . import views
from .views import (
    JobDetails, SaveJob, IsSaved, GetSavedJobs, MarkNotInterested
)

urlpatterns = [
    path('<int:id>', JobDetails.as_view()),
    path('<int:job_id>/save', SaveJob.as_view()),
    path('<int:job_id>/unsave', SaveJob.as_view()),  # DELETE
    path('<int:job_id>/issaved', IsSaved.as_view()),
    path('saved', GetSavedJobs.as_view()),
    path('<int:job_id>/not-interested', MarkNotInterested.as_view()),
]