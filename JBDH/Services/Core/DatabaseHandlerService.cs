using System;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;
using JBDH.Models;
using JBDH.Services.Configuration;

namespace JBDH.Services.Core
{
    class DatabaseHandlerService
    {
        private const string JsonFile = "jobs.json";

        private static void EnsureJsonFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{}");
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
                // Use the new organized query numbers
                var createRawTable                 = SqlQueries.SqlQuery(1);
                var createJobsTable                = SqlQueries.SqlQuery(2);
                var createAIResponsesTable         = SqlQueries.SqlQuery(3);
                var createUsersTable               = SqlQueries.SqlQuery(4);
                var createFriendshipsTable         = SqlQueries.SqlQuery(5);
                var createEmailVerificationTable   = SqlQueries.SqlQuery(6);
                var createOrganisationsTable       = SqlQueries.SqlQuery(7);
                var createJobApplicationsTable     = SqlQueries.SqlQuery(8);
                var createDataFlowsTable           = SqlQueries.SqlQuery(9);
                var createJobAIResponsesTable      = SqlQueries.SqlQuery(10);
                var createUserRoleAssignmentsTable = SqlQueries.SqlQuery(11);
                var createUserReportsTable         = SqlQueries.SqlQuery(12);
                var createPasswordResetsTable      = SqlQueries.SqlQuery(13);
                var createUserAIResponsesTable     = SqlQueries.SqlQuery(14); // <-- Added

                // Execute each create table command
                using (var command = new MySqlCommand(createUsersTable, connection))
                {
                    command.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo("Users table created.");
                }
                using (var command = new MySqlCommand(createRawTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createJobsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createAIResponsesTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createFriendshipsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createEmailVerificationTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createOrganisationsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createJobApplicationsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createDataFlowsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createJobAIResponsesTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createUserRoleAssignmentsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createUserReportsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createPasswordResetsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createUserAIResponsesTable, connection)) { command.ExecuteNonQuery(); } // <-- Added

                LoggingConfigService.ReturnInfo("Ensured all tables exist.");
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