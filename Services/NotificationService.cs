using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Hubs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IMongoCollection<Notification> _notifications;
        private readonly IHubContext<AppHub> _hubContext;

        public NotificationService(IMongoDatabase database, IHubContext<AppHub> hubContext)
        {
            _notifications = database.GetCollection<Notification>("Notifications");
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(string recipientUserId, string triggeredById, string type, string targetId, string content)
        {
            // عدم إرسال إشعار إذا كان الشخص يتفاعل مع نفسه
            if (recipientUserId == triggeredById) return;

            var notification = new Notification
            {
                UserId = recipientUserId,
                TriggeredById = triggeredById,
                Type = type,
                TargetId = targetId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            await _notifications.InsertOneAsync(notification);

            var dto = new NotificationResponseDto(
                notification.Id!,
                notification.TriggeredById,
                notification.Type,
                notification.TargetId,
                notification.Content,
                notification.IsRead,
                notification.CreatedAt
            );

            // إرسال الإشعار لحظياً عبر SignalR
            await _hubContext.Clients.Group(recipientUserId).SendAsync("ReceiveNotification", dto);
        }

        public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId)
        {
            var notifications = await _notifications.Find(n => n.UserId == userId)
                                                     .SortByDescending(n => n.CreatedAt)
                                                     .ToListAsync();

            return notifications.Select(n => new NotificationResponseDto(
                n.Id!, n.TriggeredById, n.Type, n.TargetId, n.Content, n.IsRead, n.CreatedAt
            )).ToList();
        }

        public async Task<bool> MarkAsReadAsync(string userId, string notificationId)
        {
            var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
            var result = await _notifications.UpdateOneAsync(n => n.Id == notificationId && n.UserId == userId, update);
            return result.ModifiedCount > 0;
        }
    }
}