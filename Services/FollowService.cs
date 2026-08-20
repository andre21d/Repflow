using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class FollowService : IFollowService
    {
        private readonly IMongoCollection<Follow> _follows;
        private readonly IMongoCollection<User> _users;
        private readonly INotificationService? _notificationService;

        public FollowService(IMongoDatabase database)
        {
            _follows = database.GetCollection<Follow>("Follows");
            _users = database.GetCollection<User>("Users");
        }

        public FollowService(IMongoDatabase database, INotificationService notificationService) 
            : this(database)
        {
            _notificationService = notificationService;
        }

        public async Task<FollowStatusDto> ToggleFollowAsync(string currentUserId, string targetUserId)
        {
            if (currentUserId == targetUserId)
            {
                throw new InvalidOperationException("You cannot follow yourself.");
            }

            var existingFollow = await _follows
                .Find(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId)
                .FirstOrDefaultAsync();

            if (existingFollow == null)
            {
                var targetUser = await _users.Find(u => u.Id == targetUserId).FirstOrDefaultAsync();
                bool isPrivate = targetUser?.IsPrivate ?? false;

                string followStatus = isPrivate ? "Pending" : "Accepted";
                string notificationContent = isPrivate ? "Requested to follow you" : "Started following you";

                var follow = new Follow
                {
                    FollowerId = currentUserId,
                    FollowingId = targetUserId,
                    Status = followStatus
                };
                await _follows.InsertOneAsync(follow);

                if (_notificationService != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        recipientUserId: targetUserId,
                        triggeredById: currentUserId,
                        type: "FollowRequest",
                        targetId: currentUserId,
                        content: notificationContent
                    );
                }

                string responseMsg = isPrivate ? "Follow request sent." : "User followed successfully.";
                return new FollowStatusDto(true, responseMsg);
            }
            else
            {
                await _follows.DeleteOneAsync(f => f.Id == existingFollow.Id);
                return new FollowStatusDto(false, "Follow removed/canceled.");
            }
        }

        public async Task<bool> AcceptFollowRequestAsync(string followerId, string currentUserId)
        {
            var filter = Builders<Follow>.Filter.Where(f => f.FollowerId == followerId && f.FollowingId == currentUserId && f.Status == "Pending");
            var update = Builders<Follow>.Update.Set(f => f.Status, "Accepted");

            var result = await _follows.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                if (_notificationService != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        recipientUserId: followerId,
                        triggeredById: currentUserId,
                        type: "FollowAccepted",
                        targetId: currentUserId,
                        content: "Accepted your Follow Request"
                    );
                }
                return true;
            }

            return false;
        }

        public async Task<List<string>> GetFollowingUserIdsAsync(string userId)
        {
            return await _follows
                .Find(f => f.FollowerId == userId && f.Status == "Accepted")
                .Project(f => f.FollowingId)
                .ToListAsync();
        }

        public async Task<List<string>> GetFollowerUserIdsAsync(string userId)
        {
            return await _follows
                .Find(f => f.FollowingId == userId && f.Status == "Accepted")
                .Project(f => f.FollowerId)
                .ToListAsync();
        }
    }
}