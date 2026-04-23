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
        ExternalCV.objects.create(user=self.user, text="my cv")
        ExternalCV.objects.create(user=other, text="other cv")

        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(len(response.data), 1)
        self.assertEqual(response.data[0]['text'], "my cv")

    def test_create_cv(self):
        response = self.client.post(self.url, {'text': 'Python developer'})

        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['text'], 'Python developer')
        self.assertEqual(ExternalCV.objects.filter(user=self.user).count(), 1)

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
        self.cv = ExternalCV.objects.create(user=self.user, text="original text")
        self.url = reverse('external-cv-detail', args=[self.cv.pk])

    def test_retrieve_own_cv(self):
        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['text'], "original text")

    def test_put_updates_cv(self):
        response = self.client.put(self.url, {'text': 'updated text'})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.cv.refresh_from_db()
        self.assertEqual(self.cv.text, 'updated text')

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
        ExternalJobAd.objects.create(user=self.user, text="my job ad")
        ExternalJobAd.objects.create(user=other, text="other job ad")

        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(len(response.data), 1)
        self.assertEqual(response.data[0]['text'], "my job ad")

    def test_create_job_ad(self):
        response = self.client.post(self.url, {'text': 'Senior Django developer'})

        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['text'], 'Senior Django developer')
        self.assertEqual(ExternalJobAd.objects.filter(user=self.user).count(), 1)

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
        self.job_ad = ExternalJobAd.objects.create(user=self.user, text="original job ad")
        self.url = reverse('external-job-ad-detail', args=[self.job_ad.pk])

    def test_retrieve_own_job_ad(self):
        response = self.client.get(self.url)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['text'], "original job ad")

    def test_put_updates_job_ad(self):
        response = self.client.put(self.url, {'text': 'updated job ad'})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.job_ad.refresh_from_db()
        self.assertEqual(self.job_ad.text, 'updated job ad')

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
# Original service tests
# ---------------------------------------------------------------------------

from apps.ai_services.services.user_ai_job_matching_service import UserAIJobMatchingService


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
