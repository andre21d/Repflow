using System.ComponentModel.DataAnnotations;
using Repflow.Api.Models;

namespace Repflow.Api.DTOs
{
    public record PlannedExerciseInputDto(
        [Required] string ExerciseId,
        [Range(1, int.MaxValue)] int PlannedSets,
        [Range(1, int.MaxValue)] int PlannedReps,
        [Range(0, double.MaxValue)] double PlannedWeight
    );

    public record TemplateDayInputDto(
        [Required] string Name,
        bool IsRestDay,
        List<PlannedExerciseInputDto>? Exercises
    );

    public record CreateWorkoutTemplateDto(
        [Required, StringLength(100)] string Name,
        [Range(1, 365)] int DurationDays,
        bool IsGeneral,
        [Required, MinLength(1)] List<TemplateDayInputDto> Days
    );

    public record WorkoutTemplateResponseDto(
        string Id,
        string UserId,
        string Name,
        int DurationDays,
        bool IsGeneral,
        List<WorkoutPlanDayTemplate> Days
    );

    public record ManualPlanDayInputDto(
        [Required] string Name,
        bool IsRestDay,
        List<PlannedExerciseInputDto>? Exercises
    );

    public record CreateWorkoutPlanDto(
        [Required, StringLength(100)] string Name,
        [Range(1, 3650)] int DurationDays,
        string? OwnerUserId,
        List<string>? TemplateIds,
        List<ManualPlanDayInputDto>? Days
    );

    public record StartWorkoutPlanDto([Required] DateOnly StartDate);

    public record CompleteWorkoutDayDto(
        [Range(1, int.MaxValue)] int TotalDurationMinutes,
        string? Description,
        List<Muscle>? Muscles,
        [Required, MinLength(1)] List<UserExerciseInputDto> Exercises
    );

    public record WorkoutPlanResponseDto(
        WorkoutPlan Plan,
        List<WorkoutPlanDay> Days
    );
}