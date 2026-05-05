import os
import tempfile
import logging

logger = logging.getLogger(__name__)

from django.shortcuts import get_object_or_404

from apps.ai_services.models import ExternalCV, ExternalJobAd
from apps.ai_services.serializers import ExternalCVSerializer, ExternalJobAdSerializer
from apps.ai_services.services.file_text_extraction_service import extract_text_from_file


def _name_from_text(text: str) -> str:
    """Generate a short display name from the first 3 words of text."""
    words = text.split()[:3]
    return ' '.join(words)[:60] if words else 'Namnlös'


class ExternalCVService:

    @staticmethod
    def list(user):
        cvs = ExternalCV.objects.filter(user=user)
        return ExternalCVSerializer(cvs, many=True).data

    @staticmethod
    def create(data, user):
        text = data.get('text', '') or ''
        name = (data.get('name', '') or '').strip()
        if not name:
            name = _name_from_text(text)
        serializer = ExternalCVSerializer(data={'text': text, 'name': name})
        if serializer.is_valid():
            cv_obj = serializer.save(user=user)

            # --- Batch-matchning mot alla användarens jobb ---
            from apps.ai_services.models import ExternalJobAd, ATSResult
            from core.ats_client import score_cv_job_ats
            job_ads = ExternalJobAd.objects.filter(user=user)
            matches_created = 0
            for job in job_ads:
                # Duplicate check: exact text match (startswith was too broad and
                # caused CV 66 and CV 66(1) to be treated as identical).
                exists = ATSResult.objects.filter(
                    user=user,
                    cv_text=text,
                    job_text=job.text,
                ).exists()
                if not exists:
                    logger.info(f"Kör ATS-motor för user={user.id}, CV={cv_obj.id if cv_obj else '-'}, JOB={job.id if job else '-'}")
                    ats_json = score_cv_job_ats(text, job.text)  # eller (cv.text, text)
                    ATSResult.objects.create(
                        user=user,
                        cv_text=text,
                        job_text=job.text,
                        ats_json=ats_json,
                        overall_score=ats_json.get('overall_score') or ats_json.get('total_score') or ats_json.get('totalScore') or 0
                    )
                    matches_created += 1
                    logger.info(f"ATSResult: Skapar matchning user={user.id} cv={cv_obj.id if cv_obj else '-'} job={job.id if job else '-'}")

            result = serializer.data.copy()
            result['matches_created'] = matches_created
            return result, None
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
        from apps.ai_services.models import ATSResult
        ATSResult.objects.filter(user=user, cv_text=cv.text).delete()
        cv.delete()

    @staticmethod
    def upload_pdf(uploaded_file, user, name: str = ''):
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

        display_name = name.strip() if name and name.strip() else os.path.splitext(uploaded_file.name)[0][:60]
        cv = ExternalCV.objects.create(user=user, name=display_name, text=cv_text)

        # --- Batch-matchning mot alla användarens jobb ---
        from apps.ai_services.models import ExternalJobAd, ATSResult
        from core.ats_client import score_cv_job_ats
        job_ads = ExternalJobAd.objects.filter(user=user)
        for job in job_ads:
            exists = ATSResult.objects.filter(
                user=user,
                cv_text=cv_text,
                job_text=job.text,
            ).exists()
            if not exists:
                logger.info(f"Kör ATS-motor för user={user.id}, CV={cv.id}, JOB={job.id}")
                try:
                    ats_json = score_cv_job_ats(cv_text, job.text)
                    ATSResult.objects.create(
                        user=user,
                        cv_text=cv_text,
                        job_text=job.text,
                        ats_json=ats_json,
                        overall_score=ats_json.get('total_score') or ats_json.get('overall_score') or ats_json.get('totalScore') or 0
                    )
                except Exception as e:
                    logger.error(f"ATS-matchning misslyckades för CV={cv.id} JOB={job.id}: {e}")

        return ExternalCVSerializer(cv).data, None


class ExternalJobAdService:

    @staticmethod
    def list(user):
        job_ads = ExternalJobAd.objects.filter(user=user)
        return ExternalJobAdSerializer(job_ads, many=True).data

    @staticmethod
    def create(data, user):
        text = data.get('text', '') or ''
        name = (data.get('name', '') or '').strip()
        if not name:
            name = _name_from_text(text)
        serializer = ExternalJobAdSerializer(data={'text': text, 'name': name})
        if serializer.is_valid():
            job_obj = serializer.save(user=user)

            # --- Batch-matchning mot alla användarens CV:n ---
            from apps.ai_services.models import ExternalCV, ATSResult
            from core.ats_client import score_cv_job_ats
            cvs = ExternalCV.objects.filter(user=user)
            matches_created = 0
            for cv in cvs:
                # Duplicate check: exact text match (startswith was too broad).
                exists = ATSResult.objects.filter(
                    user=user,
                    cv_text=cv.text,
                    job_text=text,
                ).exists()
                if not exists:
                    logger.info(f"Kör ATS-motor för user={user.id}, CV={cv.id if cv else '-'}, JOB={job_obj.id if job_obj else '-'}")
                    ats_json = score_cv_job_ats(cv.text, text)  # eller (cv.text, text)
                    ATSResult.objects.create(
                        user=user,
                        cv_text=cv.text,
                        job_text=text,
                        ats_json=ats_json,
                        overall_score=ats_json.get('overall_score') or ats_json.get('total_score') or ats_json.get('totalScore') or 0
                    )
                    matches_created += 1
                    logger.info(f"ATSResult: Skapar matchning user={user.id} cv={cv.id if cv else '-'} job={job_obj.id if job_obj else '-'}")

            result = serializer.data.copy()
            result['matches_created'] = matches_created
            return result, None
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
        from apps.ai_services.models import ATSResult
        ATSResult.objects.filter(user=user, job_text=job_ad.text).delete()
        job_ad.delete()
