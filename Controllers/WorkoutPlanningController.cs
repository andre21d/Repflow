using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/workout-planning")]
    [Authorize]
    public class WorkoutPlanningController : ControllerBase
    {
        private readonly IWorkoutPlanningService _service;

        public WorkoutPlanningController(IWorkoutPlanningService service)
        {
            _service = service;
        }

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate(CreateWorkoutTemplateDto dto) =>
            await Execute(() => _service.CreateTemplateAsync(GetUserId(), dto), StatusCodes.Status201Created);

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates() => Ok(await _service.GetTemplatesAsync(GetUserId()));

        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplate(string id)
        {
            var template = await _service.GetTemplateAsync(GetUserId(), id);
            return template == null ? NotFound(new { message = "Template not found." }) : Ok(template);
        }

        [HttpDelete("templates/{id}")]
        public async Task<IActionResult> ArchiveTemplate(string id)
        {
            try { await _service.ArchiveTemplateAsync(GetUserId(), id); return NoContent(); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        }

        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan(CreateWorkoutPlanDto dto) =>
            await Execute(() => _service.CreatePlanAsync(GetUserId(), dto), StatusCodes.Status201Created);

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans() => Ok(await _service.GetPlansAsync(GetUserId()));

        [HttpGet("plans/{id}")]
        public async Task<IActionResult> GetPlan(string id)
        {
            var plan = await _service.GetPlanAsync(GetUserId(), id);
            return plan == null ? NotFound(new { message = "Plan not found." }) : Ok(plan);
        }

        [HttpPost("plans/{id}/send")]
        public async Task<IActionResult> SendPlan(string id) => await Execute(async () => { await _service.SendPlanAsync(GetUserId(), id); return new { message = "Plan sent." }; });

        [HttpPost("plans/{id}/accept")]
        public async Task<IActionResult> AcceptPlan(string id) => await Execute(async () => { await _service.AcceptPlanAsync(GetUserId(), id, true); return new { message = "Plan accepted." }; });

        [HttpPost("plans/{id}/reject")]
        public async Task<IActionResult> RejectPlan(string id) => await Execute(async () => { await _service.AcceptPlanAsync(GetUserId(), id, false); return new { message = "Plan rejected." }; });

        [HttpPost("plans/{id}/start")]
        public async Task<IActionResult> StartPlan(string id, StartWorkoutPlanDto dto) =>
            await Execute(() => _service.StartPlanAsync(GetUserId(), id, dto));

        [HttpPost("plans/{planId}/days/{dayId}/complete")]
        public async Task<IActionResult> CompleteDay(string planId, string dayId, CompleteWorkoutDayDto dto) =>
            await Execute(() => _service.CompleteDayAsync(GetUserId(), planId, dayId, dto));

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in claims.");

        private async Task<IActionResult> Execute<T>(Func<Task<T>> action, int successStatus = StatusCodes.Status200OK)
        {
            try
            {
                var result = await action();
                return successStatus == StatusCodes.Status201Created ? StatusCode(successStatus, result) : Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }
    }
}