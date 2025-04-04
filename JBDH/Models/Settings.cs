using System;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models
using JBDH.Controllers;
 
namespace JBDH.Models
{
    public static class Settings
    {
        public static MySqlConnection SetupConnection()
        {
            try
            {
                LoggingConfig.ReturnInfo("Setting up database connection...");
                var connection = new MySqlConnection(DbConfig.DB_CONFIG);
                connection.Open();
                LoggingConfig.ReturnInfo("Database connection established.");
                return connection;
            }
            catch (MySqlException e)
            {
                LoggingConfig.ReturnError($"Failed to establish database connection: {e.Message}");
                throw;
            }
        }
 
        public static void CloseConnection(MySqlConnection connection)
        {
            try
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                    LoggingConfig.ReturnInfo("Database connection closed.");
                }
            }
            catch (Exception e)
            {
                LoggingConfig.ReturnError($"Failed to close database connection: {e.Message}");
            }
        }
 
        public static void CloseCursor(MySqlCommand command)
        {
            try
            {
                command?.Dispose();
                LoggingConfig.ReturnInfo("Database command disposed.");
            }
            catch (Exception e)
            {
                LoggingConfig.ReturnError($"Failed to dispose database command: {e.Message}");
            }
        }
 
        public static void CommitToConnection(MySqlConnection connection)
        {
            try
            {
                connection?.BeginTransaction()?.Commit();
                LoggingConfig.ReturnInfo("Transaction committed to the database.");
            }
            catch (Exception e)
            {
                LoggingConfig.ReturnError($"Failed to commit transaction: {e.Message}");
                throw;
            }
        }
 
        public static void TestConnection()
        {
            try
            {
                using (var connection = SetupConnection())
                {
                    LoggingConfig.ReturnInfo("Testing database connection...");
                    LoggingConfig.ReturnInfo("Database connection test successful.");
                }
            }
            catch (Exception e)
            {
                LoggingConfig.ReturnError($"Database connection test failed: {e.Message}");
                throw;
            }
        }
    }
}