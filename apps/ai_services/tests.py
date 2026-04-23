import io
import json
from unittest.mock import patch, MagicMock

from django.core.files.uploadedfile import SimpleUploadedFile
from django.test import SimpleTestCase, TestCase
from django.urls import reverse
from rest_framework.test import APIClient
from rest_framework import status

from apps.users.models import User
from apps.ai_services.models import ExternalCV, ExternalJobAd
from apps.ai_services.services.user_ai_job_matching_service import UserAIJobMatchingService


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def make_user(email="test@example.com", password="pass1234"):
    user = User.objects.create_user(email=email, password=password)
    user.is_active = True
    user.save()
    return user


# ---------------------------------------------------------------------------
# ExternalCV CRUD tests
# ---------------------------------------------------------------------------

class ExternalCVListCreateTests(TestCase):
    def setUp(self):
        self.client = APIClient()
        self.user = make_user()
        self.client.force_authenticate(user=self.user)
        self.url = reverse('external-cv-list-create')

    def test_list_returns_only_own_cvs(self):
        other = make_user("other@example.com")
        ExternalCV.objects.create(user=self.user, name='My CV', text="my cv")
        ExternalCV.objects.create(user=other, name='Other CV', text="other cv")

        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(len(response.data), 1)
        self.assertEqual(response.data[0]['text'], "my cv")
        self.assertEqual(response.data[0]['name'], "My CV")

    def test_create_cv(self):
        response = self.client.post(self.url, {'text': 'Python developer'})

        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['text'], 'Python developer')
        self.assertIn('name', response.data)
        self.assertTrue(response.data['name'])  # auto-generated from text
        self.assertEqual(ExternalCV.objects.filter(user=self.user).count(), 1)

    def test_create_cv_with_custom_name(self):
        response = self.client.post(self.url, {'text': 'Python developer', 'name': 'My Python CV'})

        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['name'], 'My Python CV')

    def test_create_cv_missing_text_returns_400(self):
        response = self.client.post(self.url, {})

        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)

    def test_unauthenticated_returns_401(self):
        self.client.force_authenticate(user=None)
        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_401_UNAUTHORIZED)


class ExternalCVDetailTests(TestCase):
    def setUp(self):
        self.client = APIClient()
        self.user = make_user()
        self.client.force_authenticate(user=self.user)
        self.cv = ExternalCV.objects.create(user=self.user, name='Original CV', text="original text")
        self.url = reverse('external-cv-detail', args=[self.cv.pk])

    def test_retrieve_own_cv(self):
        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['text'], "original text")
        self.assertEqual(response.data['name'], "Original CV")

    def test_put_updates_cv(self):
        response = self.client.put(self.url, {'text': 'updated text', 'name': 'Updated CV'})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.cv.refresh_from_db()
        self.assertEqual(self.cv.text, 'updated text')
        self.assertEqual(self.cv.name, 'Updated CV')

    def test_patch_updates_cv(self):
        response = self.client.patch(self.url, {'text': 'patched text'})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.cv.refresh_from_db()
        self.assertEqual(self.cv.text, 'patched text')

    def test_delete_cv(self):
        response = self.client.delete(self.url)

        self.assertEqual(response.status_code, status.HTTP_204_NO_CONTENT)
        self.assertFalse(ExternalCV.objects.filter(pk=self.cv.pk).exists())

    def test_cannot_access_another_users_cv(self):
        other = make_user("other@example.com")
        other_cv = ExternalCV.objects.create(user=other, text="secret")
        url = reverse('external-cv-detail', args=[other_cv.pk])

        response = self.client.get(url)

        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)

    def test_cannot_delete_another_users_cv(self):
        other = make_user("other@example.com")
        other_cv = ExternalCV.objects.create(user=other, text="secret")
        url = reverse('external-cv-detail', args=[other_cv.pk])

        response = self.client.delete(url)

        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)
        self.assertTrue(ExternalCV.objects.filter(pk=other_cv.pk).exists())


# ---------------------------------------------------------------------------
# ExternalJobAd CRUD tests
# ---------------------------------------------------------------------------

class ExternalJobAdListCreateTests(TestCase):
    def setUp(self):
        self.client = APIClient()
        self.user = make_user()
        self.client.force_authenticate(user=self.user)
        self.url = reverse('external-job-ad-list-create')

    def test_list_returns_only_own_job_ads(self):
        other = make_user("other@example.com")
        ExternalJobAd.objects.create(user=self.user, name='My Job', text="my job ad")
        ExternalJobAd.objects.create(user=other, name='Other Job', text="other job ad")

        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(len(response.data), 1)
        self.assertEqual(response.data[0]['text'], "my job ad")
        self.assertEqual(response.data[0]['name'], "My Job")

    def test_create_job_ad(self):
        response = self.client.post(self.url, {'text': 'Senior Django developer'})

        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['text'], 'Senior Django developer')
        self.assertIn('name', response.data)
        self.assertTrue(response.data['name'])  # auto-generated from text
        self.assertEqual(ExternalJobAd.objects.filter(user=self.user).count(), 1)

    def test_create_job_ad_with_custom_name(self):
        response = self.client.post(self.url, {'text': 'Senior Django developer', 'name': 'Django Job'})

        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['name'], 'Django Job')

    def test_create_job_ad_missing_text_returns_400(self):
        response = self.client.post(self.url, {})

        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)

    def test_unauthenticated_returns_401(self):
        self.client.force_authenticate(user=None)
        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_401_UNAUTHORIZED)


class ExternalJobAdDetailTests(TestCase):
    def setUp(self):
        self.client = APIClient()
        self.user = make_user()
        self.client.force_authenticate(user=self.user)
        self.job_ad = ExternalJobAd.objects.create(user=self.user, name='Original Job', text="original job ad")
        self.url = reverse('external-job-ad-detail', args=[self.job_ad.pk])

    def test_retrieve_own_job_ad(self):
        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['text'], "original job ad")
        self.assertEqual(response.data['name'], "Original Job")

    def test_put_updates_job_ad(self):
        response = self.client.put(self.url, {'text': 'updated job ad', 'name': 'Updated Job'})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.job_ad.refresh_from_db()
        self.assertEqual(self.job_ad.text, 'updated job ad')
        self.assertEqual(self.job_ad.name, 'Updated Job')

    def test_patch_updates_job_ad(self):
        response = self.client.patch(self.url, {'text': 'patched job ad'})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.job_ad.refresh_from_db()
        self.assertEqual(self.job_ad.text, 'patched job ad')

    def test_delete_job_ad(self):
        response = self.client.delete(self.url)

        self.assertEqual(response.status_code, status.HTTP_204_NO_CONTENT)
        self.assertFalse(ExternalJobAd.objects.filter(pk=self.job_ad.pk).exists())

    def test_cannot_access_another_users_job_ad(self):
        other = make_user("other@example.com")
        other_job_ad = ExternalJobAd.objects.create(user=other, text="secret")
        url = reverse('external-job-ad-detail', args=[other_job_ad.pk])

        response = self.client.get(url)

        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)

    def test_cannot_delete_another_users_job_ad(self):
        other = make_user("other@example.com")
        other_job_ad = ExternalJobAd.objects.create(user=other, text="secret")
        url = reverse('external-job-ad-detail', args=[other_job_ad.pk])

        response = self.client.delete(url)

        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)
        self.assertTrue(ExternalJobAd.objects.filter(pk=other_job_ad.pk).exists())


# ---------------------------------------------------------------------------
# ExternalCV – PDF upload endpoint
# ---------------------------------------------------------------------------

class ExternalCVUploadPDFTests(TestCase):
    def setUp(self):
        self.client = APIClient()
        self.user = make_user()
        self.client.force_authenticate(user=self.user)
        self.url = reverse('external-cv-upload-pdf')

    def _make_pdf(self, name='cv.pdf', content=b'%PDF-1.4 fake pdf content'):
        return SimpleUploadedFile(name, content, content_type='application/pdf')

    @patch('apps.ai_services.services.external_cv_job_ad_service.extract_text_from_file', return_value='Extracted CV text from PDF')
    def test_upload_pdf_creates_external_cv(self, _mock_extract):
        response = self.client.post(
            self.url,
            {'file': self._make_pdf()},
            format='multipart',
        )
        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertIn('Extracted CV text from PDF', response.data['text'])
        self.assertEqual(ExternalCV.objects.filter(user=self.user).count(), 1)

    @patch('apps.ai_services.services.external_cv_job_ad_service.extract_text_from_file', return_value='Extracted CV text from PDF')
    def test_upload_pdf_includes_filename_label_in_text(self, _mock_extract):
        response = self.client.post(self.url, {'file': self._make_pdf(name='mitt-cv.pdf')}, format='multipart')
        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertIn('mitt-cv', response.data['name'])

    def test_upload_without_file_returns_400(self):
        response = self.client.post(self.url, {}, format='multipart')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertIn('error', response.data)

    @patch('apps.ai_services.services.external_cv_job_ad_service.extract_text_from_file', return_value='')
    def test_upload_empty_pdf_returns_400(self, _mock_extract):
        """Filen laddas upp men textextraktionen ger tom sträng."""
        response = self.client.post(self.url, {'file': self._make_pdf()}, format='multipart')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertIn('error', response.data)

    def test_unauthenticated_upload_returns_401(self):
        self.client.force_authenticate(user=None)
        response = self.client.post(self.url, {'file': self._make_pdf()}, format='multipart')
        self.assertEqual(response.status_code, status.HTTP_401_UNAUTHORIZED)

    @patch('apps.ai_services.services.external_cv_job_ad_service.extract_text_from_file', return_value='Extracted CV text from PDF')
    def test_upload_stores_cv_for_correct_user(self, _mock_extract):
        other = make_user('other@example.com')
        other_client = APIClient()
        other_client.force_authenticate(user=other)
        other_client.post(self.url, {'file': self._make_pdf()}, format='multipart')

        self.client.post(self.url, {'file': self._make_pdf()}, format='multipart')

        self.assertEqual(ExternalCV.objects.filter(user=self.user).count(), 1)
        self.assertEqual(ExternalCV.objects.filter(user=other).count(), 1)


# ---------------------------------------------------------------------------
# CVJobSSEService – generate_events med external_cv_id
# ---------------------------------------------------------------------------

class CVJobSSEExternalCVTests(TestCase):
    def setUp(self):
        self.user = make_user()
        self.cv = ExternalCV.objects.create(user=self.user, name='Sparad CV', text='Sparad CV-text från databasen')

    def _make_request(self, post_data, files=None):
        """Bygger ett minimalt fake request."""
        req = MagicMock()
        req.user = self.user
        req.POST = post_data
        req.FILES = files or {}
        req.data = {}
        return req

    @patch('apps.ai_services.services.cv_job_sse_service.vectorize_text', return_value=[0.1, 0.2, 0.3])
    @patch('apps.ai_services.services.cv_job_sse_service.extract_cv_job_analysis', return_value={'result': 'ok'})
    @patch('apps.ai_services.services.cv_job_sse_service.cosine_similarity', return_value=0.85)
    def test_generate_events_with_external_cv_id(self, _cos, _analysis, _vec):
        from apps.ai_services.services.cv_job_sse_service import CVJobSSEService
        req = self._make_request({
            'external_cv_id': str(self.cv.pk),
            'JobAdText': 'Vi söker en Python-utvecklare.',
        })
        events = list(CVJobSSEService().generate_events(req))
        payloads = [json.loads(e.replace('data: ', '').strip()) for e in events if e.strip()]
        # Ska inte innehålla error
        errors = [p for p in payloads if 'error' in p]
        self.assertEqual(errors, [], msg=f"Fick ej förväntade SSE-fel: {errors}")

    def test_generate_events_with_invalid_cv_id_returns_error(self):
        from apps.ai_services.services.cv_job_sse_service import CVJobSSEService
        req = self._make_request({
            'external_cv_id': '99999',
            'JobAdText': 'Vi söker en Python-utvecklare.',
        })
        events = list(CVJobSSEService().generate_events(req))
        payloads = [json.loads(e.replace('data: ', '').strip()) for e in events if e.strip()]
        errors = [p for p in payloads if 'error' in p]
        self.assertTrue(len(errors) > 0)
        self.assertIn('hittades inte', errors[-1]['error'])

    def test_generate_events_without_cv_returns_error(self):
        from apps.ai_services.services.cv_job_sse_service import CVJobSSEService
        req = self._make_request({'JobAdText': 'Vi söker en Python-utvecklare.'})
        events = list(CVJobSSEService().generate_events(req))
        payloads = [json.loads(e.replace('data: ', '').strip()) for e in events if e.strip()]
        errors = [p for p in payloads if 'error' in p]
        self.assertTrue(len(errors) > 0)

    def test_generate_events_without_job_ad_returns_error(self):
        from apps.ai_services.services.cv_job_sse_service import CVJobSSEService
        req = self._make_request({'external_cv_id': str(self.cv.pk)})
        events = list(CVJobSSEService().generate_events(req))
        payloads = [json.loads(e.replace('data: ', '').strip()) for e in events if e.strip()]
        errors = [p for p in payloads if 'error' in p]
        self.assertTrue(len(errors) > 0)

    def test_generate_events_cannot_use_another_users_cv(self):
        from apps.ai_services.services.cv_job_sse_service import CVJobSSEService
        other = make_user('other@example.com')
        other_cv = ExternalCV.objects.create(user=other, text='Hemlig CV')
        req = self._make_request({
            'external_cv_id': str(other_cv.pk),
            'JobAdText': 'Jobbtext',
        })
        req.user = self.user  # försöker hämta annan användares CV
        events = list(CVJobSSEService().generate_events(req))
        payloads = [json.loads(e.replace('data: ', '').strip()) for e in events if e.strip()]
        errors = [p for p in payloads if 'error' in p]
        self.assertTrue(len(errors) > 0)


# ---------------------------------------------------------------------------
# Original service tests
# ---------------------------------------------------------------------------


class UserAIJobMatchingServiceTests(SimpleTestCase):
	def test_returns_error_when_no_saved_user_ai_response_exists(self):
		class FakeUserAIResponseService:
			@staticmethod
			def get_latest_user_ai_response(_user):
				return None

		service = UserAIJobMatchingService(
			ai_call=lambda *_args, **_kwargs: {},
			vectorize_fn=lambda _text: [0.1, 0.2],
			user_ai_response_service=FakeUserAIResponseService(),
		)

		fake_user = type("FakeUser", (), {"id": 1})()
		result = service.compare_latest_user_ai_response_to_jobs(user=fake_user)

		self.assertFalse(result["success"])
		self.assertEqual(result["error"], "No saved AI CV analysis found for this user.")

	def test_converts_ai_analysis_into_matching_tab_shape(self):
		class FakeResponse:
			id = 42
			response_text = '{"English": {"HardSkills": ["Python"]}}'

		class FakeUserAIResponseService:
			@staticmethod
			def get_latest_user_ai_response(_user):
				return FakeResponse()

		def fake_ai_call(_prompt, language="English"):
			self.assertEqual(language, "English")
			return {
				"choices": [
					{
						"message": {
							"content": (
								'{"FitSummary":"Bra match",'
								'"MatchingPoints":["Python erfarenhet"],'
								'"NonMatchingPoints":["Saknar AWS"],'
								'"ImprovementOpportunities":["Lär Kubernetes"],'
								'"CoachTips":"Fokusera på cloud"}'
							)
						}
					}
				]
			}

		service = UserAIJobMatchingService(
			ai_call=fake_ai_call,
			vectorize_fn=lambda _text: [0.5, 0.6],
			user_ai_response_service=FakeUserAIResponseService(),
		)

		service._find_top_jobs = lambda query_vector, top_k, min_score: [
			{
				"jobId": 10,
				"externalJobId": "abc",
				"title": "Backend Dev",
				"company": "Acme",
				"location": "Stockholm",
				"score": 0.91,
				"description": "Python och AWS",
			}
		]

		fake_user = type("FakeUser", (), {"id": 2})()
		result = service.compare_latest_user_ai_response_to_jobs(user=fake_user)

		self.assertTrue(result["success"])
		self.assertEqual(result["userAiResponseId"], 42)
		self.assertEqual(result["matchingAnalysis"]["matches"][0]["label"], "Python erfarenhet")
		self.assertEqual(result["matchingAnalysis"]["nonMatches"][0]["label"], "Saknar AWS")
		self.assertEqual(result["matchingAnalysis"]["improvements"][0]["label"], "Lär Kubernetes")
		self.assertEqual(result["matchingAnalysis"]["coachTips"], "Fokusera på cloud")
