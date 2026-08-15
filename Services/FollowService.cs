using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class FollowService : IFollowService
    {
        private readonly IMongoCollection<Follow> _follows;
        private readonly IMongoCollection<User> _users;

        public FollowService(IMongoDatabase database)
        {
            _follows = database.GetCollection<Follow>("Follows");
            _users = database.GetCollection<User>("Users");
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
                var follow = new Follow
                {
                    FollowerId = currentUserId,
                    FollowingId = targetUserId,
                    Status = "Accepted"
                };
                await _follows.InsertOneAsync(follow);

                return new FollowStatusDto(true, "User followed successfully.");
            }
            else
            {
                await _follows.DeleteOneAsync(f => f.Id == existingFollow.Id);

                return new FollowStatusDto(false, "User unfollowed successfully.");
            }
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