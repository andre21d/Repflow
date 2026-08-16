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
    public class CommunityController : ControllerBase
    {
        private readonly ICoummunityService _communityService;

        public CommunityController(ICoummunityService communityService)
        {
            _communityService = communityService;
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(string communityId)
        {
            var userId = GetUserId();
            var result = await _communityService.JoinCommunityAsync(communityId, userId);
            if (result == "Already a member")
                return BadRequest(new { message = "You are already a member of this community." });
            else if (result == "Request sent")
                return Ok(new { message = "Your request to join the private community has been sent." });
            else if (result == "Request pending")
                return BadRequest(new { message = "Your request to join the private community is still pending." });
            else if (result == "Community not found")
                return NotFound(new { message = "Community not found." });
            else 
                return Ok(new { message = "You have successfully joined the community." });
            
        }
     
        [HttpPost("/requests/{requestId}")]
        public async Task<IActionResult> HandleRequest( string requestId, bool accepted)
        {
            try{
                var adminId = GetUserId();
                var result = await _communityService.CommunityRequestsAsync(requestId, adminId, accepted);
                if (result)
                {
                    return Ok(new { message = accepted ? "Request approved." : "Request rejected." });
                }
                else
                {
                    return BadRequest(new { message = "Failed to process the request." });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCommunityDto dto)
        {
            var userId = GetUserId();
          try
          {  var community = await _communityService.CreateCommunityAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = community.Id }, community);}
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var community = await _communityService.GetCommunityByIdAsync(id);
            if (community == null) return NotFound(new { message = "Community not found." });
            return Ok(community);
        }
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> Leave(string id){
        
            var userId = GetUserId();
            var result = await _communityService.LeaveCommunityAsync(id, userId);
            if (result == "Community or membership not found")
                return BadRequest(new { message = "You are not a member of this community." });
            else if (result == "Community deleted as you were the owner")
                return NotFound(new { message = "Community deleted as you were the owner." });
            else
                return Ok(new { message = "You have successfully left the community." });
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User ID not found in claims.");
        }
    }
}