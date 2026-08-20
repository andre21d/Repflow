using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IMongoCollection<UserSession> _sessions;
        private readonly IMongoCollection<UserExercise> _userExercises;
        private readonly IMongoCollection<Exercise> _exercises;
        private readonly IUserPhysicalDataService _physicalDataService;

        public UserSessionService(IMongoDatabase database, IUserPhysicalDataService physicalDataService)
        {
            _sessions = database.GetCollection<UserSession>("UserSessions");
            _userExercises = database.GetCollection<UserExercise>("UserExercises");
            _exercises = database.GetCollection<Exercise>("Exercises");
            _physicalDataService = physicalDataService;

            var index = new CreateIndexModel<UserSession>(
                Builders<UserSession>.IndexKeys
                    .Ascending(session => session.UserId)
                    .Ascending(session => session.Date),
                new CreateIndexOptions { Unique = true });
            try
            {
                _sessions.Indexes.CreateOne(index);
            }
            catch (Exception ex) when (ex.Message.Contains("E11000", StringComparison.Ordinal))
            {
            }
        }

        public async Task<UserSessionResponseDto> CreateAsync(string userId, UserSessionInputDto dto)
        {
            ValidateInput(dto);
            var date = DateTime.UtcNow.Date;
            if (await _sessions.Find(session => session.UserId == userId &&
                session.Date >= date && session.Date < date.AddDays(1)).AnyAsync())
                throw new InvalidOperationException("You can only create one session per day.");

            var exerciseDefinitions = await GetExerciseDefinitionsAsync(dto.Exercises);
            var userExercises = dto.Exercises.Select(input => new UserExercise
            {
                UserId = userId,
                ExerciseId = input.ExerciseId,
                Reps = input.Reps,
                Sets = input.Sets,
                Weight = input.Weight,
                RecordedAt = date
            }).ToList();

            await _userExercises.InsertManyAsync(userExercises);
            await RecalculatePrsAsync(userId, userExercises.Select(e => e.ExerciseId));
            await _physicalDataService.UpdatePersonalRecordsAsync(userId, userExercises);

            var session = new UserSession
            {
                UserId = userId,
                Description = dto.Description,
                Muscles = dto.Muscles,
                UserExerciseIds = userExercises.Select(e => e.Id!).ToList(),
                Date = date,
                TotalDurationMinutes = dto.TotalDurationMinutes
            };
            try
            {
                await _sessions.InsertOneAsync(session);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
            {
                throw new InvalidOperationException("You can only create one session per day.", ex);
            }

            return Map(session, userExercises, exerciseDefinitions.Values);
        }

        public async Task<UserSessionResponseDto?> UpdateAsync(string userId, string sessionId, UserSessionInputDto dto)
        {
            var session = await _sessions.Find(s => s.Id == sessionId && s.UserId == userId).FirstOrDefaultAsync();
            if (session == null)
                return null;
            if (DateTime.UtcNow.Date != session.Date.Date)
                throw new InvalidOperationException("A session can only be updated within 24 hours of creation.");

            ValidateInput(dto);
            var exerciseDefinitions = await GetExerciseDefinitionsAsync(dto.Exercises);
            var previousExerciseIds = await _userExercises.Find(e => session.UserExerciseIds.Contains(e.Id!))
                .Project(e => e.ExerciseId)
                .ToListAsync();
            await _userExercises.DeleteManyAsync(e => e.UserId == userId && session.UserExerciseIds.Contains(e.Id!));

            var userExercises = dto.Exercises.Select(input => new UserExercise
            {
                UserId = userId,
                ExerciseId = input.ExerciseId,
                Reps = input.Reps,
                Sets = input.Sets,
                Weight = input.Weight,
                RecordedAt = session.Date
            }).ToList();
            await _userExercises.InsertManyAsync(userExercises);
            await RecalculatePrsAsync(userId, previousExerciseIds.Concat(userExercises.Select(e => e.ExerciseId)));
            await _physicalDataService.RecalculatePersonalRecordsAsync(userId, previousExerciseIds.Concat(userExercises.Select(e => e.ExerciseId)));

            session.Description = dto.Description;
            session.Muscles = dto.Muscles;
            session.UserExerciseIds = userExercises.Select(e => e.Id!).ToList();
            session.TotalDurationMinutes = dto.TotalDurationMinutes;
            await _sessions.ReplaceOneAsync(s => s.Id == sessionId && s.UserId == userId, session);

            return Map(session, userExercises, exerciseDefinitions.Values);
        }

        public Task<List<UserSessionResponseDto>> GetByDayAsync(string userId, DateTime date)
        {
            var start = date.Date;
            return GetSessionsAsync(userId, start, start.AddDays(1));
        }

        public Task<List<UserSessionResponseDto>> GetAllAsync(string userId) =>
            GetSessionsAsync(userId, DateTime.MinValue, DateTime.MaxValue);

        public Task<List<UserSessionResponseDto>> GetByMonthAsync(string userId, int year, int month)
        {
            if (month is < 1 or > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            return GetSessionsAsync(userId, start, start.AddMonths(1));
        }

        private async Task<List<UserSessionResponseDto>> GetSessionsAsync(string userId, DateTime start, DateTime end)
        {
            var sessions = await _sessions.Find(s => s.UserId == userId && s.Date >= start && s.Date < end)
                .SortByDescending(s => s.Date)
                .ToListAsync();
            var ids = sessions.SelectMany(s => s.UserExerciseIds).Distinct().ToList();
            var userExercises = await _userExercises.Find(e => ids.Contains(e.Id!)).ToListAsync();
            var exerciseIds = userExercises.Select(e => e.ExerciseId).Distinct().ToList();
            var definitions = await _exercises.Find(e => exerciseIds.Contains(e.Id!)).ToListAsync();
            var userExercisesById = userExercises.ToDictionary(e => e.Id!);
            var definitionsById = definitions.ToDictionary(e => e.Id!);

            return sessions.Select(session => Map(session,
                session.UserExerciseIds.Where(userExercisesById.ContainsKey).Select(id => userExercisesById[id]).ToList(),
                definitionsById.Values.ToList())).ToList();
        }

        private async Task<Dictionary<string, Exercise>> GetExerciseDefinitionsAsync(List<UserExerciseInputDto> inputs)
        {
            var ids = inputs.Select(input => input.ExerciseId).Distinct().ToList();
            var definitions = await _exercises.Find(exercise => ids.Contains(exercise.Id!)).ToListAsync();
            if (definitions.Count != ids.Count)
                throw new KeyNotFoundException("One or more exercises were not found.");
            return definitions.ToDictionary(exercise => exercise.Id!);
        }

        private async Task RecalculatePrsAsync(string userId, IEnumerable<string> exerciseIds)
        {
            foreach (var exerciseId in exerciseIds.Distinct())
            {
                var attempts = await _userExercises.Find(e => e.UserId == userId && e.ExerciseId == exerciseId)
                    .SortByDescending(e => e.Weight)
                    .ToListAsync();
                var highestWeight = attempts.FirstOrDefault()?.Weight;
                for (var i = 0; i < attempts.Count; i++)
                {
                    var isPr = highestWeight.HasValue && attempts[i].Weight == highestWeight.Value;
                    if (attempts[i].IsPr != isPr)
                    {
                        attempts[i].IsPr = isPr;
                        await _userExercises.ReplaceOneAsync(e => e.Id == attempts[i].Id, attempts[i]);
                    }
                }
            }
        }

        private static UserSessionResponseDto Map(UserSession session, List<UserExercise> userExercises,
            IEnumerable<Exercise> definitions)
        {
            var definitionsById = definitions.ToDictionary(e => e.Id!);
            var exerciseResponses = userExercises
                .Where(userExercise => definitionsById.ContainsKey(userExercise.ExerciseId))
                .Select(userExercise =>
                {
                    var definition = definitionsById[userExercise.ExerciseId];
                    return new UserExerciseResponseDto(userExercise.Id!, definition.Id!, definition.Name,
                        definition.Description, definition.MainMuscle, definition.SecondaryMuscles,
                        userExercise.Reps, userExercise.Sets, userExercise.Weight, userExercise.IsPr);
                }).ToList();

            return new UserSessionResponseDto(session.Id!, session.UserId, session.Description,
                session.Muscles, DateOnly.FromDateTime(session.Date),
                FormatDuration(session.TotalDurationMinutes), exerciseResponses);
        }

        private static void ValidateInput(UserSessionInputDto dto)
        {
            if (dto.Exercises == null || dto.Exercises.Count == 0)
                throw new ArgumentException("A session must contain at least one UserExercise.");

            if (dto.TotalDurationMinutes <= 0)
                throw new ArgumentException("Total duration must be greater than zero.");

            if (dto.Exercises.Any(exercise => exercise.Reps <= 0 || exercise.Sets <= 0 || exercise.Weight <= 0))
                throw new ArgumentException("Reps, sets, and weight must be greater than zero.");
        }

        private static string FormatDuration(int totalDurationMinutes) =>
            $"{totalDurationMinutes / 60}:{totalDurationMinutes % 60:00}";
    }
}