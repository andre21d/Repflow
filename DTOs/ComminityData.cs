using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs
{
    public record CommunityRequestesResponseDto(
        string Id,
        string CommunityId,
        string UserId,
        string Username,
        string? ImageUrl
    );
    public record CommunityResponseDto(
        string Id,
        string Name,
        string? Description,
        string? ImageUrl,
        bool IsPrivate,
        string OwnerId,
        bool IsOwner,
        bool IsAdmin,
        bool IsMember,
        List<string>? AdminIds,
        int MemberCount = 1
    );
    public record CreateCommunityDto(
        [Required(ErrorMessage = "اسم المجتمع مطلوب")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "يجب أن يكون الاسم بين 3 و 50 حرف")]
        string Name,
        [StringLength(200, ErrorMessage = "يجب أن لا يزيد الوصف عن 200 حرف")]
        string? Description,
        string? ImageUrl,
        bool IsPrivate
    );
    public record CommunityMemberResponseDto(
        string UserId,
        string UserName,
        bool IsAdmin
    );
   
}