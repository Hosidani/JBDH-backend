using System;
using System.Collections.Generic;
using JBDH.Models.User;
using JBDH.Models.ViewModels;

namespace JBDH.Tests.TestHelpers
{
    public static class TestDataBuilder
    {
        public static RegisterUserViewModel CreateValidRegisterUserViewModel(
            string username = "testuser",
            string email = "test@example.com",
            string password = "TestPassword123!")
        {
            return new RegisterUserViewModel
            {
                Username = username,
                Email = email,
                Password = password,
                ConfirmPassword = password,
                Location = "Test City",
                OpenForHiring = true,
                SelectedRoles = new List<string> { "Developer" }
            };
        }

        public static User CreateTestUser(
            string username = "testuser",
            string email = "test@example.com",
            string location = "Test City")
        {
            return new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Location = location,
                OpenForHiring = true,
                DeveloperMode = false
            };
        }

        public static User CreateTestUserWithId(
            int id,
            string username = "testuser",
            string email = "test@example.com")
        {
            var user = CreateTestUser(username, email);
            user.Id = id;
            return user;
        }
    }
} 