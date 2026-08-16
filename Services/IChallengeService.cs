using Repflow.Api.DTOs;
namespace Repflow.Api.Services
{
     public interface IChallengeService
    {
        Task<CommunityResponseDto> CreateChallengeAsync(string userId, CreateCommunityDto dto);
        // Task<List<CommunityResponseDto>> GetAllCommunitiesAsync();
        Task<CommunityResponseDto?> GetChallengeByIdAsync(string id);
        Task<string> JoinChallengeAsync(string communityId, string userId);
        Task<string> updateParticipant(string challengeId, string userId, double goalParticipation);
        }
}
