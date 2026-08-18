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
         [HttpGet("{communityId}/members")]
        public async Task<IActionResult> GetMembers(string communityId)
        {
            var userId = GetUserId();
            try{var members = await _communityService.GetCommunityMembersAsync(communityId,userId);
            if (members == null)
                return NotFound(new { message = "Community not found." });
            return Ok(members);}

            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
        [HttpGet("user/communities")]
        public async Task<IActionResult> GetUserCommunities()
        {
            var userId = GetUserId();
            var communities = await _communityService.GetUserCommunitiesAsync(userId);
            return Ok(communities);
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

        [HttpPost("{communityId}/join")]
        public async Task<IActionResult> Join(string communityId)
        {
            
            var userId = GetUserId();
            string result = await _communityService.JoinCommunityAsync(communityId, userId);
           
            if (result == "Already a member")
                return BadRequest(new { message = "You are already a member of this community." });
            else if (result == "Request Sent")
                return Ok(new { message = "Your request to join the private community has been sent." });
            else if (result == "Request Pending")
                return BadRequest(new { message = "Your request to join the private community is still pending." });
            else if (result == "Community not found")
                return NotFound(new { message = "Community not found." });
            else 
                return Ok(result);
            
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
        [HttpPatch("{communityId}/make-admin/{userId}")]
        public async Task<IActionResult> MakeAdmin(string communityId, string userId)
        {
            var ownerId = GetUserId();
            var result = await _communityService.MakeAdminAsync(communityId, userId, ownerId);
            if (result == "Community not found")
                return NotFound(new { message = "Community not found." });
            else if (result == "Only the owner can make someone an admin")
                return StatusCode(403, new { message = "Only the owner can make someone an admin." });
            else
            return Ok(new { message = result });
        }
        [HttpPatch("{communityId}/remove-admin/{userId}")]
        public async Task<IActionResult> RemoveAdmin(string communityId, string userId)
        {
            string ownerId = GetUserId();
            var result = await _communityService.RemoveAdminAsync(communityId, userId, ownerId);
            if (result == "Community not found")
                return NotFound(new { message = "Community not found." });
            else if (result == "Only the owner can remove an admin")
                return StatusCode(403, new { message = "Only the owner can remove an admin." });
            else
            return Ok(new { message = result });
        }
        [HttpDelete("{communityId}/remove-member/{userId}")]
        public async Task<IActionResult> RemoveMember(string communityId, string userId)        
        {
            var adminId = GetUserId();
            var result = await _communityService.RemoveMemberAsync(communityId, userId, adminId);
            if (result == "Community not found")
                return NotFound(new { message = "Community not found." });
            else if (result == "User is not a member")
                return BadRequest(new { message = "User is not a member of this community." });
            else if (result == "Only admins can remove members")
                return StatusCode(403, new { message = "Only admins can remove members." });
            else
            return Ok(new { message = result });
        }
       
        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User ID not found in claims.");
        }

    }
}