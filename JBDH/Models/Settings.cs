using System;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models
using JBDH.Controllers;
 
namespace JBDH.Models
{
    public static class Settings
    {
        public static string SetupConnection()
        {
            try
            {
                LoggingConfig.ReturnInfo("Setting up database connection...");
                return DbConfig.DB_CONFIG;
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
    }

    public static class AISettings
    {
        private const string APIKey = "sk-3c7fc5ab902a4832b4799b9f1d10fb0e";
        private const string APIUrl = "https://api.deepseek.com/chat/completions";
        private const string Model = "deepseek-chat";
        // How should we build the messeage? 
        private const string SystemMessage = "You are a helpful assistant that helps users find jobs. You will be given a job description and location, and you will return the best job matches from the database. You will also provide a summary of the job and its location.";
        private const string Temperature = "0.7";
        private const string Max_Tokens = "1000";
        private const bool IsStreaming = false;
        private const string TopP = "1.0";
        public static string GetAPIKey()
        {
            return APIKey;
        }
        public static string GetAPIUrl()
        {
            return APIUrl;
        }
        public static string GetModel()
        {
            return Model;
        }
        public static string GetSystemMessage()
        {
            return SystemMessage;
        }
        public static string GetTemperature()
        {
            return Temperature;
        }
        public static string GetMaxTokens()
        {
            return Max_Tokens;
        }
        public static bool GetIsStreaming()
        {
            return IsStreaming;
        }
        public static double GetTopP()
        {
            return float.Parse(TopP);
        }
}
}