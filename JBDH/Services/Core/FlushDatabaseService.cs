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
                var deleteRawTableCommand = new MySqlCommand(SqlQueries.SqlQuery(202), connection);
                var rawRowsDeleted = deleteRawTableCommand.ExecuteNonQuery();
                LoggingConfigService.ReturnInfo($"Deleted {rawRowsDeleted} rows from raw_table.");

                // Delete all rows from Jobs
                var deleteJobsTableCommand = new MySqlCommand(SqlQueries.SqlQuery(203), connection);
                var jobsRowsDeleted = deleteJobsTableCommand.ExecuteNonQuery();
                LoggingConfigService.ReturnInfo($"Deleted {jobsRowsDeleted} rows from Jobs.");

                // Delete all rows from EmailVerificationTokens table
                var deleteEmailVerificationTableCommand = new MySqlCommand(SqlQueries.SqlQuery(200), connection);
                var emailVerificationRowsDeleted = deleteEmailVerificationTableCommand.ExecuteNonQuery();
                LoggingConfigService.ReturnInfo($"Deleted {emailVerificationRowsDeleted} rows from EmailVerificationTokens table.");

                // Delete all rows from AIResponses table
                var deleteAIResponsesTableCommand = new MySqlCommand(SqlQueries.SqlQuery(205), connection);
                var aiResponsesRowsDeleted = deleteAIResponsesTableCommand.ExecuteNonQuery();
                LoggingConfigService.ReturnInfo($"Deleted {aiResponsesRowsDeleted} rows from AIResponses table.");

                // Delete all rows from UserRoleAssignments table (must be before Users)
                var deleteUserRoleAssignmentsCommand = new MySqlCommand(SqlQueries.SqlQuery(206), connection);
                var userRoleAssignmentsRowsDeleted = deleteUserRoleAssignmentsCommand.ExecuteNonQuery();
                LoggingConfigService.ReturnInfo($"Deleted {userRoleAssignmentsRowsDeleted} rows from UserRoleAssignments.");

                // Delete all rows from Users table
                var deleteUsersTableCommand = new MySqlCommand(SqlQueries.SqlQuery(204), connection);
                var userRowsDeleted = deleteUsersTableCommand.ExecuteNonQuery();
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