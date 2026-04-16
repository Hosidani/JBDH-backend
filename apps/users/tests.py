from unittest.mock import patch

from django.test import SimpleTestCase, TestCase
from rest_framework import status
from rest_framework.test import APIRequestFactory, force_authenticate
from django.urls import reverse
from django.contrib.auth import get_user_model

from apps.users.views import (
    vector_search_jobs, ai_match_jobs, 
    DeleteMyUserView, UpdateUserSettingsView,
    ChangePasswordView, GetAccountSettingsView
)

User = get_user_model()

if __name__ == '__main__':
    import unittest
    unittest.main(verbosity=2)

    
class VectorSearchJobsEndpointTests(SimpleTestCase):
    def setUp(self):
        self.factory = APIRequestFactory()
        self.url = "/profile/vectorsearchjobs/"
        self.user = type("AuthUser", (), {"is_authenticated": True})()

    @patch("apps.users.views.UserAIJobMatchingService.create_vector_search_session")
    def test_vector_search_jobs_returns_session_and_top_jobs(self, mock_create_session):
        mock_create_session.return_value = {
            "success": True,
            "sessionId": "123e4567-e89b-12d3-a456-426614174000",
            "topJobs": [{"jobId": 1, "score": 0.88, "description": "desc"}],
        }

        request = self.factory.post(self.url, {"topK": 5, "minScore": 0.2, "language": "English"}, format="json")
        force_authenticate(request, user=self.user)
        response = vector_search_jobs(request)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data["sessionId"], "123e4567-e89b-12d3-a456-426614174000")
        self.assertEqual(len(response.data["topJobs"]), 1)


class AIMatchJobsEndpointTests(SimpleTestCase):
    def setUp(self):
        self.factory = APIRequestFactory()
        self.url = "/profile/aimatchjobs/"
        self.user = type("AuthUser", (), {"is_authenticated": True})()

    def test_ai_match_jobs_requires_session_id(self):
        request = self.factory.post(self.url, {}, format="json")
        force_authenticate(request, user=self.user)
        response = ai_match_jobs(request)

        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertEqual(response.data["error"], "sessionId is required.")

    @patch("apps.users.views.UserAIJobMatchingService.ai_match_from_session")
    def test_ai_match_jobs_returns_analysis(self, mock_ai_match):
        mock_ai_match.return_value = {
            "success": True,
            "sessionId": "123e4567-e89b-12d3-a456-426614174000",
            "jobAnalyses": [{"jobId": 1}],
            "matchingAnalysis": {"matches": [], "nonMatches": [], "improvements": [], "coachTips": ""},
        }

        request = self.factory.post(self.url, {"sessionId": "123e4567-e89b-12d3-a456-426614174000"}, format="json")
        force_authenticate(request, user=self.user)
        response = ai_match_jobs(request)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data["sessionId"], "123e4567-e89b-12d3-a456-426614174000")

    @patch("apps.users.views.UserAIJobMatchingService.ai_match_from_session")
    def test_ai_match_jobs_passes_job_id_when_provided(self, mock_ai_match):
        mock_ai_match.return_value = {
            "success": True,
            "sessionId": "123e4567-e89b-12d3-a456-426614174000",
            "jobAnalyses": [{"jobId": 99}],
            "matchingAnalysis": {"matches": [], "nonMatches": [], "improvements": [], "coachTips": ""},
        }

        request = self.factory.post(
            self.url,
            {"sessionId": "123e4567-e89b-12d3-a456-426614174000", "jobId": 99},
            format="json",
        )
        force_authenticate(request, user=self.user)
        response = ai_match_jobs(request)

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        mock_ai_match.assert_called_once_with(
            user=self.user,
            session_id="123e4567-e89b-12d3-a456-426614174000",
            language="English",
            job_id=99,
        )
    

class AccountServiceTests(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(username='test', password='pass')
        self.factory = APIRequestFactory()
    
    @patch('services.account_service.AccountService.delete_user_account')
    def test_delete_my_user_view(self, mock_service):
        mock_service.return_value = {"message": "deleted"}
        
        request = self.factory.delete('/account/delete/')
        force_authenticate(request, user=self.user)
        response = DeleteMyUserView.as_view()(request)
        
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        mock_service.assert_called_once_with(self.user)
    
    @patch('services.account_service.AccountService.get_account_settings')
    def test_get_account_settings_view(self, mock_service):
        mock_account = type('Account', (), {'name': 'Test Account'})()
        mock_service.return_value = mock_account
        
        request = self.factory.get('/account/settings/')
        force_authenticate(request, user=self.user)
        response = GetAccountSettingsView.as_view()(request)
        
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        mock_service.assert_called_once_with(self.user)

class ProfileServiceTests(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(username='test2', password='pass')
        self.factory = APIRequestFactory()

    @patch('apps.users.services.profile_service.ProfileService.update_user_settings')
    def test_update_user_settings_view(self, mock_service):
        mock_service.return_value = {"message": "updated"}
        
        request = self.factory.put('/profile/settings/', {"name": "Test"})
        force_authenticate(request, user=self.user)
        response = UpdateUserSettingsView.as_view()(request)
        
        self.assertEqual(response.status_code, status.HTTP_200_OK)

class ChangePasswordTests(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(username='test3', password='oldpass')
        self.factory = APIRequestFactory()
    
    @patch('apps.users.services.ai_match_jobs_service.auth_service.AuthService.change_password')
    def test_change_password_view(self, mock_service):
        mock_service.return_value = {"success": True}
        
        request = self.factory.post('/change-password/', {
            'currentPassword': 'oldpass', 'newPassword': 'new123', 'confirmNewPassword': 'new123'
        })
        force_authenticate(request, user=self.user)
        response = ChangePasswordView.as_view()(request)
        
        self.assertEqual(response.status_code, status.HTTP_200_OK)
