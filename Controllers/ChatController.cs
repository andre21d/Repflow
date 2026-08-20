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
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var userId = GetUserId();
            var message = await _chatService.SendMessageAsync(userId, dto);
            return Ok(message);
        }

        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetHistory(string otherUserId)
        {
            var userId = GetUserId();
            var history = await _chatService.GetChatHistoryAsync(userId, otherUserId);
            return Ok(history);
        }

        [HttpPut("read/{messageId}")]
        public async Task<IActionResult> MarkRead(string messageId)
        {
            var userId = GetUserId();
            var result = await _chatService.MarkAsReadAsync(userId, messageId);
            return result ? Ok() : BadRequest();
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}