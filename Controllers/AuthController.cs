using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            
            if (result == "Email already exists.")
            {
                return BadRequest(new { message = result });
            }
            
            return Ok(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (result == "INVALID_CREDENTIALS")
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (result == "EMAIL_NOT_VERIFIED")
            {
                return BadRequest(new { message = "Please verify your email first." });
            }

            return Ok(new { Token = result });
        }
        
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            var result = await _authService.VerifyEmailAsync(dto.Token);
            if (!result) return BadRequest(new { message = "Invalid or expired verification token." });
            
            return Ok(new { message = "Account verified successfully! You can now log in." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            // Returns a generic success message to protect registered email addresses from enumeration attacks
            return Ok(new { message = "If the account exists, a password reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result) return BadRequest(new { message = "Invalid or expired reset token." });
            
            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpGet("test-protected")]
        [Authorize]
        public IActionResult TestProtected()
        {
            return Ok(new { message = "If you can see this, your JWT authentication works perfectly!" });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }
    }
}