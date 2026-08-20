using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Models;
using Repflow.Api.Services;
using System.Security.Claims;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExercisesController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _exerciseService.GetAllAsync());

        [HttpGet("muscles")]
        public IActionResult GetMuscles() => Ok(Enum.GetNames<Muscle>());

        [HttpGet("main-muscle/{muscle}")]
        public async Task<IActionResult> GetByMainMuscle(Muscle muscle) =>
            Ok(await _exerciseService.GetByMainMuscleAsync(muscle));

        [HttpGet("secondary-muscle/{muscle}")]
        public async Task<IActionResult> GetBySecondaryMuscle(Muscle muscle) =>
            Ok(await _exerciseService.GetBySecondaryMuscleAsync(muscle));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var exercise = await _exerciseService.GetByIdAsync(id);
            return exercise == null
                ? NotFound(new { message = "Exercise not found." })
                : Ok(exercise);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExerciseDto dto)
        {
            try
            {
                var exercise = await _exerciseService.CreateAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetById), new { id = exercise.Id }, exercise);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateExerciseDto dto)
        {
            try
            {
                var exercise = await _exerciseService.UpdateAsync(GetUserId(), id, dto);
                return exercise == null
                    ? NotFound(new { message = "Exercise not found." })
                    : Ok(exercise);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _exerciseService.DeleteAsync(GetUserId(), id);
                return deleted
                    ? NoContent()
                    : NotFound(new { message = "Exercise not found." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in claims.");
    }
}