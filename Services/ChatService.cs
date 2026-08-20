using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Hubs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<Message> _messages;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<AppHub> _hubContext;

        public ChatService(IMongoDatabase database, INotificationService notificationService, IHubContext<AppHub> hubContext)
        {
            _messages = database.GetCollection<Message>("Messages");
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        public async Task<MessageResponseDto> SendMessageAsync(string senderId, SendMessageDto dto)
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                SentAt = DateTime.UtcNow
            };

            await _messages.InsertOneAsync(message);

            var responseDto = new MessageResponseDto(
                message.Id!,
                message.SenderId,
                message.ReceiverId,
                message.Content,
                message.IsRead,
                message.SentAt
            );

            // 1. إرسال الرسالة لحظياً للمستلم والـ Sender عبر SignalR
            await _hubContext.Clients.Group(dto.ReceiverId).SendAsync("ReceiveMessage", responseDto);
            await _hubContext.Clients.Group(senderId).SendAsync("MessageSent", responseDto);

            // 2. إرسال إشعار للمستلم برسالة جديدة
            await _notificationService.CreateNotificationAsync(
                recipientUserId: dto.ReceiverId,
                triggeredById: senderId,
                type: "Message",
                targetId: message.Id!,
                content: "أرسل لك رسالة جديدة"
            );

            return responseDto;
        }

        public async Task<List<MessageResponseDto>> GetChatHistoryAsync(string userId, string otherUserId)
        {
            var builder = Builders<Message>.Filter;
            var filter = (builder.Eq(m => m.SenderId, userId) & builder.Eq(m => m.ReceiverId, otherUserId)) |
                         (builder.Eq(m => m.SenderId, otherUserId) & builder.Eq(m => m.ReceiverId, userId));

            var messages = await _messages.Find(filter)
                                          .SortBy(m => m.SentAt)
                                          .ToListAsync();

            return messages.Select(m => new MessageResponseDto(
                m.Id!, m.SenderId, m.ReceiverId, m.Content, m.IsRead, m.SentAt
            )).ToList();
        }

        public async Task<bool> MarkAsReadAsync(string userId, string messageId)
        {
            var update = Builders<Message>.Update.Set(m => m.IsRead, true);
            var result = await _messages.UpdateOneAsync(m => m.Id == messageId && m.ReceiverId == userId, update);
            return result.ModifiedCount > 0;
        }
    }
}