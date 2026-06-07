using LoginAppWebApi.Models;
using Microsoft.Data.SqlClient;

namespace LoginAppWebApi.Repositories
{
    public class UserRepository
    {
        private const int CommandTimeoutSeconds = 30;
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> LoginExistsAsync(string login)
        {
            const string procedureName = "LoginExists";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);


            command.Parameters.AddWithValue("@login", login);

            await connection.OpenAsync();
            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            const string procedureName = "EmailExists";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@email", email);

            await connection.OpenAsync();
            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            const string procedureName = "Select * from GetUserByLoginFN(@login)";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = new SqlCommand(procedureName, connection);

            command.CommandType = System.Data.CommandType.Text;
            command.Parameters.AddWithValue("@login", login);

            await connection.OpenAsync();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string procedureName = "GetUserByEmail";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@email", email);

            await connection.OpenAsync();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }

        public async Task<int> CreateUserAsync(User user)
        {
            const string procedureName = "CreateUser";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@fullName", user.FullName);
            command.Parameters.AddWithValue("@login", user.Login);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);

            await connection.OpenAsync();
            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task ConfirmEmailAsync(int userId)
        {
            const string procedureName = "ConfirmEmail";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task IncreaseFailedAttemptsAsync(int userId)
        {
            const string procedureName = "IncreaseFailedLoginAttempts";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task ResetFailedAttemptsAsync(int userId)
        {
            const string procedureName = "ResetFailedLoginAttempts";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task LockUserAsync(int userId, DateTime lockoutEnd)
        {
            const string procedureName = "LockUser";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@lockoutEnd", lockoutEnd);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task CreateEmailVerificationCodeAsync(int userId, string codeHash, DateTime expiresAt)
        {
            const string procedureName = "CreateEmailVerificationCode";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@CodeHash", codeHash);
            command.Parameters.AddWithValue("@ExpiresAt", expiresAt);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetActiveEmailVerificationCodeHashAsync(int userId)
        {
            const string procedureName = "GetActiveEmailVerificationCodeHash";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            object? result = await command.ExecuteScalarAsync();

            return result?.ToString();
        }

        public async Task MarkEmailVerificationCodeAsUsedAsync(int userId)
        {
            const string procedureName = "MarkEmailVerificationCodeAsUsed";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task CreatePasswordResetCodeAsync(int userId, string codeHash, DateTime expiresAt)
        {
            const string procedureName = "CreatePasswordResetCode";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@codeHash", codeHash);
            command.Parameters.AddWithValue("@expiresAt", expiresAt);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetActivePasswordResetCodeHashAsync(int userId)
        {
            const string procedureName = "GetActivePasswordResetCodeHash";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@userId", userId);

            await connection.OpenAsync();
            object? result = await command.ExecuteScalarAsync();

            return result?.ToString();
        }

        public async Task MarkPasswordResetCodeAsUsedAsync(int userId)
        {
            const string procedureName = "MarkPasswordResetCodeAsUsed";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@userId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            const string procedureName = "UpdatePassword";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = CreateStoredProcedureCommand(procedureName, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@passwordHash", newPasswordHash);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        private static SqlCommand CreateStoredProcedureCommand(string procedureName, SqlConnection connection)
        {
            return new SqlCommand(procedureName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = CommandTimeoutSeconds
            };
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = (int)reader["Id"],
                FullName = reader["FullName"].ToString()!,
                Login = reader["Login"].ToString()!,
                Email = reader["Email"].ToString()!,
                PasswordHash = reader["PasswordHash"].ToString()!,
                IsEmailConfirmed = (bool)reader["IsEmailConfirmed"],
                FailedLoginAttempts = (int)reader["FailedLoginAttempts"],
                LockoutEnd = reader["LockoutEnd"] == DBNull.Value
                    ? null
                    : (DateTime)reader["LockoutEnd"]
            };
        }
    }
}
