using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services;

public class UserPhysicalDataService : IUserPhysicalDataService
{
    private readonly IMongoCollection<UserPhysicalData> _physicalData;
    private readonly IMongoCollection<TrainingRequest> _trainingRequests;
    private readonly IMongoCollection<UserExercise> _userExercises;
    private readonly IMongoCollection<Exercise> _exercises;

    public UserPhysicalDataService(IMongoDatabase database)
    {
        _physicalData = database.GetCollection<UserPhysicalData>("UserPhysicalData");
        _trainingRequests = database.GetCollection<TrainingRequest>("TrainingRequests");
        _userExercises = database.GetCollection<UserExercise>("UserExercises");
        _exercises = database.GetCollection<Exercise>("Exercises");
        _physicalData.Indexes.CreateOne(new CreateIndexModel<UserPhysicalData>(
            Builders<UserPhysicalData>.IndexKeys.Ascending(data => data.UserId),
            new CreateIndexOptions { Unique = true }));
    }

    public async Task<UserPhysicalDataResponseDto> GetAsync(string requesterId, string userId)
    {
        var data = await GetOrCreateAsync(userId);
        var canViewPrivate = requesterId == userId || await IsApprovedCoachAsync(requesterId, userId);
        return await MapAsync(data, canViewPrivate);
    }

    public async Task<UserPhysicalDataResponseDto> UpdateAsync(string requesterId, string userId, UpdatePhysicalDataDto dto)
    {
        EnsureOwner(requesterId, userId);
        if (dto.Birthday.HasValue)
            dto = dto with { Birthday = dto.Birthday.Value.Date };
        var data = await GetOrCreateAsync(userId);
        data.HeightCm = dto.HeightCm;
        data.Sex = dto.Sex;
        data.Birthday = dto.Birthday;
        data.HeightIsPrivate = dto.HeightIsPrivate;
        data.WeightsIsPrivate = dto.WeightsIsPrivate;
        data.SexIsPrivate = dto.SexIsPrivate;
        data.BirthdayIsPrivate = dto.BirthdayIsPrivate;
        data.PersonalRecordsIsPrivate = dto.PersonalRecordsIsPrivate;
        await _physicalData.ReplaceOneAsync(item => item.Id == data.Id, data);
        return await MapAsync(data, true);
    }

    public async Task<UserPhysicalDataResponseDto> AddWeightAsync(string requesterId, string userId, AddWeightDto dto)
    {
        EnsureOwner(requesterId, userId);
        var data = await GetOrCreateAsync(userId);
        data.Weights.Add(new WeightEntry { WeightKg = dto.WeightKg, AddedAt = DateTime.UtcNow });
        await _physicalData.ReplaceOneAsync(item => item.Id == data.Id, data);
        return await MapAsync(data, true);
    }

    public async Task UpdatePersonalRecordsAsync(string userId, IEnumerable<UserExercise> exercises)
    {
        await RecalculatePersonalRecordsAsync(userId, exercises.Select(exercise => exercise.ExerciseId));
    }

    public async Task RecalculatePersonalRecordsAsync(string userId, IEnumerable<string> exerciseIdsInput)
    {
        var exerciseIds = exerciseIdsInput.Distinct().ToList();
        if (exerciseIds.Count == 0)
            return;

        var data = await GetOrCreateAsync(userId);
        var records = data.PersonalRecords.Where(record => !exerciseIds.Contains(record.ExerciseId)).ToList();
        foreach (var exerciseId in exerciseIds)
        {
            var best = await GetBestExerciseAsync(userId, exerciseId);
            if (best != null)
                records.Add(new PersonalRecord { ExerciseId = exerciseId, MaxWeightKg = best.Weight, Date = best.RecordedAt });
        }
        data.PersonalRecords = records;
        await _physicalData.ReplaceOneAsync(item => item.Id == data.Id, data);
    }

    private async Task<UserPhysicalData> GetOrCreateAsync(string userId)
    {
        var data = await _physicalData.Find(item => item.UserId == userId).FirstOrDefaultAsync();
        if (data != null)
            return data;

        data = new UserPhysicalData { UserId = userId };
        try
        {
            await _physicalData.InsertOneAsync(data);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            data = await _physicalData.Find(item => item.UserId == userId).FirstAsync();
        }
        return data;
    }

    private async Task<UserExercise?> GetBestExerciseAsync(string userId, string exerciseId)
    {
        return await _exerciseAttempts(userId, exerciseId)
            .SortByDescending(exercise => exercise.Weight)
            .ThenByDescending(exercise => exercise.RecordedAt)
            .FirstOrDefaultAsync();
    }

    private IFindFluent<UserExercise, UserExercise> _exerciseAttempts(string userId, string exerciseId) =>
        _userExercises.Find(exercise => exercise.UserId == userId && exercise.ExerciseId == exerciseId);

    private async Task<bool> IsApprovedCoachAsync(string coachId, string athleteId) =>
        await _trainingRequests.Find(request => request.CoachId == coachId && request.AthleteId == athleteId && request.Status == TrainingRequestStatus.Approved).AnyAsync();

    private static void EnsureOwner(string requesterId, string userId)
    {
        if (requesterId != userId)
            throw new UnauthorizedAccessException("Only the user can edit physical data.");
    }

    private async Task<UserPhysicalDataResponseDto> MapAsync(UserPhysicalData data, bool canViewPrivate)
    {
        var exerciseIds = data.PersonalRecords.Select(record => record.ExerciseId).ToList();
        var exercises = await _exercises.Find(exercise => exerciseIds.Contains(exercise.Id!)).ToListAsync();
        var names = exercises.ToDictionary(exercise => exercise.Id!, exercise => exercise.Name);
        return new UserPhysicalDataResponseDto(
            data.UserId,
            canViewPrivate || !data.HeightIsPrivate ? data.HeightCm : null,
            data.HeightIsPrivate,
            canViewPrivate || !data.WeightsIsPrivate ? data.Weights.Select(weight => new WeightEntryDto(weight.WeightKg, weight.AddedAt)).ToList() : null,
            data.WeightsIsPrivate,
            canViewPrivate || !data.SexIsPrivate ? data.Sex : null,
            data.SexIsPrivate,
            canViewPrivate || !data.BirthdayIsPrivate ? data.Birthday : null,
            data.BirthdayIsPrivate,
            canViewPrivate || !data.PersonalRecordsIsPrivate ? data.PersonalRecords.Select(record => new PersonalRecordDto(record.ExerciseId, names.GetValueOrDefault(record.ExerciseId, "Unknown exercise"), record.MaxWeightKg, record.Date)).ToList() : null,
            data.PersonalRecordsIsPrivate);
    }
}