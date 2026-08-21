namespace Repflow.Api.DTOs;

public record SubmitCoachApplicationDto(string CertificationUrl);

public record ReviewCoachApplicationDto(bool Approved, string? ReviewNote);

public record CreateTrainingRequestDto(string CoachId, string? Message);

public record ReviewTrainingRequestDto(bool Approved);

public record RateCoachDto(int Rating);

public record CoachResponseDto(
    string UserId,
    string Username,
    string? Bio,
    string? ProfilePictureUrl,
    string CertificationUrl,
    DateTime ApprovedAt,
    double AverageRating,
    int TotalParticipants
);

public record CoachApplicationResponseDto(
    string? Id,
    string UserId,
    string CertificationUrl,
    string Status,
    string? ReviewNote,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? CoachName = null
);

public record TrainingRequestResponseDto(
    string? Id,
    string AthleteId,
    string CoachId,
    string? Message,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt
);