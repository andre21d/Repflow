using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface IChatService
    {
        Task<MessageResponseDto> SendMessageAsync(string senderId, SendMessageDto dto);
        Task<List<MessageResponseDto>> GetChatHistoryAsync(string userId, string otherUserId);
        Task<bool> MarkAsReadAsync(string userId, string messageId);
    }
}