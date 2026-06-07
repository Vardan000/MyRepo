using LoginAppWebApi.Dtos.Auth;
using LoginAppWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using LoginAppWebApi.Models;


namespace LoginAppWebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            ServiceResult result = await _authService.RegisterAsync(
                request.FullName,
                request.Login,
                request.Email,
                request.Password);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "User created. Verification code sent to your email."
            });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            ServiceResult result = await _authService.VerifyEmailAsync(
                request.Email,
                request.Code);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Email confirmed successfully. Now you can login."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            ServiceResult result = await _authService.LoginAsync(
                request.Login,
                request.Password);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Login successful."
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            ServiceResult result = await _authService.SendResetCodeAsync(request.Email);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "If the email exists, a reset code was sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            ServiceResult result = await _authService.ResetPasswordAsync(
                request.Email,
                request.Code,
                request.NewPassword);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Password successfully changed."
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            ServiceResult result = await _authService.ChangePasswordAsync(
                request.Login,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Password changed successfully."
            });
        }
    }
}