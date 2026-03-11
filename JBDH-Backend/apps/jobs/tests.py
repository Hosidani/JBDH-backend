from rest_framework import status
from rest_framework.test import APITestCase

from apps.jobs.models import Job, NotInterestedJob, SavedJob
from apps.users.models import User


class JobsEndpointsContractTests(APITestCase):
	def setUp(self):
		self.user = User.objects.create_user(
			email="jobs-tester@example.com",
			password="TestPass123!",
			is_active=True,
		)
		self.client.force_authenticate(user=self.user)

		self.job = Job.objects.create(
			job_id="ext-123",
			title="Backend Developer",
			description="Python, Django and APIs",
			company="Acme",
			location="Stockholm",
			required_skills="Python,Django,REST",
		)

	def test_get_job_details_contract(self):
		response = self.client.get(f"/api/Jobs/{self.job.id}")

		self.assertEqual(response.status_code, status.HTTP_200_OK)
		self.assertEqual(response.data["jobId"], self.job.id)
		self.assertIn("requiredSkills", response.data)
		self.assertIsInstance(response.data["requiredSkills"], list)
		self.assertIn("isSaved", response.data)
		self.assertIn("hasApplied", response.data)
		self.assertIn("isNotInterested", response.data)

	def test_get_is_saved_endpoint(self):
		response_before = self.client.get(f"/api/Jobs/{self.job.id}/issaved")
		self.assertEqual(response_before.status_code, status.HTTP_200_OK)
		self.assertFalse(response_before.data["isSaved"])

		SavedJob.objects.create(user=self.user, job=self.job)
		response_after = self.client.get(f"/api/Jobs/{self.job.id}/issaved")
		self.assertEqual(response_after.status_code, status.HTTP_200_OK)
		self.assertTrue(response_after.data["isSaved"])

	def test_post_save_endpoint_and_conflict(self):
		response = self.client.post(f"/api/Jobs/{self.job.id}/save", data={}, format="json")
		self.assertEqual(response.status_code, status.HTTP_201_CREATED)

		conflict = self.client.post(f"/api/Jobs/{self.job.id}/save", data={}, format="json")
		self.assertEqual(conflict.status_code, status.HTTP_409_CONFLICT)

	def test_put_save_updates_notes(self):
		SavedJob.objects.create(user=self.user, job=self.job)

		response = self.client.put(
			f"/api/Jobs/{self.job.id}/save",
			data={"notes": "follow up next week"},
			format="json",
		)

		self.assertEqual(response.status_code, status.HTTP_200_OK)
		self.assertEqual(response.data["notes"], "follow up next week")

	def test_delete_unsave_endpoint(self):
		SavedJob.objects.create(user=self.user, job=self.job)

		response = self.client.delete(f"/api/Jobs/{self.job.id}/unsave")
		self.assertEqual(response.status_code, status.HTTP_204_NO_CONTENT)
		self.assertFalse(SavedJob.objects.filter(user=self.user, job=self.job).exists())

	def test_get_saved_jobs_endpoint(self):
		SavedJob.objects.create(user=self.user, job=self.job, notes="priority")

		response = self.client.get("/api/jobs/saved")

		self.assertEqual(response.status_code, status.HTTP_200_OK)
		self.assertEqual(len(response.data), 1)
		self.assertEqual(response.data[0]["jobId"], self.job.id)
		self.assertTrue(response.data[0]["isSaved"])

	def test_post_not_interested_endpoint(self):
		response = self.client.post(f"/api/Jobs/{self.job.id}/not-interested", data={}, format="json")
		self.assertEqual(response.status_code, status.HTTP_200_OK)
		self.assertEqual(response.data["success"], True)

	def test_search_for_jobs_filters_by_description_and_location(self):
		other_job = Job.objects.create(
			job_id="ext-456",
			title="Frontend Developer",
			description="React and TypeScript",
			company="Beta",
			location="Gothenburg",
		)

		response = self.client.get(
			"/api/Search/SearchForJobs",
			{"description": "Backend", "location": "Stockholm"},
		)

		self.assertEqual(response.status_code, status.HTTP_200_OK)
		self.assertEqual(len(response.json()), 1)
		self.assertEqual(response.json()[0]["jobId"], self.job.id)
		self.assertNotEqual(response.json()[0]["jobId"], other_job.id)

	def test_search_for_jobs_excludes_not_interested_for_authenticated_user(self):
		NotInterestedJob.objects.create(user=self.user, job=self.job)

		response = self.client.get("/api/Search/SearchForJobs")

		self.assertEqual(response.status_code, status.HTTP_200_OK)
		returned_ids = [item["jobId"] for item in response.json()]
		self.assertNotIn(self.job.id, returned_ids)
