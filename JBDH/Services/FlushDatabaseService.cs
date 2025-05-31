using JBDH.Models;
using MySql.Data.MySqlClient; // For MySqlConnection, MySqlCommand, etc.

namespace JBDH.Services
{
    public class FlushDatabaseService
    {
        public static void FlushDatabase()
            {
                try
                {
                    MySqlConnection connection = Settings.OpenConnection();
                    LoggingConfigService.ReturnInfo("Flushing database...");

                    // Delete all rows from raw_table
                    var deleteRawTableCommand = new MySqlCommand(SqlQueries.SqlQuery(11), connection);
                    var rawRowsDeleted = deleteRawTableCommand.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo($"Deleted {rawRowsDeleted} rows from raw_table.");

                    // Delete all rows from clean_table
                    var deleteCleanTableCommand = new MySqlCommand(SqlQueries.SqlQuery(12), connection);
                    var cleanRowsDeleted = deleteCleanTableCommand.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo($"Deleted {cleanRowsDeleted} rows from clean_table.");

                    // Delete all rows from email_verification table
                    var deleteEmailVerificationTableCommand = new MySqlCommand(SqlQueries.SqlQuery(4), connection);
                    var emailVerificationRowsDeleted = deleteEmailVerificationTableCommand.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo($"Deleted {emailVerificationRowsDeleted} rows from email_verification table.");
                    
                    // Delete all rows from AIResponses table
                    var deleteAIResponsesTableCommand = new MySqlCommand(SqlQueries.SqlQuery(20), connection);
                    var aiResponsesRowsDeleted = deleteAIResponsesTableCommand.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo($"Deleted {aiResponsesRowsDeleted} rows from AIResponses table.");
                    // Delete all rows from Users table
                    var deleteUserTableCommand = new MySqlCommand(SqlQueries.SqlQuery(15), connection);
                    var userRowsDeleted = deleteUserTableCommand.ExecuteNonQuery();
                    LoggingConfigService.ReturnInfo($"Deleted {userRowsDeleted} rows from Users.");
                    LoggingConfigService.ReturnInfo("Flushed database successfully.");
                }
                catch (Exception e)
                {
                    LoggingConfigService.ReturnError($"Error flushing database: {e.Message}");
                }
            }
    }
}