using LoginAppWebApi.Helpers;
using LoginAppWebApi.Models;
using LoginAppWebApi.Repositories;
using Microsoft.Data.SqlClient;

namespace LoginAppWebApi.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserRepository userRepository, EmailService emailService, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;

        }

        public async Task<ServiceResult> RegisterAsync(string fullName, string login, string email, string password)
        {
            try
            {
                string? validationError = ValidateRegisterData(fullName, login, email, password);
                if (validationError != null)
                    return ServiceResult.Fail(validationError);

                if (await _userRepository.LoginExistsAsync(login))
                    return ServiceResult.Fail("Login already exists.");

                if (await _userRepository.EmailExistsAsync(email))
                    return ServiceResult.Fail("Email already exists.");

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                User user = new User
                {
                    FullName = fullName,
                    Login = login,
                    Email = email,
                    PasswordHash = passwordHash
                };

                int userId = await _userRepository.CreateUserAsync(user);

                string code = GenerateCode();
                string codeHash = BCrypt.Net.BCrypt.HashPassword(code);
                DateTime expiresAt = DateTime.Now.AddMinutes(10);

                await _userRepository.CreateEmailVerificationCodeAsync(userId, codeHash, expiresAt);
                await _emailService.SendVerificationCodeAsync(email, code);

                _logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}", userId, email);

                return ServiceResult.Ok();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during registration for email {Email}", email);

                return ServiceResult.Fail(SqlErrorHelper.GetUserMessage(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email {Email}", email);
                return ServiceResult.Fail("Something went wrong. Please try again later.");
            }
        }

        public async Task<ServiceResult> VerifyEmailAsync(string email, string code)
        {
            try
            {
                _logger.LogInformation("Email verification started for email {Email}", email);
                User? user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("Email verification failed. User not found for email {Email}", email);
                    return ServiceResult.Fail("User not found.");
                }

                if (user.IsEmailConfirmed)
                {
                    _logger.LogWarning("Email verification skipped. Email already confirmed for userId {UserId}", user.Id);
                    return ServiceResult.Fail("Email is already confirmed.");
                }

                string? codeHash = await _userRepository.GetActiveEmailVerificationCodeHashAsync(user.Id);

                if (codeHash == null)
                {
                    _logger.LogWarning("Email verification failed. Code not found or expired for userId {UserId}", user.Id);
                    return ServiceResult.Fail("Verification code not found or expired.");
                }

                bool isCodeCorrect = BCrypt.Net.BCrypt.Verify(code, codeHash);

                if (!isCodeCorrect)
                {
                    _logger.LogWarning("Email verification failed. Invalid code for userId {UserId}", user.Id);
                    return ServiceResult.Fail("Invalid verification code.");
                }

                await _userRepository.ConfirmEmailAsync(user.Id);
                await _userRepository.MarkEmailVerificationCodeAsUsedAsync(user.Id);

                _logger.LogInformation("Email verified successfully for userId {UserId}", user.Id);

                return ServiceResult.Ok();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during email verification for email {Email}", email);

                return ServiceResult.Fail(SqlErrorHelper.GetUserMessage(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during email verification for email {Email}", email);

                return ServiceResult.Fail("Something went wrong. Please try again later.");
            }
        }

        public async Task<ServiceResult> LoginAsync(string login, string password)
        {
            try
            {
                _logger.LogInformation("Login attempt for login {Login}", login);

                User? user = await _userRepository.GetByLoginAsync(login);

                if (user == null)
                {

                    _logger.LogWarning("Login failed. User not found for login {Login}", login);
                    return ServiceResult.Fail("Invalid login or password.");
                }

                if (!user.IsEmailConfirmed)
                {
                    _logger.LogWarning("Login blocked. Email is not confirmed for userId {UserId}", user.Id);
                    return ServiceResult.Fail("Please verify your email first.");
                }
                if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
                {
                    _logger.LogWarning("Login blocked. UserId {UserId} is locked until {LockoutEnd}", user.Id, user.LockoutEnd);
                    return ServiceResult.Fail($"Account is locked until {user.LockoutEnd}");
                }

                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (!isPasswordCorrect)
                {
                    _logger.LogWarning("Wrong password for userId {UserId}", user.Id);
                    await _userRepository.IncreaseFailedAttemptsAsync(user.Id);

                    if (user.FailedLoginAttempts + 1 >= 3)
                    {
                        DateTime lockoutEnd = DateTime.Now.AddMinutes(10);
                        await _userRepository.LockUserAsync(user.Id, lockoutEnd);

                        _logger.LogWarning("UserId {UserId} locked until {LockoutEnd}", user.Id, lockoutEnd);
                        return ServiceResult.Fail($"Too many attempts. Account locked until {lockoutEnd}");
                    }

                    return ServiceResult.Fail("Invalid login or password.");
                }

                await _userRepository.ResetFailedAttemptsAsync(user.Id);

                _logger.LogInformation("Login successful for userId {UserId}", user.Id);

                return ServiceResult.Ok();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during login for login {Login}", login);
                return ServiceResult.Fail(SqlErrorHelper.GetUserMessage(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for login {Login}", login);
                return ServiceResult.Fail("Something went wrong. Please try again later.");
            }
        }

        public async Task<ServiceResult> SendResetCodeAsync(string email)
        {
            try
            {
                User? user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                    return ServiceResult.Ok();

                string code = GenerateCode();
                string codeHash = BCrypt.Net.BCrypt.HashPassword(code);
                DateTime expiresAt = DateTime.Now.AddMinutes(10);

                await _userRepository.CreatePasswordResetCodeAsync(user.Id, codeHash, expiresAt);
                await _emailService.SendPasswordResetCodeAsync(email, code);

                return ServiceResult.Ok();
            }
            catch (SqlException ex)
            {
                return ServiceResult.Fail(SqlErrorHelper.GetUserMessage(ex));
            }
            catch
            {
                return ServiceResult.Fail("Something went wrong. Please try again later.");
            }
        }

        public async Task<ServiceResult> ResetPasswordAsync(string email, string code, string newPassword)
        {
            try
            {
                string? passwordError = ValidatePassword(newPassword);
                if (passwordError != null)
                    return ServiceResult.Fail(passwordError);

                User? user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                    return ServiceResult.Fail("Invalid request.");

                string? codeHash = await _userRepository.GetActivePasswordResetCodeHashAsync(user.Id);

                if (codeHash == null)
                    return ServiceResult.Fail("Code expired.");

                if (!BCrypt.Net.BCrypt.Verify(code, codeHash))
                    return ServiceResult.Fail("Invalid code.");

                string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                await _userRepository.UpdatePasswordAsync(user.Id, newHash);
                await _userRepository.MarkPasswordResetCodeAsUsedAsync(user.Id);

                return ServiceResult.Ok();
            }
            catch (SqlException ex)
            {
                return ServiceResult.Fail(SqlErrorHelper.GetUserMessage(ex));
            }
            catch
            {
                return ServiceResult.Fail("Something went wrong. Please try again later.");
            }
        }

        public async Task<ServiceResult> ChangePasswordAsync(string login, string currentPassword, string newPassword)
        {
            try
            {
                string? passwordError = ValidatePassword(newPassword);
                if (passwordError != null)
                    return ServiceResult.Fail(passwordError);

                User? user = await _userRepository.GetByLoginAsync(login);

                if (user == null)
                    return ServiceResult.Fail("User not found.");

                if (!user.IsEmailConfirmed)
                    return ServiceResult.Fail("Please verify your email first.");

                bool isCurrentPasswordCorrect = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);

                if (!isCurrentPasswordCorrect)
                    return ServiceResult.Fail("Wrong current password.");

                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                await _userRepository.UpdatePasswordAsync(user.Id, newPasswordHash);

                return ServiceResult.Ok();
            }
            catch (SqlException ex)
            {
                return ServiceResult.Fail(SqlErrorHelper.GetUserMessage(ex));
            }
            catch
            {
                return ServiceResult.Fail("Something went wrong. Please try again later.");
            }
        }

        private static string GenerateCode()
        {
            int code = Random.Shared.Next(100000, 1000000);
            return code.ToString();
        }

        private static string? ValidateRegisterData(string fullName, string login, string email, string password)
        {
            if (!IsValidFullName(fullName))
                return "Full name must contain at least 2 characters.";

            if (!IsValidLogin(login))
                return "Login must be 4-30 characters and contain only letters, digits or underscore.";

            if (!IsValidEmail(email))
                return "Invalid email format.";

            return ValidatePassword(password);
        }

        private static bool IsValidFullName(string fullName)
        {
            return !string.IsNullOrWhiteSpace(fullName) && fullName.Length >= 2;
        }

        private static bool IsValidLogin(string login)
        {
            return !string.IsNullOrWhiteSpace(login)
                   && login.Length >= 4
                   && login.Length <= 30
                   && login.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email)
                   && email.Contains('@')
                   && email.Contains('.');
        }

        private static string? ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Password cannot be empty.";

            if (password.Length < 8)
                return "Password must be at least 8 characters.";

            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter.";

            if (!password.Any(char.IsLower))
                return "Password must contain at least one lowercase letter.";

            if (!password.Any(char.IsDigit))
                return "Password must contain at least one digit.";

            if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch)))
                return "Password must contain at least one special character.";

            return null;
        }
    }
}
