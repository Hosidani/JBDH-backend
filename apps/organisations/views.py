from django.shortcuts import render
from rest_framework.views import APIView
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from .models import Organisation
from .serializers import OrganisationSerializer

# Create your views here.

class GetAvailableOrganisationsView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        user = request.user
        member_ids = list(user.organisations.values_list('id', flat=True))
        manager_ids = list(user.managed_organisations.values_list('id', flat=True))
        exclude_ids = set(member_ids + manager_ids)
        available_orgs = Organisation.objects.exclude(id__in=exclude_ids)
        data = [
            {'id': org.id, 'name': org.name, 'role': ''}
            for org in available_orgs
        ]
        return Response(data)
