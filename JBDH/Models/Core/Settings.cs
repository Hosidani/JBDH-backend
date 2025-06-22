using System;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models
using JBDH.Controllers;
using JBDH.Services;
using JBDH.Services.Configuration;

namespace JBDH.Models.Core
{
    public static class Settings
    {
        public static readonly string ConnectionString = DbConfig.DB_CONFIG; // Ensure this is a valid MySQL connection string
        public static string SetupConnection()
        {
            try
            {
                LoggingConfigService.ReturnInfo("Setting up database connection...");
                return DbConfig.DB_CONFIG;
            }
            catch (MySqlException e)
            {
                LoggingConfigService.ReturnError($"Failed to establish database connection: {e.Message}");
                throw;
            }
        }

        public static MySqlConnection OpenConnection()
        {
            try
            {
                var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Failed to establish a connection: {ex.Message}");
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
                    LoggingConfigService.ReturnInfo("Database connection closed.");
                }
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Failed to close database connection: {e.Message}");
            }
        }
 
        public static void CloseCursor(MySqlCommand command)
        {
            try
            {
                command?.Dispose();
                LoggingConfigService.ReturnInfo("Database command disposed.");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Failed to dispose database command: {e.Message}");
            }
        }
        public static void CommitToConnection(MySqlConnection connection)
        {
            try
            {
                connection?.BeginTransaction()?.Commit();
                LoggingConfigService.ReturnInfo("Transaction committed to the database.");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Failed to commit transaction: {e.Message}");
                throw;
            }
        }
    }
}