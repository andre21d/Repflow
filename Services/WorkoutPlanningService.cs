using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class WorkoutPlanningService : IWorkoutPlanningService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Exercise> _exercises;
        private readonly IMongoCollection<TrainingRequest> _trainingRequests;
        private readonly IMongoCollection<WorkoutTemplate> _templates;
        private readonly IMongoCollection<WorkoutPlanDayTemplate> _templateDays;
        private readonly IMongoCollection<WorkoutPlan> _plans;
        private readonly IMongoCollection<WorkoutPlanDay> _planDays;
        private readonly IMongoCollection<UserSession> _sessions;
        private readonly IMongoCollection<UserExercise> _userExercises;
        private readonly IUserPhysicalDataService _physicalDataService;

        public WorkoutPlanningService(IMongoDatabase database, IUserPhysicalDataService physicalDataService)
        {
            _users = database.GetCollection<User>("Users");
            _exercises = database.GetCollection<Exercise>("Exercises");
            _trainingRequests = database.GetCollection<TrainingRequest>("TrainingRequests");
            _templates = database.GetCollection<WorkoutTemplate>("WorkoutTemplates");
            _templateDays = database.GetCollection<WorkoutPlanDayTemplate>("WorkoutPlanDayTemplates");
            _plans = database.GetCollection<WorkoutPlan>("WorkoutPlans");
            _planDays = database.GetCollection<WorkoutPlanDay>("WorkoutPlanDays");
            _sessions = database.GetCollection<UserSession>("UserSessions");
            _userExercises = database.GetCollection<UserExercise>("UserExercises");
            _physicalDataService = physicalDataService;
        }

        public async Task<WorkoutTemplateResponseDto> CreateTemplateAsync(string userId, CreateWorkoutTemplateDto dto)
        {
            if (dto.IsGeneral)
                await EnsureAdminAsync(userId);
            var days = await BuildTemplateDaysAsync(userId, dto.Days);
            if (days.Count > dto.DurationDays)
                throw new ArgumentException("Template days cannot exceed its duration.");

            var template = new WorkoutTemplate { UserId = userId, Name = dto.Name, DurationDays = dto.DurationDays, IsGeneral = dto.IsGeneral };
            await _templates.InsertOneAsync(template);
            foreach (var day in days)
                day.WorkoutTemplateId = template.Id!;
            if (days.Count > 0)
                await _templateDays.InsertManyAsync(days);
            return new WorkoutTemplateResponseDto(template.Id!, template.UserId, template.Name, template.DurationDays, template.IsGeneral, days);
        }

        public async Task<List<WorkoutTemplateResponseDto>> GetTemplatesAsync(string userId)
        {
            var templates = await _templates.Find(t => !t.IsArchived && (t.IsGeneral || t.UserId == userId)).ToListAsync();
            return await Task.WhenAll(templates.Select(t => MapTemplateAsync(t))) is var mapped
                ? mapped.ToList()
                : new List<WorkoutTemplateResponseDto>();
        }

        public async Task<WorkoutTemplateResponseDto?> GetTemplateAsync(string userId, string templateId)
        {
            var template = await _templates.Find(t => t.Id == templateId && !t.IsArchived && (t.IsGeneral || t.UserId == userId)).FirstOrDefaultAsync();
            return template == null ? null : await MapTemplateAsync(template);
        }

        public async Task ArchiveTemplateAsync(string userId, string templateId)
        {
            var template = await _templates.Find(t => t.Id == templateId && t.UserId == userId).FirstOrDefaultAsync();
            if (template == null)
                throw new KeyNotFoundException("Template not found.");
            if (template.IsGeneral)
                await EnsureAdminAsync(userId);
            template.IsArchived = true;
            await _templates.ReplaceOneAsync(t => t.Id == templateId, template);
        }

        public async Task<WorkoutPlanResponseDto> CreatePlanAsync(string userId, CreateWorkoutPlanDto dto)
        {
            var ownerId = string.IsNullOrWhiteSpace(dto.OwnerUserId) ? userId : dto.OwnerUserId;
            string? coachId = null;
            var status = WorkoutPlanStatus.Draft;
            if (ownerId != userId)
            {
                await EnsureCoachParticipantAsync(userId, ownerId);
                coachId = userId;
                status = WorkoutPlanStatus.PendingAcceptance;
            }

            var days = new List<WorkoutPlanDay>();
            var order = 1;
            foreach (var templateId in dto.TemplateIds ?? new List<string>())
            {
                var template = await _templates.Find(t => t.Id == templateId && !t.IsArchived && (t.IsGeneral || t.UserId == userId)).FirstOrDefaultAsync();
                if (template == null)
                    throw new KeyNotFoundException("Template not found.");
                var templateDays = await _templateDays.Find(d => d.WorkoutTemplateId == templateId).SortBy(d => d.Order).ToListAsync();
                foreach (var templateDay in templateDays)
                    days.Add(ToPlanDay(templateDay, ownerId, order++));
            }

            if (dto.Days != null)
            {
                var manualDays = await BuildManualDaysAsync(ownerId, dto.Days, order);
                days.AddRange(manualDays);
            }
            if (days.Count == 0)
                throw new ArgumentException("A plan must contain at least one day.");
            if (days.Count > dto.DurationDays)
                throw new ArgumentException("Plan days cannot exceed its duration.");

            var plan = new WorkoutPlan { OwnerUserId = ownerId, CreatedByUserId = userId, CoachId = coachId, Name = dto.Name, DurationDays = dto.DurationDays, Status = status };
            await _plans.InsertOneAsync(plan);
            foreach (var day in days)
                day.WorkoutPlanId = plan.Id!;
            await _planDays.InsertManyAsync(days);
            return new WorkoutPlanResponseDto(plan, days);
        }

        public async Task<List<WorkoutPlanResponseDto>> GetPlansAsync(string userId)
        {
            var plans = await _plans.Find(p => !p.IsArchived && (p.OwnerUserId == userId || p.CreatedByUserId == userId)).SortByDescending(p => p.CreatedAt).ToListAsync();
            var result = new List<WorkoutPlanResponseDto>();
            foreach (var plan in plans)
                result.Add(await MapPlanAsync(plan));
            return result;
        }

        public async Task<WorkoutPlanResponseDto?> GetPlanAsync(string userId, string planId)
        {
            var plan = await _plans.Find(p => p.Id == planId && !p.IsArchived && (p.OwnerUserId == userId || p.CreatedByUserId == userId)).FirstOrDefaultAsync();
            return plan == null ? null : await MapPlanAsync(plan);
        }

        public async Task SendPlanAsync(string userId, string planId)
        {
            var plan = await GetOwnedPlanAsync(userId, planId);
            if (plan.CreatedByUserId != userId || plan.CoachId != userId || plan.Status != WorkoutPlanStatus.PendingAcceptance)
                throw new UnauthorizedAccessException("Only the assigned coach can send this plan.");
            await _plans.UpdateOneAsync(p => p.Id == planId, Builders<WorkoutPlan>.Update.Set(p => p.Status, WorkoutPlanStatus.PendingAcceptance));
        }

        public async Task AcceptPlanAsync(string userId, string planId, bool accepted)
        {
            var plan = await _plans.Find(p => p.Id == planId && p.OwnerUserId == userId).FirstOrDefaultAsync();
            if (plan == null)
                throw new KeyNotFoundException("Plan not found.");
            if (plan.Status != WorkoutPlanStatus.PendingAcceptance)
                throw new InvalidOperationException("This plan is not waiting for acceptance.");
            plan.Status = accepted ? WorkoutPlanStatus.Accepted : WorkoutPlanStatus.Rejected;
            await _plans.ReplaceOneAsync(p => p.Id == planId, plan);
        }

        public async Task<WorkoutPlanResponseDto> StartPlanAsync(string userId, string planId, StartWorkoutPlanDto dto)
        {
            var plan = await _plans.Find(p => p.Id == planId && p.OwnerUserId == userId).FirstOrDefaultAsync();
            if (plan == null)
                throw new KeyNotFoundException("Plan not found.");
            if (plan.Status != WorkoutPlanStatus.Draft && plan.Status != WorkoutPlanStatus.Accepted)
                throw new InvalidOperationException("This plan cannot be started in its current status.");
            var start = dto.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var days = await _planDays.Find(d => d.WorkoutPlanId == planId).SortBy(d => d.Order).ToListAsync();
            foreach (var day in days)
            {
                day.Date = start.AddDays(day.Order - 1);
                await _planDays.ReplaceOneAsync(d => d.Id == day.Id, day);
            }
            plan.StartDate = start;
            plan.Status = WorkoutPlanStatus.Active;
            await _plans.ReplaceOneAsync(p => p.Id == planId, plan);
            return new WorkoutPlanResponseDto(plan, days);
        }

        public async Task<WorkoutPlanResponseDto?> CompleteDayAsync(string userId, string planId, string dayId, CompleteWorkoutDayDto dto)
        {
            var plan = await _plans.Find(p => p.Id == planId && p.OwnerUserId == userId).FirstOrDefaultAsync();
            var day = await _planDays.Find(d => d.Id == dayId && d.WorkoutPlanId == planId && d.UserId == userId).FirstOrDefaultAsync();
            if (plan == null || day == null)
                return null;
            if (day.Completed)
                return await MapPlanAsync(plan);
            if (plan.Status != WorkoutPlanStatus.Active || day.Date?.Date != DateTime.UtcNow.Date)
                throw new InvalidOperationException("Only today's active plan day can be completed.");
            if (day.IsRestDay)
            {
                day.Completed = true;
                await _planDays.ReplaceOneAsync(d => d.Id == day.Id, day);
                return await MapPlanAsync(plan);
            }
            if (dto.Exercises == null || dto.Exercises.Count == 0 || dto.TotalDurationMinutes <= 0)
                throw new ArgumentException("A completed workout needs exercises and a positive duration.");
            var definitions = await GetDefinitionsAsync(dto.Exercises.Select(e => e.ExerciseId));
            var actual = dto.Exercises.Select(input => new UserExercise { UserId = userId, ExerciseId = input.ExerciseId, Reps = input.Reps, Sets = input.Sets, Weight = input.Weight, RecordedAt = day.Date!.Value.Date }).ToList();
            if (actual.Any(e => e.Reps <= 0 || e.Sets <= 0 || e.Weight <= 0))
                throw new ArgumentException("Reps, sets, and weight must be greater than zero.");
            await _userExercises.InsertManyAsync(actual);
            await RecalculatePrsAsync(userId, actual.Select(e => e.ExerciseId));
            await _physicalDataService.UpdatePersonalRecordsAsync(userId, actual);
            var session = new UserSession { UserId = userId, Date = day.Date.Value.Date, Description = dto.Description, Muscles = dto.Muscles ?? new List<Muscle>(), UserExerciseIds = actual.Select(e => e.Id!).ToList(), TotalDurationMinutes = dto.TotalDurationMinutes };
            try { await _sessions.InsertOneAsync(session); }
            catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000) { throw new InvalidOperationException("A session already exists for this day.", ex); }
            day.Completed = true;
            day.UserSessionId = session.Id;
            await _planDays.ReplaceOneAsync(d => d.Id == day.Id, day);
            return await MapPlanAsync(plan);
        }

        private async Task<WorkoutPlan> GetOwnedPlanAsync(string userId, string planId) =>
            await _plans.Find(p => p.Id == planId && p.CreatedByUserId == userId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Plan not found.");

        private async Task EnsureAdminAsync(string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) throw new KeyNotFoundException("User not found.");
            if (!user.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase))) throw new UnauthorizedAccessException("Only an admin can manage general templates.");
        }

        private async Task EnsureCoachParticipantAsync(string coachId, string participantId)
        {
            var coach = await _users.Find(u => u.Id == coachId).FirstOrDefaultAsync();
            if (coach == null || !coach.Roles.Any(r => r.Equals("Coach", StringComparison.OrdinalIgnoreCase))) throw new UnauthorizedAccessException("Only approved coaches can create participant plans.");
            var approved = await _trainingRequests.Find(r => r.CoachId == coachId && r.AthleteId == participantId && r.Status == TrainingRequestStatus.Approved).AnyAsync();
            if (!approved) throw new UnauthorizedAccessException("The participant has not approved this coach relationship.");
        }

        private async Task<List<WorkoutPlanDayTemplate>> BuildTemplateDaysAsync(string userId, List<TemplateDayInputDto> inputs)
        {
            var result = new List<WorkoutPlanDayTemplate>();
            for (var i = 0; i < inputs.Count; i++)
                result.Add(new WorkoutPlanDayTemplate { UserId = userId, Order = i + 1, Name = inputs[i].Name, IsRestDay = inputs[i].IsRestDay, Exercises = await BuildExercisesAsync(inputs[i].Exercises) });
            return result;
        }

        private async Task<List<WorkoutPlanDay>> BuildManualDaysAsync(string userId, List<ManualPlanDayInputDto> inputs, int startOrder)
        {
            var result = new List<WorkoutPlanDay>();
            for (var i = 0; i < inputs.Count; i++)
                result.Add(new WorkoutPlanDay { UserId = userId, Order = startOrder + i, Name = inputs[i].Name, IsRestDay = inputs[i].IsRestDay, Exercises = await BuildExercisesAsync(inputs[i].Exercises) });
            return result;
        }

        private async Task<List<PlannedExercise>> BuildExercisesAsync(List<PlannedExerciseInputDto>? inputs)
        {
            if (inputs == null || inputs.Count == 0) return new List<PlannedExercise>();
            var definitions = await GetDefinitionsAsync(inputs.Select(i => i.ExerciseId));
            return inputs.Select((input, index) => new PlannedExercise { ExerciseId = input.ExerciseId, ExerciseName = definitions[input.ExerciseId].Name, Order = index + 1, PlannedSets = input.PlannedSets, PlannedReps = input.PlannedReps, PlannedWeight = input.PlannedWeight }).ToList();
        }

        private async Task<Dictionary<string, Exercise>> GetDefinitionsAsync(IEnumerable<string> ids)
        {
            var distinctIds = ids.Distinct().ToList();
            var definitions = await _exercises.Find(e => distinctIds.Contains(e.Id!)).ToListAsync();
            if (definitions.Count != distinctIds.Count) throw new KeyNotFoundException("One or more exercises were not found.");
            return definitions.ToDictionary(e => e.Id!);
        }

        private async Task<WorkoutTemplateResponseDto> MapTemplateAsync(WorkoutTemplate template)
        {
            var days = await _templateDays.Find(d => d.WorkoutTemplateId == template.Id).SortBy(d => d.Order).ToListAsync();
            return new WorkoutTemplateResponseDto(template.Id!, template.UserId, template.Name, template.DurationDays, template.IsGeneral, days);
        }

        private async Task<WorkoutPlanResponseDto> MapPlanAsync(WorkoutPlan plan)
        {
            var days = await _planDays.Find(d => d.WorkoutPlanId == plan.Id).SortBy(d => d.Order).ToListAsync();
            return new WorkoutPlanResponseDto(plan, days);
        }

        private static WorkoutPlanDay ToPlanDay(WorkoutPlanDayTemplate source, string userId, int order) =>
            new() { UserId = userId, Order = order, Name = source.Name, IsRestDay = source.IsRestDay, Exercises = source.Exercises.Select(e => new PlannedExercise { ExerciseId = e.ExerciseId, ExerciseName = e.ExerciseName, Order = e.Order, PlannedSets = e.PlannedSets, PlannedReps = e.PlannedReps, PlannedWeight = e.PlannedWeight }).ToList() };

        private async Task RecalculatePrsAsync(string userId, IEnumerable<string> exerciseIds)
        {
            foreach (var exerciseId in exerciseIds.Distinct())
            {
                var attempts = await _userExercises.Find(e => e.UserId == userId && e.ExerciseId == exerciseId).SortByDescending(e => e.Weight).ToListAsync();
                var highest = attempts.FirstOrDefault()?.Weight;
                foreach (var attempt in attempts)
                {
                    var isPr = highest.HasValue && attempt.Weight == highest.Value;
                    if (attempt.IsPr != isPr) { attempt.IsPr = isPr; await _userExercises.ReplaceOneAsync(e => e.Id == attempt.Id, attempt); }
                }
            }
        }
    }
}