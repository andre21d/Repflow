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
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            var userId = GetUserId();
            var post = await _postService.CreatePostAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _postService.GetAllPostsAsync();
            return Ok(posts);
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed()
        {
            var userId = GetUserId();
            var posts = await _postService.GetFeedPostsAsync(userId);
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var post = await _postService.GetPostByIdAsync(id);
            if (post == null) return NotFound(new { message = "Post not found." });

            return Ok(post);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = GetUserId();
            var result = await _postService.DeletePostAsync(id, userId);

            if (!result) return BadRequest(new { message = "Unable to delete post or unauthorized." });

            return Ok(new { message = "Post deleted successfully." });
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(string id)
        {
            var userId = GetUserId();
            var isLiked = await _postService.ToggleLikeAsync(id, userId);

            return Ok(new { 
                message = isLiked ? "Post liked successfully." : "Post unliked successfully.",
                isLiked = isLiked 
            });
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(string id, [FromBody] CreateCommentDto dto)
        {
            var userId = GetUserId();
            var comment = await _postService.AddCommentAsync(id, userId, dto);

            return Ok(comment);
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }
    }
}