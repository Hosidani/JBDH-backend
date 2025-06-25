using System;
using System.IO;
using JBDH.Services.Core;
using Xunit;

namespace JBDH.Tests.Services.Core
{
    public class DeleteJsonFileServiceTests
    {
        private const string TestFileName = "jobs.json";
        private readonly string _testFilePath;

        public DeleteJsonFileServiceTests()
        {
            _testFilePath = Path.Combine(Directory.GetCurrentDirectory(), TestFileName);
        }

        [Fact]
        public void DeleteJsonFile_WithExistingFile_ShouldDeleteFile()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithNonExistentFile_ShouldNotThrowException()
        {
            // Arrange
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);

            // Act & Assert - Should not throw exception
            DeleteJsonFileService.DeleteJsonFile();
        }

        [Fact]
        public void DeleteJsonFile_WithEmptyFile_ShouldDeleteFile()
        {
            // Arrange
            CreateTestFile(_testFilePath, "");

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithLargeFile_ShouldDeleteFile()
        {
            // Arrange
            var largeContent = new string('A', 10000);
            CreateTestFile(_testFilePath, largeContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithSpecialCharactersInContent_ShouldDeleteFile()
        {
            // Arrange
            var specialContent = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
            CreateTestFile(_testFilePath, specialContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithUnicodeContent_ShouldDeleteFile()
        {
            // Arrange
            var unicodeContent = "Unicode: 你好世界 🌍 🚀";
            CreateTestFile(_testFilePath, unicodeContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithJsonContent_ShouldDeleteFile()
        {
            // Arrange
            var jsonContent = @"{
                ""jobs"": [
                    {""id"": 1, ""title"": ""Software Engineer""},
                    {""id"": 2, ""title"": ""Senior Developer""}
                ]
            }";
            CreateTestFile(_testFilePath, jsonContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithMultilineContent_ShouldDeleteFile()
        {
            // Arrange
            var multilineContent = "Line 1\nLine 2\nLine 3";
            CreateTestFile(_testFilePath, multilineContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithFileInUse_ShouldHandleGracefully()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            
            // Open file for reading to simulate file in use
            using (var fileStream = File.OpenRead(_testFilePath))
            {
                // Act & Assert - Should not throw exception even if file is in use
                DeleteJsonFileService.DeleteJsonFile();
            }
        }

        [Fact]
        public void DeleteJsonFile_WithReadOnlyFile_ShouldHandleGracefully()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.ReadOnly);

            try
            {
                // Act & Assert - Should not throw exception
                DeleteJsonFileService.DeleteJsonFile();
            }
            finally
            {
                // Cleanup
                File.SetAttributes(_testFilePath, FileAttributes.Normal);
                if (File.Exists(_testFilePath))
                    File.Delete(_testFilePath);
            }
        }

        [Fact]
        public void DeleteJsonFile_WithHiddenFile_ShouldDeleteFile()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.Hidden);

            try
            {
                // Act
                DeleteJsonFileService.DeleteJsonFile();

                // Assert
                Assert.False(File.Exists(_testFilePath));
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testFilePath))
                {
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
            }
        }

        [Fact]
        public void DeleteJsonFile_WithSystemFile_ShouldHandleGracefully()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.System);

            try
            {
                // Act & Assert - Should not throw exception
                DeleteJsonFileService.DeleteJsonFile();
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testFilePath))
                {
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
            }
        }

        [Fact]
        public void DeleteJsonFile_WithArchiveFile_ShouldDeleteFile()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.Archive);

            try
            {
                // Act
                DeleteJsonFileService.DeleteJsonFile();

                // Assert
                Assert.False(File.Exists(_testFilePath));
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testFilePath))
                {
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
            }
        }

        [Fact]
        public void DeleteJsonFile_WithCompressedFile_ShouldHandleGracefully()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.Compressed);

            try
            {
                // Act & Assert - Should not throw exception
                DeleteJsonFileService.DeleteJsonFile();
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testFilePath))
                {
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
            }
        }

        [Fact]
        public void DeleteJsonFile_WithEncryptedFile_ShouldHandleGracefully()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.Encrypted);

            try
            {
                // Act & Assert - Should not throw exception
                DeleteJsonFileService.DeleteJsonFile();
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testFilePath))
                {
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
            }
        }

        [Fact]
        public void DeleteJsonFile_WithMultipleAttributes_ShouldHandleGracefully()
        {
            // Arrange
            CreateTestFile(_testFilePath, "test content");
            File.SetAttributes(_testFilePath, FileAttributes.ReadOnly | FileAttributes.Hidden);

            try
            {
                // Act & Assert - Should not throw exception
                DeleteJsonFileService.DeleteJsonFile();
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testFilePath))
                {
                    File.SetAttributes(_testFilePath, FileAttributes.Normal);
                    File.Delete(_testFilePath);
                }
            }
        }

        [Fact]
        public void DeleteJsonFile_WithFileInDifferentDirectory_ShouldUseCorrectPath()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var tempFilePath = Path.Combine(tempDir, TestFileName);
            CreateTestFile(tempFilePath, "test content");

            try
            {
                // Change current directory temporarily
                var originalDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(tempDir);

                try
                {
                    // Act
                    DeleteJsonFileService.DeleteJsonFile();

                    // Assert
                    Assert.False(File.Exists(tempFilePath));
                }
                finally
                {
                    // Restore original directory
                    Directory.SetCurrentDirectory(originalDirectory);
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void DeleteJsonFile_WithLongFileName_ShouldHandleGracefully()
        {
            // Arrange
            var longFileName = new string('A', 200) + ".json";
            var longFilePath = Path.Combine(Directory.GetCurrentDirectory(), longFileName);
            CreateTestFile(longFilePath, "test content");

            try
            {
                // Act & Assert - Should not throw exception
                // Note: This test verifies the service doesn't break with long file names
                DeleteJsonFileService.DeleteJsonFile();
            }
            finally
            {
                // Cleanup
                if (File.Exists(longFilePath))
                    File.Delete(longFilePath);
            }
        }

        [Fact]
        public void DeleteJsonFile_WithFileContainingNullBytes_ShouldDeleteFile()
        {
            // Arrange
            var contentWithNulls = "Normal content\0Null byte\0More content";
            CreateTestFile(_testFilePath, contentWithNulls);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithFileContainingControlCharacters_ShouldDeleteFile()
        {
            // Arrange
            var contentWithControls = "Normal content\x01\x02\x03Control chars\x04\x05\x06";
            CreateTestFile(_testFilePath, contentWithControls);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithBinaryContent_ShouldDeleteFile()
        {
            // Arrange
            var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD };
            File.WriteAllBytes(_testFilePath, binaryContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithVeryLargeFile_ShouldDeleteFile()
        {
            // Arrange
            var largeContent = new string('A', 1000000); // 1MB of content
            CreateTestFile(_testFilePath, largeContent);

            // Act
            DeleteJsonFileService.DeleteJsonFile();

            // Assert
            Assert.False(File.Exists(_testFilePath));
        }

        [Fact]
        public void DeleteJsonFile_WithFileInRootDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var tempFilePath = Path.Combine(tempDir, TestFileName);
            CreateTestFile(tempFilePath, "test content");

            try
            {
                // Change current directory temporarily
                var originalDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(tempDir);

                try
                {
                    // Act
                    DeleteJsonFileService.DeleteJsonFile();

                    // Assert
                    Assert.False(File.Exists(tempFilePath));
                }
                finally
                {
                    // Restore original directory
                    Directory.SetCurrentDirectory(originalDirectory);
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        private void CreateTestFile(string filePath, string content)
        {
            // Cleanup any existing file
            if (File.Exists(filePath))
                File.Delete(filePath);

            // Create new file with content
            File.WriteAllText(filePath, content);
        }
    }
} 