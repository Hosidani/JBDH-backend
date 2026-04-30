from rest_framework import serializers
from .models import ATSResult, ExternalCV, ExternalJobAd


class ATSResultPreviewSerializer(serializers.ModelSerializer):
    job_name = serializers.SerializerMethodField()
    cv_name = serializers.SerializerMethodField()

    class Meta:
        model = ATSResult
        fields = ['id', 'job_name', 'cv_name', 'overall_score', 'created_at']

    def get_job_name(self, obj):
        job = ExternalJobAd.objects.filter(user=obj.user, text=obj.job_text).first()
        return job.name if job else ""

    def get_cv_name(self, obj):
        cv = ExternalCV.objects.filter(user=obj.user, text=obj.cv_text).first()
        return cv.name if cv else ""


class ATSResultDetailSerializer(serializers.ModelSerializer):
    job_name = serializers.SerializerMethodField()
    cv_name = serializers.SerializerMethodField()

    class Meta:
        model = ATSResult
        fields = [
            'id',
            'job_name',
            'cv_name',
            'overall_score',
            'created_at',
            'cv_text',
            'job_text',
            'ats_json'
        ]

    def get_job_name(self, obj):
        job = ExternalJobAd.objects.filter(user=obj.user, text=obj.job_text).first()
        return job.name if job else ""

    def get_cv_name(self, obj):
        cv = ExternalCV.objects.filter(user=obj.user, text=obj.cv_text).first()
        return cv.name if cv else ""


class ExternalCVSerializer(serializers.ModelSerializer):
    class Meta:
        model = ExternalCV
        fields = ['id', 'name', 'text', 'created_at']
        read_only_fields = ['id', 'created_at']


class ExternalJobAdSerializer(serializers.ModelSerializer):
    class Meta:
        model = ExternalJobAd
        fields = ['id', 'name', 'text', 'created_at']
        read_only_fields = ['id', 'created_at']
