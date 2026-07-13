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
            var user = await _authService.RegisterAsync(dto);
            if (user == null)
            {
                return BadRequest(new { message = "Username or Email already exists." });
            }
            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);
        if (token == null)
        {
            return Unauthorized(new { message = "إيميل أو كلمة مرور خاطئة" });
        }

    return Ok(new { Token = token });
    }

        [HttpGet("test-protected")]
        [Authorize]
        public IActionResult TestProtected()
        {
            return Ok(new { message = "If you can see this, your JWT authentication works perfectly!" });
        }
    }
}