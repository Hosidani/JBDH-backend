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
            public static async Task<List<Dictionary<string, object>>> ReadJobsFromBlobAsync(IConfiguration config)
            {
                var accountName = config["AzureBlobStorage:AccountName"];
                var accountKey = config["AzureBlobStorage:AccountKey"];
                var containerName = config["AzureBlobStorage:ContainerName"];
                var blobFileName = config["AzureBlobStorage:BlobFileName"] ?? "jobs.json";
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

                var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobFileName);

                using var stream = await blobClient.OpenReadAsync();
                var jobs = await System.Text.Json.JsonSerializer.DeserializeAsync<List<Dictionary<string, object>>>(stream);
                return jobs ?? new List<Dictionary<string, object>>();
            }
    
        private readonly string _connectionString;
        private readonly JobSyncService _jobSyncService;
        private readonly IConfiguration _configuration;

        public DatabaseHandlerService(IConfiguration configuration, JobSyncService jobSyncService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
            _jobSyncService = jobSyncService;
            _configuration = configuration;
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

        public async Task DatabaseHandlerAsync(List<Dictionary<string, object>> jobData)
        {
            //JBDH.Services.Configuration.LoggingConfigService.ReturnInfo("[DatabaseHandlerService] DatabaseHandler method called.");
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
            {
                LoggingConfigService.ReturnInfo("Setting up database tables and running ETL pipeline...");
                //EnsureTablesExist(connection);
                await EtlPipeline.RunEtlPipelineAsync(jobData, _connectionString, _configuration);
                LoggingConfigService.ReturnInfo("ETL pipeline completed. Syncing Jobs table with EF...");
                _jobSyncService.SyncJobsFromRaw();
                LoggingConfigService.ReturnInfo("Jobs table sync completed.");
                LoggingConfigService.ReturnInfo("Database handler setup completed.");
            }
        }
    }
}