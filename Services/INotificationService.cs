using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string recipientUserId, string triggeredById, string type, string targetId, string content);
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId);
        Task<bool> MarkAsReadAsync(string userId, string notificationId);
    }
}