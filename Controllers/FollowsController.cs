using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.Services;
using System.Security.Claims;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowsController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowsController(IFollowService followService)
        {
            _followService = followService;
        }

        [HttpPost("{targetUserId}")]
        public async Task<IActionResult> ToggleFollow(string targetUserId)
        {
            try
            {
                var currentUserId = GetUserId();
                var result = await _followService.ToggleFollowAsync(currentUserId, targetUserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("following")]
        public async Task<IActionResult> GetFollowing()
        {
            var userId = GetUserId();
            var followingIds = await _followService.GetFollowingUserIdsAsync(userId);
            return Ok(followingIds);
        }

        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowers()
        {
            var userId = GetUserId();
            var followerIds = await _followService.GetFollowerUserIdsAsync(userId);
            return Ok(followerIds);
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }
    }
}