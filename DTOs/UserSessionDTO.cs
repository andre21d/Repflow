using System.ComponentModel.DataAnnotations;
using Repflow.Api.Models;

namespace Repflow.Api.DTOs
{
    public record UserExerciseInputDto(
        [Required] string ExerciseId,
        [Range(1, int.MaxValue)] int Reps,
        [Range(1, int.MaxValue)] int Sets,
        [Range(double.Epsilon, double.MaxValue)] double Weight
    );

    public record UserSessionInputDto(
        [StringLength(1000)] string? Description,
        List<Muscle> Muscles,
        [Range(1, int.MaxValue)] int TotalDurationMinutes,
        [Required, MinLength(1)] List<UserExerciseInputDto> Exercises
    );

    public record UserExerciseResponseDto(
        string Id,
        string ExerciseId,
        string ExerciseName,
        string ExerciseDescription,
        Muscle MainMuscle,
        List<Muscle> SecondaryMuscles,
        int Reps,
        int Sets,
        double Weight,
        bool IsPr
    );

    public record UserSessionResponseDto(
        string Id,
        string UserId,
        string? Description,
        List<Muscle> Muscles,
        DateOnly Date,
        string TotalDuration,
        List<UserExerciseResponseDto> Exercises
    );
}