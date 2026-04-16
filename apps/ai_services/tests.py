from django.test import SimpleTestCase

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
