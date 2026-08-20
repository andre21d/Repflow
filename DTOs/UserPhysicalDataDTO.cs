using System.ComponentModel.DataAnnotations;
using Repflow.Api.Models;

namespace Repflow.Api.DTOs;

public record UpdatePhysicalDataDto(
    [Range(double.Epsilon, double.MaxValue)] double? HeightCm,
    Sex? Sex,
    DateTime? Birthday,
    bool HeightIsPrivate,
    bool WeightsIsPrivate,
    bool SexIsPrivate,
    bool BirthdayIsPrivate,
    bool PersonalRecordsIsPrivate
);

public record AddWeightDto(
    [Range(double.Epsilon, double.MaxValue)] double WeightKg
);

public record WeightEntryDto(double WeightKg, DateTime AddedAt);

public record PersonalRecordDto(string ExerciseId, string ExerciseName, double MaxWeightKg, DateTime Date);

public record UserPhysicalDataResponseDto(
    string UserId,
    double? HeightCm,
    bool HeightIsPrivate,
    List<WeightEntryDto>? Weights,
    bool WeightsIsPrivate,
    Sex? Sex,
    bool SexIsPrivate,
    DateTime? Birthday,
    bool BirthdayIsPrivate,
    List<PersonalRecordDto>? PersonalRecords,
    bool PersonalRecordsIsPrivate
);