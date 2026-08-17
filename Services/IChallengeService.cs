using Repflow.Api.DTOs;
namespace Repflow.Api.Services
{
     public interface IChallengeService
    {
        Task<ChallengeResponseDto> CreateChallengeAsync(string userId,string communityId, CreateChallengeDto dto);
        // Task<List<CommunityResponseDto>> GetAllCommunitiesAsync();
        Task<ChallengeResponseDto?> GetChallengeByIdAsync(string id);
        Task<string> JoinChallengeAsync(string communityId, string userId, string challengeId);
        Task<string> updateParticipantAsync(string challengeId, string userId, double goalParticipation);
        Task<List<ChallengeResponseDto>> GetChallengesByCommunityIdAsync(string communityId);
        Task<List<ChallengeResponseDto>> GetActiveChallengesByCommunityId(string communityId);
        Task<List<ChallengeResponseDto>> GetChallengesByUserIdAsync(string userId);
        Task<List<ChallengeResponseDto>> GetChallengesUserCanJoinAsync(string userId);
        }
}
