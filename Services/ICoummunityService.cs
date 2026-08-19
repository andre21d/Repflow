using Repflow.Api.DTOs;
using Repflow.Api.Models;
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
        Task<string> MakeAdminAsync(string communityId, string userId, string ownerId);
        Task<string> RemoveAdminAsync(string communityId, string userId, string ownerId);
        Task<string> RemoveMemberAsync(string communityId, string userId, string adminId);
        Task<List<CommunityMemberResponseDto?>> GetCommunityMembersAsync(string communityId,string userId);
        Task<List<CommunityResponseDto>> GetUserCommunitiesAsync(string userId);
        Task<List<CommunityRequestesResponseDto>> GetPrivateCommunityRequestsAsync(string communityId,string userId);
    }   
}