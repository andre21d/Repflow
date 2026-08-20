using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public interface IExerciseService
    {
        Task<ExerciseResponseDto> CreateAsync(string userId, CreateExerciseDto dto);
        Task<List<ExerciseResponseDto>> GetAllAsync();
        Task<ExerciseResponseDto?> GetByIdAsync(string id);
        Task<List<ExerciseResponseDto>> GetByMainMuscleAsync(Repflow.Api.Models.Muscle muscle);
        Task<List<ExerciseResponseDto>> GetBySecondaryMuscleAsync(Repflow.Api.Models.Muscle muscle);
        Task<ExerciseResponseDto?> UpdateAsync(string userId, string id, UpdateExerciseDto dto);
        Task<bool> DeleteAsync(string userId, string id);
    }
}