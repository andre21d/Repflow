using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;
using System.Security.Claims;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserSessionsController : ControllerBase
    {
        private readonly IUserSessionService _sessionService;

        public UserSessionsController(IUserSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserSessionInputDto dto)
        {
            try
            {
                var session = await _sessionService.CreateAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetByDay), new { date = session.Date.ToString("yyyy-MM-dd") }, session);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _sessionService.GetAllAsync(GetUserId()));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserSessionInputDto dto)
        {
            try
            {
                var session = await _sessionService.UpdateAsync(GetUserId(), id, dto);
                return session == null
                    ? NotFound(new { message = "Session not found." })
                    : Ok(session);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("day/{date}")]
        public async Task<IActionResult> GetByDay(string date)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedDate))
                return BadRequest(new { message = "Date must use yyyy-MM-dd format." });

            return Ok(await _sessionService.GetByDayAsync(GetUserId(), parsedDate));
        }

        [HttpGet("month/{year:int}/{month:int}")]
        public async Task<IActionResult> GetByMonth(int year, int month)
        {
            try
            {
                return Ok(await _sessionService.GetByMonthAsync(GetUserId(), year, month));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in claims.");
    }
}