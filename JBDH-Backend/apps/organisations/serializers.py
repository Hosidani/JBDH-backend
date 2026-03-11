from rest_framework import serializers
from .models import Organisation

class OrganisationSerializer(serializers.ModelSerializer):
    role = serializers.CharField(default='')

    class Meta:
        model = Organisation
        fields = ['id', 'name', 'role']