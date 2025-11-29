using System;
using System.Collections.Generic;
using JBDH.Models.User;
using JBDH.Models.ViewModels;

namespace JBDH.Tests.TestHelpers
{
    public static class TestDataBuilder
    {
        public static RegisterUserViewModel CreateValidRegisterUserViewModel(
            string email = "test@example.com",
            string password = "TestPassword123!")
        {
            return new RegisterUserViewModel
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                Location = "Test City",
                Open = true,
                SelectedRoles = new List<string> { "Developer" }
            };
        }

        public static User CreateTestUser(
            string email = "test@example.com",
            string location = "Test City")
        {
            return new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Location = location,
                Searchable = true,
                DeveloperMode = false
            };
        }

        public static User CreateTestUserWithId(
            int id,
            string email = "test@example.com")
        {
            var user = CreateTestUser(email);
            user.Id = id;
            user.Searchable = true;
            return user;
        }
    }
}