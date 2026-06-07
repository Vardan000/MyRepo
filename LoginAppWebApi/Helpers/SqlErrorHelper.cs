using Microsoft.Data.SqlClient;

namespace LoginAppWebApi.Helpers
{
    public static class SqlErrorHelper
    {
        public static string GetUserMessage(SqlException ex)
        {
            return ex.Number switch
            {
                -2 => "Database is busy. Please try again later.",
                2601 or 2627 => "This value already exists.",
                53 => "Database server was not found.",
                4060 => "Database cannot be opened.",
                18456 => "Database login failed.",
                _ => "Database error. Please try again later."
            };
        }
    }
}
