using System;
using System.IO; // For file operations
using System.Text.Json;
using MySql.Data.MySqlClient; // MySQL library for database connections
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Controllers // Assuming this is the namespace for your controller
{
    class DbHandler
    {
        public static readonly string ConnectionString = DbConfig.DB_CONFIG; // Ensure this is a valid MySQL connection string
        private const string JsonFile = "jobs.json";

        public static void Main(string[] args)
        {
            LoggingConfig.ConfigLogging();
            LoggingConfig.ReturnInfo("Starting up Job database handler...");
            LoggingConfig.ReturnInfo("Setting up connection...");
            EnsureJsonFileExists(JsonFile);
            using (var connection = Settings.OpenConnection())
            {
                LoggingConfig.ReturnInfo("Connection established...");
                EnsureTablesExist(connection);
                EtlPipeline.RunEtlPipeline(JsonFile);
                LoggingConfig.ReturnInfo("ETL pipeline completed...");
                LoggingConfig.ReturnInfo("Starting sorting queries...");
            }
            LoggingConfig.ReturnInfo("Closing application...");
        }
        private static MySqlConnection OpenConnection()
        {
            try
            {
                var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                LoggingConfig.ReturnError($"Failed to establish a connection: {ex.Message}");
                throw;
            }
        }

        private static void EnsureJsonFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{}"); // Create an empty JSON file
                LoggingConfig.ReturnInfo($"JSON file created: {filePath}");
            }
            else
            {
                LoggingConfig.ReturnInfo($"JSON file already exists: {filePath}");
            }
        }

        private static void DeleteJsonFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                LoggingConfig.ReturnInfo($"Deleted JSON file: {filePath}");
            }
            else
            {
                LoggingConfig.ReturnInfo($"JSON file not found: {filePath}");
            }
        }

        private static void EnsureTablesExist(MySqlConnection connection)
        {
            try
            {
                var createRawTable = SqlQueries.SqlQuery(1);
                var createCleanTable = SqlQueries.SqlQuery(2);
                var createUserTable = SqlQueries.SqlQuery(14);
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
                LoggingConfig.ReturnInfo("Ensured that 'raw_table', 'clean_table', and 'Users' exist.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error ensuring tables exist: {e.Message}");
            }
        }
        public static void FlushDatabase(MySqlConnection connection)
        {
            try
            {
                LoggingConfig.ReturnInfo("Flushing database...");

                // Delete all rows from raw_table
                var deleteRawTableCommand = new MySqlCommand(SqlQueries.SqlQuery(11), connection);
                var rawRowsDeleted = deleteRawTableCommand.ExecuteNonQuery();
                LoggingConfig.ReturnInfo($"Deleted {rawRowsDeleted} rows from raw_table.");

                // Delete all rows from clean_table
                var deleteCleanTableCommand = new MySqlCommand(SqlQueries.SqlQuery(12), connection);
                var cleanRowsDeleted = deleteCleanTableCommand.ExecuteNonQuery();
                LoggingConfig.ReturnInfo($"Deleted {cleanRowsDeleted} rows from clean_table.");

                // Delete all rows from Users table
                var deleteUserTableCommand = new MySqlCommand(SqlQueries.SqlQuery(15), connection);
                var userRowsDeleted = deleteUserTableCommand.ExecuteNonQuery();
                LoggingConfig.ReturnInfo($"Deleted {userRowsDeleted} rows from Users.");
                LoggingConfig.ReturnInfo("Flushed database successfully.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error flushing database: {e.Message}");
            }
        }
    }
}
