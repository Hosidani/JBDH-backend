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


def _get_overall_score(ats_json: dict) -> float:
    """Extract overall score from ATS result regardless of key name."""
    return (
        ats_json.get('overall_score')
        or ats_json.get('total_score')
        or ats_json.get('totalScore')
        or 0
    )


def _ensure_ats_result(user, cv_text: str, job_text: str, cv_id=None, job_id=None) -> bool:
    """
    Score a single CV/job pair and persist the result if it does not already exist.

    Returns True if a new ATSResult was created, False if it already existed.
    Logs and swallows ATS errors so a single failure never blocks the others.
    """
    from apps.ai_services.models import ATSResult
    from core.ats_client import score_cv_job_ats

    if ATSResult.objects.filter(user=user, cv_text=cv_text, job_text=job_text).exists():
        return False

    logger.info(
        "Kör ATS-motor för user=%s, CV=%s, JOB=%s",
        user.id, cv_id or '-', job_id or '-',
    )
    try:
        ats_json = score_cv_job_ats(cv_text, job_text)
        ATSResult.objects.create(
            user=user,
            cv_text=cv_text,
            job_text=job_text,
            ats_json=ats_json,
            overall_score=_get_overall_score(ats_json),
        )
        logger.info(
            "ATSResult skapad: user=%s, CV=%s, JOB=%s",
            user.id, cv_id or '-', job_id or '-',
        )
        return True
    except Exception as exc:
        logger.error(
            "ATS-matchning misslyckades för CV=%s JOB=%s: %s",
            cv_id or '-', job_id or '-', exc,
        )
        return False


# ---------------------------------------------------------------------------
# ATSRelationService — gap-detection used by the list endpoint
# ---------------------------------------------------------------------------

class ATSRelationService:
    """
    Ensures that every (ExternalCV, ExternalJobAd) pair for a user has a
    corresponding ATSResult row.  Call ensure_all_relations() before returning
    data to the frontend so the API is always self-healing.
    """

    @staticmethod
    def ensure_all_relations(user) -> int:
        """
        Compute and persist missing ATSResult rows for all CV × Job pairs
        belonging to *user*.

        Returns the number of newly created results.
        """
        cvs = list(ExternalCV.objects.filter(user=user))
        jobs = list(ExternalJobAd.objects.filter(user=user))

        created = 0
        for cv in cvs:
            for job in jobs:
                if _ensure_ats_result(user, cv.text, job.text, cv_id=cv.id, job_id=job.id):
                    created += 1

        if created:
            logger.info(
                "ensure_all_relations: %d nya ATSResult skapade för user=%s",
                created, user.id,
            )
        return created


# ---------------------------------------------------------------------------
# ExternalCVService
# ---------------------------------------------------------------------------

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

            job_ads = ExternalJobAd.objects.filter(user=user)
            matches_created = sum(
                _ensure_ats_result(user, text, job.text, cv_id=cv_obj.id, job_id=job.id)
                for job in job_ads
            )

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

        job_ads = ExternalJobAd.objects.filter(user=user)
        for job in job_ads:
            _ensure_ats_result(user, cv_text, job.text, cv_id=cv.id, job_id=job.id)

        return ExternalCVSerializer(cv).data, None


# ---------------------------------------------------------------------------
# ExternalJobAdService
# ---------------------------------------------------------------------------

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

            cvs = ExternalCV.objects.filter(user=user)
            matches_created = sum(
                _ensure_ats_result(user, cv.text, text, cv_id=cv.id, job_id=job_obj.id)
                for cv in cvs
            )

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
