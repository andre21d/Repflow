using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "admin,superadmin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly ICoachService _coachService;
    private readonly IExerciseService _exerciseService;

    public DashboardController(
        IDashboardService dashboard,
        ICoachService coachService,
        IExerciseService exerciseService)
    {
        _dashboard = dashboard;
        _coachService = coachService;
        _exerciseService = exerciseService;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics() => await Execute(() => _dashboard.GetStatisticsAsync(UserId()));

    [HttpGet("coach-applications")]
    public async Task<IActionResult> GetCoachApplications() => await Execute(() => _coachService.GetAllCoachApplicationsAsync(UserId()));

    [HttpGet("coach-applications/pending")]
    public async Task<IActionResult> GetPendingCoachApplications() =>
        await Execute(() => _coachService.GetPendingCoachApplicationsAsync(UserId()));

    [HttpPatch("coach-applications/{applicationId}")]
    public async Task<IActionResult> ReviewCoachApplication(string applicationId, ReviewCoachApplicationDto dto) =>
        await Execute(() => _coachService.ReviewCoachApplicationAsync(applicationId, UserId(), dto));

    [HttpPatch("users/{userId}/block")]
    public async Task<IActionResult> BlockUser(string userId, BlockResourceDto dto) => await Execute(async () =>
    {
        await _dashboard.SetUserBlockedAsync(UserId(), userId, dto.Blocked);
        return new { userId, blocked = dto.Blocked };
    });

    [HttpPatch("posts/{postId}/block")]
    public async Task<IActionResult> BlockPost(string postId, BlockResourceDto dto) => await Execute(async () =>
    {
        await _dashboard.SetPostBlockedAsync(UserId(), postId, dto.Blocked);
        return new { postId, blocked = dto.Blocked };
    });

    [HttpPost("admins")]
    public async Task<IActionResult> CreateAdmin(CreateAdminDto dto) => await Execute(async () =>
    {
        await _dashboard.CreateAdminAsync(UserId(), dto);
        return new { message = "Admin account created." };
    }, StatusCodes.Status201Created);

    [HttpGet("admins")]
    public async Task<IActionResult> GetAdmins() => await Execute(() => _dashboard.GetAdminsAsync(UserId()));

    [HttpDelete("admins/{adminId}")]
    public async Task<IActionResult> DeleteAdmin(string adminId) => await Execute(async () =>
    {
        await _dashboard.DeleteAdminAsync(UserId(), adminId);
        return new { message = "Admin account deleted." };
    });

    [HttpPost("exercises")]
    public async Task<IActionResult> CreateExercise(CreateExerciseDto dto) => await Execute(async () =>
    {
        var exercise = await _exerciseService.CreateAsync(UserId(), dto);
        return exercise;
    }, StatusCodes.Status201Created);

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in claims.");

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action, int successStatus = StatusCodes.Status200OK)
    {
        try
        {
            var result = await action();
            return StatusCode(successStatus, result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
