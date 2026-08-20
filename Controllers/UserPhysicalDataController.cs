using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/physical-data")]
[Authorize]
public class UserPhysicalDataController : ControllerBase
{
    private readonly IUserPhysicalDataService _service;

    public UserPhysicalDataController(IUserPhysicalDataService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string userId)
    {
        try { return Ok(await _service.GetAsync(GetUserId(), userId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut]
    public async Task<IActionResult> Update(string userId, UpdatePhysicalDataDto dto)
    {
        try { return Ok(await _service.UpdateAsync(GetUserId(), userId, dto)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("weights")]
    public async Task<IActionResult> AddWeight(string userId, AddWeightDto dto)
    {
        try { return Ok(await _service.AddWeightAsync(GetUserId(), userId, dto)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in claims.");
}