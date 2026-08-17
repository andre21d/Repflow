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
    public class ChallengeController : ControllerBase
    {
        private readonly IChallengeService _challengeService;

        public ChallengeController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        } 
        [HttpPost("{communityId}/create")]
        public async Task<IActionResult> CreateChallenge(string communityId, [FromBody] CreateChallengeDto dto)
        {
            var userId = GetUserId();
            try
            {
                var challenge = await _challengeService.CreateChallengeAsync(userId, communityId, dto);
                return CreatedAtAction(nameof(GetChallengeById), new { id = challenge.Id }, challenge);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        [HttpGet("{challengeId}")]
        public async Task<IActionResult> GetChallengeById(string challengeId)
        {
            var challenge = await _challengeService.GetChallengeByIdAsync(challengeId);
            if (challenge == null)
            {
                return NotFound(new { message = "Challenge not found." });
            }
            return Ok(challenge);
        }
        [HttpPost("{challengeId}/join")]
        public async Task<IActionResult> JoinChallenge(string challengeId) 
         {
                var userId = GetUserId();
                var challenge = await _challengeService.GetChallengeByIdAsync(challengeId);
                if (challenge == null)
                {
                 return NotFound(new { message = "Challenge not found." });
                }
                var result = await _challengeService.JoinChallengeAsync(challenge.CommunityId, userId, challengeId);
                if (result == "Already a participant")
                 return BadRequest(new { message = "You are already a participant of this challenge." });
                else if (result == "Request sent")
                 return Ok(new { message = "Your request to join the challenge has been sent." });
                else
                 return Ok(new { message = result });
            }
        [HttpPut("{challengeId}/update-participant")]
        public async Task<IActionResult> UpdateParticipant(string challengeId, [FromBody] double goalParticipation)
        {
            var userId = GetUserId();
            var challenge = await _challengeService.GetChallengeByIdAsync(challengeId);
            if (challenge == null)
            {
                return NotFound(new { message = "Challenge not found." });
            }
            var result = await _challengeService.updateParticipantAsync(challengeId, userId, goalParticipation);
            if (result == "Participant not found")
                return NotFound(new { message = "You are not a participant of this challenge." });
            return Ok(new { message = result });
        }
        [HttpGet("community/{communityId}")]
        public async Task<IActionResult> GetChallengesByCommunityId(string communityId)
        {
            var challenges = await _challengeService.GetChallengesByCommunityIdAsync(communityId);
            return Ok(challenges);
        }
        [HttpGet("community/{communityId}/active")]
        public async Task<IActionResult> GetActiveChallengesByCommunityId(string communityId)
        {
            var challenges = await _challengeService.GetActiveChallengesByCommunityId(communityId);
            return Ok(challenges);
        }
        [HttpGet("user")]
        public async Task<IActionResult> GetChallengesByUserId()
        {      
            var userId = GetUserId();
            var challenges = await _challengeService.GetChallengesByUserIdAsync(userId);
            return Ok(challenges);
        }
        [HttpGet("user/joinable")]
        public async Task<IActionResult> GetChallengesUserCanJoin()
        {
            var userId = GetUserId();
            var challenges = await _challengeService.GetChallengesUserCanJoinAsync(userId);
            return Ok(challenges);
        }

         private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User ID not found in claims.");
        }
          
    }
    
}           