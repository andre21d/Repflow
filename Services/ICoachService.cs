using Repflow.Api.DTOs;

namespace Repflow.Api.Services;

public interface ICoachService
{
    Task<List<CoachResponseDto>> GetAllCoachesAsync();
    Task<List<CoachResponseDto>> GetTopRatedCoachesAsync();
    Task<List<CoachResponseDto>> FindCoachesByNameAsync(string name);
    Task<List<CoachResponseDto>> GetParticipantCoachesAsync(string participantId);
    Task<CoachResponseDto> RateCoachAsync(string athleteId, string coachId, RateCoachDto dto);
    Task<CoachApplicationResponseDto> SubmitCoachApplicationAsync(string userId, SubmitCoachApplicationDto dto);
    Task<CoachApplicationResponseDto?> GetMyCoachApplicationAsync(string userId);
    Task<List<CoachApplicationResponseDto>> GetPendingCoachApplicationsAsync(string adminId);
    Task<CoachApplicationResponseDto> ReviewCoachApplicationAsync(string applicationId, string adminId, ReviewCoachApplicationDto dto);
    Task<TrainingRequestResponseDto> CreateTrainingRequestAsync(string athleteId, CreateTrainingRequestDto dto);
    Task<List<TrainingRequestResponseDto>> GetCoachTrainingRequestsAsync(string coachId);
    Task<TrainingRequestResponseDto> ReviewTrainingRequestAsync(string requestId, string coachId, ReviewTrainingRequestDto dto);
}