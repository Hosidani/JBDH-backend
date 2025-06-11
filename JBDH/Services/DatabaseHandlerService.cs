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
                    var createAiResponseTable = SqlQueries.SqlQuery(7);
                    var createUserTable = SqlQueries.SqlQuery(14);
                    var createEmailVerificationTable = SqlQueries.SqlQuery(17);
                    var createFriendshipTable = SqlQueries.SqlQuery(16);
                    var createOrganisationTable = SqlQueries.SqlQuery(18);
                    var createJobApplicationTable = SqlQueries.SqlQuery(19);
                    var createDataFlowTable = SqlQueries.SqlQuery(20);
                    var createJobAIResponseTable = SqlQueries.SqlQuery(22);
                    var createUserRoleAssignmentsTable = SqlQueries.SqlQuery(23);
                    var createUserReportsTable = SqlQueries.SqlQuery(24);
                    var createPasswordResetTable = SqlQueries.SqlQuery(25);
                    
                    using (var command = new MySqlCommand(createUserTable, connection))
                {
                    command.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo("Users table created.");
                }
                    using (var command = new MySqlCommand(createRawTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createCleanTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createEmailVerificationTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createFriendshipTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createOrganisationTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createJobApplicationTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createDataFlowTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createAiResponseTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createJobAIResponseTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createUserRoleAssignmentsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createUserReportsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new MySqlCommand(createPasswordResetTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    LoggingConfigService.ReturnInfo("Ensured that 'raw_table', 'Jobs', 'Users' , 'EmailVerification' , 'Friendships' , 'JobApplication' , 'DataFlow', 'ai_response', 'job_ai_responses' , 'UserReports' and 'PasswordReset' tables exists.");
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
                    if (databaseExists)
                    {
                        LoggingConfigService.ReturnInfo("Database 'testing_purposes' already exists.");
                    }
                    else
                    {
                        var createDatabase = SqlQueries.SqlQuery(0);
                        using (var dbCommand = new MySqlCommand(createDatabase, connection))
                        {
                            dbCommand.ExecuteNonQuery();
                        }
                        LoggingConfigService.ReturnInfo("Database 'testing_purposes' created.");
                    }
                }
                // Always open a new connection to the correct database before creating tables
                using (var connection = Settings.OpenConnection()) // This should connect to 'testing_purposes'
                {
                    LoggingConfigService.ReturnInfo("Connection established...");
                    EnsureTablesExist(connection);
                    EtlPipeline.RunEtlPipeline(JsonFile);
                    LoggingConfigService.ReturnInfo("Database handler setup completed.");
                }
            }
        }
}