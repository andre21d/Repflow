using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface IWorkoutPlanningService
    {
        Task<WorkoutTemplateResponseDto> CreateTemplateAsync(string userId, CreateWorkoutTemplateDto dto);
        Task<List<WorkoutTemplateResponseDto>> GetTemplatesAsync(string userId);
        Task<WorkoutTemplateResponseDto?> GetTemplateAsync(string userId, string templateId);
        Task ArchiveTemplateAsync(string userId, string templateId);
        Task<WorkoutPlanResponseDto> CreatePlanAsync(string userId, CreateWorkoutPlanDto dto);
        Task<List<WorkoutPlanResponseDto>> GetPlansAsync(string userId);
        Task<WorkoutPlanResponseDto?> GetPlanAsync(string userId, string planId);
        Task SendPlanAsync(string userId, string planId);
        Task AcceptPlanAsync(string userId, string planId, bool accepted);
        Task<WorkoutPlanResponseDto> StartPlanAsync(string userId, string planId, StartWorkoutPlanDto dto);
        Task<WorkoutPlanResponseDto?> CompleteDayAsync(string userId, string planId, string dayId, CompleteWorkoutDayDto dto);
    }
}