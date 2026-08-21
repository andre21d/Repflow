using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs;

public record DashboardStatisticsDto(
    long Users,
    long BlockedUsers,
    long Posts,
    long BlockedPosts,
    long Communities,
    long Coaches,
    long PendingCoachApplications,
    long WorkoutSessions,
    long WorkoutPlans
);

public record BlockResourceDto(bool Blocked);

public record CreateAdminDto(
    [Required] [StringLength(20, MinimumLength = 3)] string Username,
    [Required] [EmailAddress] string Email,
    [Required] [StringLength(100, MinimumLength = 6)] string Password
);

public record AdminSummaryDto(string Id, string Username, string Email, DateTime CreatedAt, bool IsBlocked);
