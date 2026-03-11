from django.urls import path
from .views import GetAvailableOrganisationsView

urlpatterns = [
    path('api/Settings/getavailableorganisations', GetAvailableOrganisationsView.as_view()),
]