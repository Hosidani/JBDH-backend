from rest_framework import serializers
from .models import ExternalCV, ExternalJobAd


class ExternalCVSerializer(serializers.ModelSerializer):
    class Meta:
        model = ExternalCV
        fields = ['id', 'text', 'created_at']
        read_only_fields = ['id', 'created_at']


class ExternalJobAdSerializer(serializers.ModelSerializer):
    class Meta:
        model = ExternalJobAd
        fields = ['id', 'text', 'created_at']
        read_only_fields = ['id', 'created_at']
