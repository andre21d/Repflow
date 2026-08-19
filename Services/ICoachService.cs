using Repflow.Api.DTOs;

namespace Repflow.Api.Services;

public interface ICoachService
{
    Task<CoachApplicationResponseDto> SubmitCoachApplicationAsync(string userId, SubmitCoachApplicationDto dto);
    Task<CoachApplicationResponseDto?> GetMyCoachApplicationAsync(string userId);
    Task<List<CoachApplicationResponseDto>> GetPendingCoachApplicationsAsync(string adminId);
    Task<CoachApplicationResponseDto> ReviewCoachApplicationAsync(string applicationId, string adminId, ReviewCoachApplicationDto dto);
    Task<TrainingRequestResponseDto> CreateTrainingRequestAsync(string athleteId, CreateTrainingRequestDto dto);
    Task<List<TrainingRequestResponseDto>> GetCoachTrainingRequestsAsync(string coachId);
    Task<TrainingRequestResponseDto> ReviewTrainingRequestAsync(string requestId, string coachId, ReviewTrainingRequestDto dto);
}