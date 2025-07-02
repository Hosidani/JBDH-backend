using Microsoft.Extensions.Configuration;

namespace JBDH.Tests
{
    public static class TestConfiguration
    {
        public static IConfiguration GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public static bool SkipStartupOperations => 
            GetTestConfiguration().GetValue<bool>("TestSettings:SkipStartupOperations", true);

        public static bool UseInMemoryDatabase => 
            GetTestConfiguration().GetValue<bool>("TestSettings:UseInMemoryDatabase", true);
    }
} 