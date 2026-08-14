using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs
{
    public record CreatCommunityDto(
        [Required(ErrorMessage = "معرف المالك مطلوب")]
        string OwnerId,
        [Required(ErrorMessage = "اسم المجتمع مطلوب")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "يجب أن يكون الاسم بين 3 و 50 حرف")]
        string Name,
        [StringLength(200, ErrorMessage = "يجب أن لا يزيد الوصف عن 200 حرف")]
        string? Description,
        string? ImageUrl,
        bool IsPrivate
    );

    public record JoinCommunityDto(
        string CommunityId,
        string UserId
    );
    public record CommunityAdminDto(
        string CommunityId,
        string UserId
    );
}