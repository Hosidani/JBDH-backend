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
            LoggingConfig.ReturnInfo("Starting up Job database handler...\nSetting up connection...");
            EnsureJsonFileExists(JsonFile);
            using (var connection = OpenConnection())
            {
                LoggingConfig.ReturnInfo("Connection established...");
                EnsureTablesExist(connection);
                EtlPipeline.RunEtlPipeline(JsonFile);
                LoggingConfig.ReturnInfo("ETL pipeline completed...");
                
                // Delete the JSON file after the ETL process
                //DeleteJsonFile(JsonFile);

                LoggingConfig.ReturnInfo("Starting sorting queries...");
                // InternalSorting.TestInternalSorting(); // Placeholder for sorting queries
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

                using (var command = new MySqlCommand(createRawTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new MySqlCommand(createCleanTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                Console.WriteLine("Ensured that 'raw_table' and 'clean_table' exist.");
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
                Console.WriteLine("Flushing database...");

                // Delete all rows from raw_table
                var deleteRawTableCommand = new MySqlCommand(SqlQueries.SqlQuery(11), connection);
                var rawRowsDeleted = deleteRawTableCommand.ExecuteNonQuery();
                Console.WriteLine($"Deleted {rawRowsDeleted} rows from raw_table.");

                // Delete all rows from clean_table
                var deleteCleanTableCommand = new MySqlCommand(SqlQueries.SqlQuery(12), connection);
                var cleanRowsDeleted = deleteCleanTableCommand.ExecuteNonQuery();
                Console.WriteLine($"Deleted {cleanRowsDeleted} rows from clean_table.");

                Console.WriteLine("Database flushed successfully.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error flushing database: {e.Message}");
            }
        }
/*
        public static void ProcessAndInsertJobs()
        {
            try
            {
                if (!File.Exists(JsonFile))
                {
                    Console.Error.WriteLine($"File {JsonFile} not found.");
                    return;
                }

                var jsonData = File.ReadAllText(JsonFile);
                var rawJobs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData);

                var transformedJobs = EtlPipeline.TransformJobs(rawJobs);

                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                EnsureTablesExist(connection);

                foreach (var job in transformedJobs)
                {
                    var command = new MySqlCommand("INSERT INTO clean_table (title, location, description) VALUES (@title, @location, @description)", connection);
                    command.Parameters.AddWithValue("@title", job["title"]);
                    command.Parameters.AddWithValue("@location", job["location"]);
                    command.Parameters.AddWithValue("@description", job["description"]);
                    command.ExecuteNonQuery();
                }

                Console.WriteLine("Jobs successfully inserted into the database.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error processing jobs: {e.Message}");
            }
        }
*/
    }
}
