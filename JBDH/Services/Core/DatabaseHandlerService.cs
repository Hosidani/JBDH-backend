using System;
using System.IO;
using System.Text.Json;
using JBDH.Models.Core;
using JBDH.Services.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
// ...existing code...

namespace JBDH.Services.Core
{
    public class DatabaseHandlerService
    {
        private readonly string _connectionString;

        public DatabaseHandlerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private const string JsonFile = "jobs.json";

        private void EnsureJsonFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{}");
                LoggingConfigService.ReturnInfo($"JSON file created: {filePath}");
            }
            // No need to log if the file already exists
        }

        private void EnsureTablesExist(MySqlConnection connection)
        {
            try
            {
                LoggingConfigService.ReturnInfo("Ensuring all required tables exist...");
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
                var createUserAIResponsesTable     = SqlQueries.SqlQuery(14);
                var createBoardsTable              = SqlQueries.SqlQuery(15);
                var createListsTable               = SqlQueries.SqlQuery(16);
                var createCardsTable               = SqlQueries.SqlQuery(17);
                var createBoardMembershipsTable    = SqlQueries.SqlQuery(18);
                var createMessagesTable            = SqlQueries.SqlQuery(19);
                var createAIUserCourseProposalsTable = SqlQueries.SqlQuery(20); // Add this line

                // Execute each create table command (no per-table logging)
                using (var command = new MySqlCommand(createUsersTable, connection)) { command.ExecuteNonQuery(); }
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
                using (var command = new MySqlCommand(createUserAIResponsesTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createBoardsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createListsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createCardsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createBoardMembershipsTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createMessagesTable, connection)) { command.ExecuteNonQuery(); }
                using (var command = new MySqlCommand(createAIUserCourseProposalsTable, connection)) { command.ExecuteNonQuery(); } // <-- Add this line

                LoggingConfigService.ReturnInfo("All required tables ensured.");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error ensuring tables exist: {e.Message}");
            }
        }

        public void DatabaseHandler(List<Dictionary<string, object>> jobData)
        {
            var databaseExists = false;
            using (var connection = new MySqlConnection(_connectionString))
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
            //using (var connection = Settings.OpenConnection())
            {
                LoggingConfigService.ReturnInfo("Setting up database tables and running ETL pipeline...");
                //EnsureTablesExist(connection);
                EtlPipeline.RunEtlPipeline(jobData, _connectionString);
                LoggingConfigService.ReturnInfo("Database handler setup completed.");
            }
        }
    }
}