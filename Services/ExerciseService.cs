using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly IMongoCollection<Exercise> _exercises;
        private readonly IMongoCollection<User> _users;

        public ExerciseService(IMongoDatabase database)
        {
            _exercises = database.GetCollection<Exercise>("Exercises");
            _users = database.GetCollection<User>("Users");

            var index = new CreateIndexModel<Exercise>(
                Builders<Exercise>.IndexKeys.Ascending(exercise => exercise.Name),
                new CreateIndexOptions { Unique = true });
            try
            {
                _exercises.Indexes.CreateOne(index);
            }
            catch (Exception ex) when (ex.Message.Contains("E11000", StringComparison.Ordinal))
            {
            }
        }

        public async Task<ExerciseResponseDto> CreateAsync(string userId, CreateExerciseDto dto)
        {
            await EnsureAdminAsync(userId);

            if (await _exercises.Find(exercise => exercise.Name == dto.Name).AnyAsync())
                throw new InvalidOperationException("An exercise with this name already exists.");

            var exercise = new Exercise
            {
                Name = dto.Name,
                Description = dto.Description,
                MainMuscle = dto.MainMuscle,
                SecondaryMuscles = dto.SecondaryMuscles
            };

            try
            {
                await _exercises.InsertOneAsync(exercise);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
            {
                throw new InvalidOperationException("An exercise with this name already exists.", ex);
            }

            return Map(exercise);
        }

        public async Task<List<ExerciseResponseDto>> GetAllAsync()
        {
            var exercises = await _exercises.Find(_ => true).SortBy(e => e.Name).ToListAsync();
            return exercises.Select(Map).ToList();
        }

        public async Task<ExerciseResponseDto?> GetByIdAsync(string id)
        {
            var exercise = await _exercises.Find(e => e.Id == id).FirstOrDefaultAsync();
            return exercise == null ? null : Map(exercise);
        }

        public async Task<List<ExerciseResponseDto>> GetByMainMuscleAsync(Repflow.Api.Models.Muscle muscle)
        {
            var exercises = await _exercises.Find(exercise => exercise.MainMuscle == muscle)
                .SortBy(exercise => exercise.Name)
                .ToListAsync();
            return exercises.Select(Map).ToList();
        }

        public async Task<List<ExerciseResponseDto>> GetBySecondaryMuscleAsync(Repflow.Api.Models.Muscle muscle)
        {
            var exercises = await _exercises.Find(exercise => exercise.SecondaryMuscles.Contains(muscle))
                .SortBy(exercise => exercise.Name)
                .ToListAsync();
            return exercises.Select(Map).ToList();
        }

        public async Task<ExerciseResponseDto?> UpdateAsync(string userId, string id, UpdateExerciseDto dto)
        {
            await EnsureAdminAsync(userId);

            var exercise = await _exercises.Find(e => e.Id == id).FirstOrDefaultAsync();
            if (exercise == null)
                return null;

            var duplicateName = await _exercises.Find(e => e.Name == dto.Name && e.Id != id).AnyAsync();
            if (duplicateName)
                throw new InvalidOperationException("An exercise with this name already exists.");

            exercise.Name = dto.Name;
            exercise.Description = dto.Description;
            exercise.MainMuscle = dto.MainMuscle;
            exercise.SecondaryMuscles = dto.SecondaryMuscles;

            try
            {
                await _exercises.ReplaceOneAsync(e => e.Id == id, exercise);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
            {
                throw new InvalidOperationException("An exercise with this name already exists.", ex);
            }

            return Map(exercise);
        }

        public async Task<bool> DeleteAsync(string userId, string id)
        {
            await EnsureAdminAsync(userId);
            var result = await _exercises.DeleteOneAsync(exercise => exercise.Id == id);
            return result.DeletedCount > 0;
        }

        private async Task EnsureAdminAsync(string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (!user.Roles.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                throw new UnauthorizedAccessException("Only an admin can create exercises.");
        }

        private static ExerciseResponseDto Map(Exercise exercise) =>
            new(exercise.Id!, exercise.Name, exercise.Description,
                exercise.MainMuscle, exercise.SecondaryMuscles);
    }
}