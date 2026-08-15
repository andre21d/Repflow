using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface IFollowService
    {
        Task<FollowStatusDto> ToggleFollowAsync(string currentUserId, string targetUserId);
        Task<List<string>> GetFollowingUserIdsAsync(string userId);
        Task<List<string>> GetFollowerUserIdsAsync(string userId);
    }
}