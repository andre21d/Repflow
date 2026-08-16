using Repflow.Api.DTOs;
namespace Repflow.Api.Services
{
    public interface ICoummunityService
    {
        Task<CommunityResponseDto> CreateCommunityAsync(string userId, CreateCommunityDto dto);
        // Task<List<CommunityResponseDto>> GetAllCommunitiesAsync();
        Task<CommunityResponseDto?> GetCommunityByIdAsync(string id);
        Task<string> JoinCommunityAsync(string communityId, string userId);
        Task<bool> CommunityRequestsAsync(string RequestId,string adminId,bool accepted);
        Task<string> LeaveCommunityAsync(string communityId, string userId);
    }
}