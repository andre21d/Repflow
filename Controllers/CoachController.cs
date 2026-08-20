using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers;

[ApiController]
[Route("api/coach")]
[Authorize]
public class CoachController : ControllerBase
{
    private readonly ICoachService _coachService;

    public CoachController(ICoachService coachService)
    {
        _coachService = coachService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCoaches() => Ok(await _coachService.GetAllCoachesAsync());

    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTopRatedCoaches() =>
        Ok(await _coachService.GetTopRatedCoachesAsync());

    [HttpGet("name/{name}")]
    public async Task<IActionResult> FindByName(string name)
    {
        try { return Ok(await _coachService.FindCoachesByNameAsync(name)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("participant/{participantId}")]
    public async Task<IActionResult> GetParticipantCoaches(string participantId) =>
        Ok(await _coachService.GetParticipantCoachesAsync(participantId));

    [HttpPost("{coachId}/rate")]
    public async Task<IActionResult> RateCoach(string coachId, RateCoachDto dto)
    {
        try { return Ok(await _coachService.RateCoachAsync(GetUserId(), coachId, dto)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("applications")]
    public async Task<IActionResult> SubmitApplication(SubmitCoachApplicationDto dto)
    {
        try
        {
            return Ok(await _coachService.SubmitCoachApplicationAsync(GetUserId(), dto));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("applications/me")]
    public async Task<IActionResult> GetMyApplication()
    {
        var application = await _coachService.GetMyCoachApplicationAsync(GetUserId());
        return application == null ? NotFound(new { message = "Coach application not found." }) : Ok(application);
    }

    [HttpGet("applications/pending")]
    public async Task<IActionResult> GetPendingApplications()
    {
        try { return Ok(await _coachService.GetPendingCoachApplicationsAsync(GetUserId())); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("applications/{applicationId}")]
    public async Task<IActionResult> ReviewApplication(string applicationId, ReviewCoachApplicationDto dto)
    {
        try { return Ok(await _coachService.ReviewCoachApplicationAsync(applicationId, GetUserId(), dto)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("training-requests")]
    public async Task<IActionResult> CreateTrainingRequest(CreateTrainingRequestDto dto)
    {
        try { return Ok(await _coachService.CreateTrainingRequestAsync(GetUserId(), dto)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("training-requests")]
    public async Task<IActionResult> GetTrainingRequests()
    {
        try { return Ok(await _coachService.GetCoachTrainingRequestsAsync(GetUserId())); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("training-requests/{requestId}")]
    public async Task<IActionResult> ReviewTrainingRequest(string requestId, ReviewTrainingRequestDto dto)
    {
        try { return Ok(await _coachService.ReviewTrainingRequestAsync(requestId, GetUserId(), dto)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in claims.");
}