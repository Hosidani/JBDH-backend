import os
import tempfile

from django.shortcuts import get_object_or_404

from apps.ai_services.models import ExternalCV, ExternalJobAd
from apps.ai_services.serializers import ExternalCVSerializer, ExternalJobAdSerializer
from apps.ai_services.services.file_text_extraction_service import extract_text_from_file


class ExternalCVService:

    @staticmethod
    def list(user):
        cvs = ExternalCV.objects.filter(user=user)
        return ExternalCVSerializer(cvs, many=True).data

    @staticmethod
    def create(data, user):
        serializer = ExternalCVSerializer(data=data)
        if serializer.is_valid():
            serializer.save(user=user)
            return serializer.data, None
        return None, serializer.errors

    @staticmethod
    def get(pk, user):
        cv = get_object_or_404(ExternalCV, pk=pk, user=user)
        return ExternalCVSerializer(cv).data

    @staticmethod
    def update(pk, user, data, partial=False):
        cv = get_object_or_404(ExternalCV, pk=pk, user=user)
        serializer = ExternalCVSerializer(cv, data=data, partial=partial)
        if serializer.is_valid():
            serializer.save()
            return serializer.data, None
        return None, serializer.errors

    @staticmethod
    def delete(pk, user):
        cv = get_object_or_404(ExternalCV, pk=pk, user=user)
        cv.delete()

    @staticmethod
    def upload_pdf(uploaded_file, user):
        """Extract text from uploaded PDF/DOCX and save as ExternalCV."""
        suffix = os.path.splitext(uploaded_file.name)[1]
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as tmp:
            for chunk in uploaded_file.chunks():
                tmp.write(chunk)
            tmp_path = tmp.name

        try:
            cv_text = (extract_text_from_file(tmp_path) or '').strip()
        finally:
            os.unlink(tmp_path)

        if not cv_text:
            return None, 'Kunde inte extrahera text ur filen.'

        label = os.path.splitext(uploaded_file.name)[0][:60]
        cv = ExternalCV.objects.create(user=user, text=f"[{label}]\n{cv_text}")
        return ExternalCVSerializer(cv).data, None


class ExternalJobAdService:

    @staticmethod
    def list(user):
        job_ads = ExternalJobAd.objects.filter(user=user)
        return ExternalJobAdSerializer(job_ads, many=True).data

    @staticmethod
    def create(data, user):
        serializer = ExternalJobAdSerializer(data=data)
        if serializer.is_valid():
            serializer.save(user=user)
            return serializer.data, None
        return None, serializer.errors

    @staticmethod
    def get(pk, user):
        job_ad = get_object_or_404(ExternalJobAd, pk=pk, user=user)
        return ExternalJobAdSerializer(job_ad).data

    @staticmethod
    def update(pk, user, data, partial=False):
        job_ad = get_object_or_404(ExternalJobAd, pk=pk, user=user)
        serializer = ExternalJobAdSerializer(job_ad, data=data, partial=partial)
        if serializer.is_valid():
            serializer.save()
            return serializer.data, None
        return None, serializer.errors

    @staticmethod
    def delete(pk, user):
        job_ad = get_object_or_404(ExternalJobAd, pk=pk, user=user)
        job_ad.delete()
