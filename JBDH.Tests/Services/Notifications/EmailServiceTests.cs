using System;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Services.Notifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace JBDH.Tests.Services.Notifications
{
    public class EmailServiceTests
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _smtpSettings = new SmtpSettings
            {
                Host = "smtp.test.com",
                Port = 587,
                EnableSSL = true,
                Username = "test@test.com",
                Password = "testpassword"
            };

            var options = Options.Create(_smtpSettings);
            _emailService = new EmailService(options);
        }

        [Fact]
        public void EmailService_Constructor_WithValidSettings_ShouldCreateInstance()
        {
            // Arrange
            var settings = new SmtpSettings
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSSL = true,
                Username = "user@gmail.com",
                Password = "password123"
            };
            var options = Options.Create(settings);

            // Act
            var emailService = new EmailService(options);

            // Assert
            Assert.NotNull(emailService);
        }

        [Fact]
        public void EmailService_Constructor_WithNullOptions_ShouldThrowNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => new EmailService(null!));
        }

        [Fact]
        public void EmailService_Constructor_WithNullSmtpSettings_ShouldCreateInstance()
        {
            // Arrange
            var options = Options.Create<SmtpSettings>(null!);

            // Act
            var emailService = new EmailService(options);

            // Assert
            Assert.NotNull(emailService);
        }

        [Fact]
        public void SmtpSettings_WithValidConfiguration_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("smtp.test.com", _smtpSettings.Host);
            Assert.Equal(587, _smtpSettings.Port);
            Assert.True(_smtpSettings.EnableSSL);
            Assert.Equal("test@test.com", _smtpSettings.Username);
            Assert.Equal("testpassword", _smtpSettings.Password);
        }

        [Fact]
        public void SmtpSettings_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var defaultSettings = new SmtpSettings();

            // Assert
            Assert.Equal("smtp.gmail.com", defaultSettings.Host);
            Assert.Equal(587, defaultSettings.Port);
            Assert.True(defaultSettings.EnableSSL);
            Assert.Equal(string.Empty, defaultSettings.Username);
            Assert.Equal(string.Empty, defaultSettings.Password);
        }

        [Fact]
        public void EmailService_WithDifferentSmtpConfigurations_ShouldCreateInstance()
        {
            // Arrange
            var settings1 = new SmtpSettings
            {
                Host = "smtp.outlook.com",
                Port = 25,
                EnableSSL = false,
                Username = "user@outlook.com",
                Password = "password"
            };

            var settings2 = new SmtpSettings
            {
                Host = "smtp.yahoo.com",
                Port = 465,
                EnableSSL = true,
                Username = "user@yahoo.com",
                Password = "password"
            };

            var options1 = Options.Create(settings1);
            var options2 = Options.Create(settings2);

            // Act
            var emailService1 = new EmailService(options1);
            var emailService2 = new EmailService(options2);

            // Assert
            Assert.NotNull(emailService1);
            Assert.NotNull(emailService2);
        }

        [Fact]
        public void EmailService_WithInvalidHost_ShouldCreateInstance()
        {
            // Arrange
            var settings = new SmtpSettings
            {
                Host = "invalid-host",
                Port = 999,
                EnableSSL = false,
                Username = "user@test.com",
                Password = "password"
            };
            var options = Options.Create(settings);

            // Act
            var emailService = new EmailService(options);

            // Assert
            Assert.NotNull(emailService);
        }

        [Fact]
        public void EmailService_WithEmptyCredentials_ShouldCreateInstance()
        {
            // Arrange
            var settings = new SmtpSettings
            {
                Host = "smtp.test.com",
                Port = 587,
                EnableSSL = true,
                Username = "",
                Password = ""
            };
            var options = Options.Create(settings);

            // Act
            var emailService = new EmailService(options);

            // Assert
            Assert.NotNull(emailService);
        }

        [Fact]
        public void SmtpSettings_WithCustomPort_ShouldBeSetCorrectly()
        {
            // Arrange
            var settings = new SmtpSettings
            {
                Host = "smtp.custom.com",
                Port = 2525,
                EnableSSL = false,
                Username = "user@custom.com",
                Password = "password"
            };

            // Assert
            Assert.Equal(2525, settings.Port);
            Assert.False(settings.EnableSSL);
        }

        [Fact]
        public void SmtpSettings_WithSSLDisabled_ShouldBeSetCorrectly()
        {
            // Arrange
            var settings = new SmtpSettings
            {
                Host = "smtp.nonssl.com",
                Port = 25,
                EnableSSL = false,
                Username = "user@nonssl.com",
                Password = "password"
            };

            // Assert
            Assert.False(settings.EnableSSL);
            Assert.Equal(25, settings.Port);
        }
    }
} 