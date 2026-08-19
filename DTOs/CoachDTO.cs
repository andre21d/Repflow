namespace Repflow.Api.DTOs;

public record SubmitCoachApplicationDto(string CertificationUrl);

public record ReviewCoachApplicationDto(bool Approved, string? ReviewNote);

public record CreateTrainingRequestDto(string CoachId, string? Message);

public record ReviewTrainingRequestDto(bool Approved);

public record CoachApplicationResponseDto(
    string? Id,
    string UserId,
    string CertificationUrl,
    string Status,
    string? ReviewNote,
    DateTime SubmittedAt,
    DateTime? ReviewedAt
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