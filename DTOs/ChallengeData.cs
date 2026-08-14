using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs
{
    public record ChallengeDto(
        
        [Required(ErrorMessage = "اسم التحدي مطلوب")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "يجب أن يكون الاسم بين 3 و 50 حرف")]
        string Name,
        [StringLength(200, ErrorMessage = "يجب أن لا يزيد الوصف عن 200 حرف")]
        string? Description,
        [Required(ErrorMessage = "معرف المالك مطلوب")]
        string OwnerId,
        [Required(ErrorMessage = "تاريخ البدء مطلوب")]
        DateTime StartDate,
        [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
        DateTime EndDate,
        [Required(ErrorMessage = "هدف التحدي مطلوب")]
        int goal
    );

    public record ChallengeParticipantDto(
        string CommunityId,
        string ChallengeId,
        string UserId,
        double? GoalParticipation
    );
}