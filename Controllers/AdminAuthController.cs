using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers;

[ApiController]
[Route("api/admin-auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AdminAuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.AdminLoginAsync(dto);

        return result switch
        {
            "INVALID_CREDENTIALS" => Unauthorized(new { message = "Invalid email or password." }),
            "USER_BLOCKED" => StatusCode(StatusCodes.Status403Forbidden, new { message = "This account has been blocked." }),
            "NOT_ADMIN" => StatusCode(StatusCodes.Status403Forbidden, new { message = "Only admin accounts can log in here." }),
            _ => Ok(new { Token = result })
        };
    }
}