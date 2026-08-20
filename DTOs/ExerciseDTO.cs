using System.ComponentModel.DataAnnotations;
using Repflow.Api.Models;

namespace Repflow.Api.DTOs
{
    public record CreateExerciseDto(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [Required, StringLength(1000, MinimumLength = 2)] string Description,
        Muscle MainMuscle,
        [Required, MinLength(1)] List<Muscle> SecondaryMuscles
    );

    public record UpdateExerciseDto(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [Required, StringLength(1000, MinimumLength = 2)] string Description,
        Muscle MainMuscle,
        [Required, MinLength(1)] List<Muscle> SecondaryMuscles
    );

    public record ExerciseResponseDto(
        string Id,
        string Name,
        string Description,
        Muscle MainMuscle,
        List<Muscle> SecondaryMuscles
    );
}