using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;
using Repflow.Api.Services;

public class CommunitiesService : ICoummunityService
{
   private readonly IMongoCollection<Community> _communities;
   private readonly IMongoCollection<CommunityMember> _communityMembers;
   private readonly IMongoCollection<PrivateCommunityRequest> _privateCommunityRequests;
   private readonly IMongoCollection<User> _users;

    public CommunitiesService(IMongoDatabase database)
    {
        _communities = database.GetCollection<Community>("Communities");
        _communityMembers = database.GetCollection<CommunityMember>("CommunityMembers");
        _privateCommunityRequests = database.GetCollection<PrivateCommunityRequest>("PrivateCommunityRequests");
        _users=database.GetCollection<User>("Users");
    }

    public async Task<CommunityResponseDto> CreateCommunityAsync(string userId, CreateCommunityDto dto)
    {
            // Fast pre-check — good UX, not a full guarantee on its own
        var existing = await _communities.Find(c => c.Name == dto.Name).AnyAsync();
        if (existing)
            throw new InvalidOperationException($"A community named '{dto.Name}' already exists");


        var community = new Community
        {
            Name = dto.Name,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            IsPrivate = dto.IsPrivate,
            OwnerId = userId,
            AdminIds = new List<string> { userId }
        };
        await _communities.InsertOneAsync(community);
        var member = new CommunityMember
        {
            CommunityId = community.Id!,
            UserId = userId
        };

        
        await _communityMembers.InsertOneAsync(member);

        return MapToResponseDto(community,IsMember: true, IsAdmin: true, IsOwner: true);
    }

    // public async Task<List<CommunityResponseDto>> GetAllCommunitiesAsync()
    // {
    //     var communities = await _communities.Find(_ => true).ToListAsync();
    //     return communities.Select(MapToResponseDto).ToList();
    // }

    public async Task<CommunityResponseDto?> GetCommunityByIdAsync(string id)
    {
        var community = await _communities.Find(c => c.Id == id).FirstOrDefaultAsync();
        return community == null ? null : MapToResponseDto(community);
    }
    

    private static CommunityResponseDto MapToResponseDto(Community community, bool IsMember = true, bool IsAdmin = false, bool IsOwner = false)
    {
        return new CommunityResponseDto(
            Id: community.Id,
            Name: community.Name,
            Description: community.Description,
            ImageUrl: community.ImageUrl,
            IsPrivate: community.IsPrivate,
            OwnerId: community.OwnerId,
            IsOwner: IsOwner,
            IsAdmin: IsAdmin,
            IsMember: IsMember,
            AdminIds: community.AdminIds,
            MemberCount: community.MembersCount
        );
    }

    public async Task<String> JoinCommunityAsync(string communityId, string userId)
    
    {
        Community community = await _communities.Find(c => c.Id == communityId).FirstOrDefaultAsync();
        CommunityMember existingMember = await _communityMembers.Find(m => m.CommunityId == communityId && m.UserId == userId).FirstOrDefaultAsync();
        if (community == null)
        {
            return "Community not found";
        }
        if (existingMember != null)
        {
            return "Already a member";
        }
        if (community.IsPrivate)
        {   PrivateCommunityRequest existingRequest = await _privateCommunityRequests.Find(r => r.CommunityId == communityId && r.UserId == userId).FirstOrDefaultAsync();
            if (existingRequest != null && existingRequest.Status == Requeststatus.Pending)
            {
            return "Request Pending";
            }
           
            else
             {
            
                var PrivateRequest = new PrivateCommunityRequest
                 {
                  CommunityId = communityId,
                    UserId = userId
                }   ;
                await _privateCommunityRequests.InsertOneAsync(PrivateRequest);
                return "Request Sent";
            }
        }
        else{
                var member = new CommunityMember
                {
                     CommunityId = communityId,
                   UserId = userId
                };
             await _communityMembers.InsertOneAsync(member);
        return "Joined";
             }
    

    }

    public async Task<bool> CommunityRequestsAsync(string RequestId, string adminId ,bool accepted)
    {
        PrivateCommunityRequest request = await _privateCommunityRequests.Find(r => r.Id == RequestId).FirstOrDefaultAsync();
        if (request == null)
        {
            throw new InvalidOperationException("Request not found");
        }
       
        Community community = await _communities.Find(c => c.Id == request.CommunityId).FirstOrDefaultAsync();
        var isAdmin = community.AdminIds.Contains(adminId);
    
        if (!isAdmin)
            {
                throw new UnauthorizedAccessException ("You are not authorized to approve/reject this request");
            }
        if (accepted)
        {
            var member = new CommunityMember
            {
                CommunityId = request.CommunityId,
                UserId = request.UserId
            };
            await _communityMembers.InsertOneAsync(member);
            request.Status = Requeststatus.Approved;
        }
        else
        {
            request.Status = Requeststatus.Rejected;
        }
            
        return accepted;
    }

    public Task<string> LeaveCommunityAsync(string communityId, string userId)
    {
       var member = _communityMembers.Find(m => m.CommunityId == communityId && m.UserId == userId).FirstOrDefault();
       var community = _communities.Find(c => c.Id == communityId).FirstOrDefault();

        if (member == null || community == null)
        {
            throw new InvalidOperationException("Community or membership not found");
        }
        _communityMembers.DeleteOne(m => m.Id == member.Id);
        
        if (community.OwnerId == userId)
        {
            _communities.DeleteOne(c => c.Id == communityId);
            _communityMembers.DeleteMany(m => m.CommunityId == communityId);
            return Task.FromResult("Community deleted as you were the owner");
        }
        else
        if (community.AdminIds.Contains(userId))
        {
            community.AdminIds.Remove(userId);
            _communities.ReplaceOne(c => c.Id == communityId, community);
        }
        return Task.FromResult("Left the community successfully");
    }

    public Task<string> MakeAdminAsync(string communityId, string userId, string ownerId)
    {
        var community = _communities.Find(c => c.Id == communityId).FirstOrDefault();
        if (community == null)
        {
            throw new InvalidOperationException("Community not found");
        }
        if (community.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("Only the owner can make someone an admin");
        }
        if (!community.AdminIds.Contains(userId))
        {
            community.AdminIds.Add(userId);
            _communities.UpdateOneAsync(c => c.Id == communityId, Builders<Community>.Update.AddToSet(c => c.AdminIds, userId));
            return Task.FromResult("User promoted to admin successfully");
        }
        else
        {
            return Task.FromResult("User is already an admin");
        }
    }
    public Task<string> RemoveAdminAsync(string communityId, string userId, string ownerId)
    {
        var community = _communities.Find(c => c.Id == communityId).FirstOrDefault();
        if (community == null)
        {
            throw new InvalidOperationException("Community not found");
        }
        if (community.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("Only the owner can remove an admin");
        }
        if (community.AdminIds.Contains(userId))
        {
            community.AdminIds.Remove(userId);
            _communities.UpdateOneAsync(c => c.Id == communityId,Builders<Community>.Update.Pull(c => c.AdminIds, userId));
            return Task.FromResult("User demoted from admin successfully");
        }
        else
        {
            return Task.FromResult("User is not an admin");
        }
    }
    public Task<string> RemoveMemberAsync(string communityId, string userId, string adminId)
    {
        var community = _communities.Find(c => c.Id == communityId).FirstOrDefault();
        if (community == null)
        {
            throw new InvalidOperationException("Community not found");
        }
        if (!community.AdminIds.Contains(adminId) && community.OwnerId != adminId)
        {
            throw new UnauthorizedAccessException("Only an admin or the owner can remove a member");
        }
        var member = _communityMembers.Find(m => m.CommunityId == communityId && m.UserId == userId).FirstOrDefault();
        if (member == null)
        {
            throw new InvalidOperationException("Member not found");
        }
        _communityMembers.DeleteOne(m => m.Id == member.Id);
        if (community.AdminIds.Contains(userId))
        {
            community.AdminIds.Remove(userId);
            _communities.UpdateOneAsync(c => c.Id == communityId, Builders<Community>.Update.Pull(c => c.AdminIds, userId));
        }
        return Task.FromResult("Member removed successfully");
    }

    public Task<List<CommunityMemberResponseDto?>> GetCommunityMembersAsync(string communityId,string userId )
    {
        var members = _communityMembers.Find(m => m.CommunityId == communityId).ToList();
        if(!IsUserMember(communityId, userId))
        {
            throw new UnauthorizedAccessException("You are not a member of this community");
        }
        var memberDtos = new List<CommunityMemberResponseDto?>();
        foreach (var member in members)
        {
            // Assuming you have a method to get user details by userId
            User user = GetUserById(member.UserId); // Implement this method to fetch user details
            if (user != null)
            {
                memberDtos.Add(new CommunityMemberResponseDto(
                    UserId: user.Id,
                    UserName: user.Username,
                    IsAdmin: _communities.Find(c => c.Id == communityId).FirstOrDefault()?.AdminIds.Contains(user.Id) ?? false
                ));
            }
        }
        return Task.FromResult(memberDtos);
    }
     public Task<List<CommunityResponseDto>> GetUserCommunitiesAsync(string userId)
    {
        var memberCommunities = _communityMembers.Find(m => m.UserId == userId).ToList();
        var communityDtos = new List<CommunityResponseDto>();
        foreach (var member in memberCommunities)
        {
            var community = _communities.Find(c => c.Id == member.CommunityId).FirstOrDefault();
            if (community != null)
            {
                bool isAdmin = community.AdminIds.Contains(userId);
                bool isOwner = community.OwnerId == userId;
                communityDtos.Add(MapToResponseDto(community, IsMember: true, IsAdmin: isAdmin, IsOwner: isOwner));
            }
        }
        return Task.FromResult(communityDtos);
    }
    private bool IsUserMember(string communityId, string userId)
    {
        var member = _communityMembers.Find(m => m.CommunityId == communityId && m.UserId == userId).FirstOrDefault();
        return member != null;
    }

    private User GetUserById(string userId)
    {
        // Implement this method to fetch user details from the _users collection
        User user = _users.Find(u => u.Id == userId).FirstOrDefault();
        return user;
    }

   
}