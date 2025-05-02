using System;
using System.IO; // For file operations
using System.Text.Json;
using MySql.Data.MySqlClient; // MySQL library for database connections
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Services
{
    class DatabaseHandlerService
        {
            private const string JsonFile = "jobs.json";

            private static void EnsureJsonFileExists(string filePath)
            {
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "{}"); // Create an empty JSON file
                    LoggingConfigService.ReturnInfo($"JSON file created: {filePath}");
                }
                else
                {
                    LoggingConfigService.ReturnInfo($"JSON file already exists: {filePath}");
                }
            }

            private static void EnsureTablesExist(MySqlConnection connection)
            {
                try
                {
                    var createRawTable = SqlQueries.SqlQuery(1);
                    var createCleanTable = SqlQueries.SqlQuery(2);
                    var createUserTable = SqlQueries.SqlQuery(14);
                    var createFriendshipTable = SqlQueries.SqlQuery(16);
                    using (var command = new MySqlCommand(createRawTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createCleanTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createUserTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createFriendshipTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    LoggingConfigService.ReturnInfo("Ensured that 'raw_table', 'clean_table', 'Users' and 'Friendships' tables exists.");
                }
                catch (Exception e)
                {
                    LoggingConfigService.ReturnError($"Error ensuring tables exist: {e.Message}");
                }
            }
            
            public static void databaseHandler(string[] args)
            {
                LoggingConfigService.ConfigLogging();
                // Check if the database already exists
                var databaseExists = false;
                var connectionString = Settings.ConnectionString;
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand("SHOW DATABASES LIKE 'testing_purposes'", connection);
                    var reader = command.ExecuteReader();
                    databaseExists = reader.HasRows;
                }
                if (databaseExists)
                {
                    LoggingConfigService.ReturnInfo("Database already exists. No need to create it again.");
                    LoggingConfigService.ReturnInfo("Starting up Job database handler...");
                    EnsureJsonFileExists(JsonFile);
                    using (var connection = Settings.OpenConnection())
                    {
                        LoggingConfigService.ReturnInfo("Connection established...");
                        EnsureTablesExist(connection);
                        EtlPipeline.RunEtlPipeline(JsonFile);
                        LoggingConfigService.ReturnInfo("Database handler setup completed.");
                    }
                }
                else
                    {
                        // Create the database and tables
                    LoggingConfigService.ReturnInfo("Creating database...");
                    var createDatabase = SqlQueries.SqlQuery(0);
                    using (var connection = Settings.OpenConnection())
                    {
                        using (var command = new MySqlCommand(createDatabase, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    LoggingConfigService.ReturnInfo("Starting up Job database handler...");
                    EnsureJsonFileExists(JsonFile);
                    using (var connection = Settings.OpenConnection())
                    {
                        LoggingConfigService.ReturnInfo("Connection established...");
                        EnsureTablesExist(connection);
                        EtlPipeline.RunEtlPipeline(JsonFile);
                        LoggingConfigService.ReturnInfo("Database handler setup completed.");
                    }
                }
            }
        }
}