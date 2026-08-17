using Microsoft.AspNetCore.Mvc;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(
            [FromQuery] string? postId, 
            [FromQuery] string? commentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!string.IsNullOrEmpty(commentId))
            {
                var comment = await _commentService.GetCommentByIdAsync(commentId);
                if (comment == null) 
                    return NotFound(new { message = "التعليق غير موجود." });

                return Ok(comment);
            }
            if (!string.IsNullOrEmpty(postId))
            {
                var comments = await _commentService.GetCommentsByPostIdAsync(postId, page, pageSize);
                return Ok(new
                {
                    page,
                    pageSize,
                    count = comments.Count,
                    data = comments
                });
            }

            return BadRequest(new { message = "يرجى تزويد postId أو commentId." });
        }
    }
}