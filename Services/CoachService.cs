using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services;

public class CoachService : ICoachService
{
    private readonly IMongoCollection<CoachApplication> _applications;
    private readonly IMongoCollection<Coach> _coaches;
    private readonly IMongoCollection<CoachRating> _coachRatings;
    private readonly IMongoCollection<TrainingRequest> _trainingRequests;
    private readonly IMongoCollection<User> _users;

    public CoachService(IMongoDatabase database)
    {
        _applications = database.GetCollection<CoachApplication>("CoachApplications");
        _coaches = database.GetCollection<Coach>("Coaches");
        _coachRatings = database.GetCollection<CoachRating>("CoachRatings");
        _trainingRequests = database.GetCollection<TrainingRequest>("TrainingRequests");
        _users = database.GetCollection<User>("Users");

        _coachRatings.Indexes.CreateOne(new CreateIndexModel<CoachRating>(
            Builders<CoachRating>.IndexKeys
                .Ascending(rating => rating.CoachId)
                .Ascending(rating => rating.AthleteId),
            new CreateIndexOptions { Unique = true }));
    }

    public async Task<List<CoachResponseDto>> GetAllCoachesAsync()
    {
        var coaches = await _coaches.Find(Builders<Coach>.Filter.Empty)
            .SortByDescending(coach => coach.ApprovedAt)
            .ToListAsync();
        var userIds = coaches.Select(coach => coach.UserId).Distinct().ToList();
        var users = await _users.Find(user => userIds.Contains(user.Id!)).ToListAsync();
        var usersById = users.ToDictionary(user => user.Id!);

        return coaches
            .Where(coach => usersById.ContainsKey(coach.UserId))
            .Select(coach =>
            {
                var user = usersById[coach.UserId];
                return new CoachResponseDto(
                    coach.UserId,
                    user.Username,
                    user.Bio,
                    user.ProfilePictureUrl,
                    coach.CertificationUrl,
                    coach.ApprovedAt,
                    coach.AverageRating,
                    coach.TotalParticipants);
            })
            .ToList();
    }

    public async Task<List<CoachResponseDto>> GetTopRatedCoachesAsync()
    {
        var coaches = await _coaches.Find(Builders<Coach>.Filter.Empty)
            .SortByDescending(coach => coach.AverageRating)
            .ThenByDescending(coach => coach.ApprovedAt)
            .Limit(10)
            .ToListAsync();
        var userIds = coaches.Select(coach => coach.UserId).Distinct().ToList();
        var users = await _users.Find(user => userIds.Contains(user.Id!)).ToListAsync();
        var usersById = users.ToDictionary(user => user.Id!);

        return coaches
            .Where(coach => usersById.ContainsKey(coach.UserId))
            .Select(coach =>
            {
                var user = usersById[coach.UserId];
                return new CoachResponseDto(
                    coach.UserId,
                    user.Username,
                    user.Bio,
                    user.ProfilePictureUrl,
                    coach.CertificationUrl,
                    coach.ApprovedAt,
                    coach.AverageRating,
                    coach.TotalParticipants);
            })
            .ToList();
    }

    public async Task<List<CoachResponseDto>> FindCoachesByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Coach name is required.");

        var matchingUsers = await _users.Find(new BsonDocumentFilterDefinition<User>(
                new BsonDocument("Username", new BsonRegularExpression(Regex.Escape(name.Trim()), "i"))))
            .ToListAsync();
        var userIds = matchingUsers.Select(user => user.Id!).ToList();
        var coaches = await _coaches.Find(coach => userIds.Contains(coach.UserId))
            .SortByDescending(coach => coach.ApprovedAt)
            .ToListAsync();
        var usersById = matchingUsers.ToDictionary(user => user.Id!);

        return coaches.Select(coach =>
        {
            var user = usersById[coach.UserId];
            return new CoachResponseDto(
                coach.UserId,
                user.Username,
                user.Bio,
                user.ProfilePictureUrl,
                coach.CertificationUrl,
                coach.ApprovedAt,
                coach.AverageRating,
                coach.TotalParticipants);
        }).ToList();
    }

    public async Task<List<CoachResponseDto>> GetParticipantCoachesAsync(string participantId)
    {
        var requests = await _trainingRequests.Find(request =>
                request.AthleteId == participantId &&
                request.Status == TrainingRequestStatus.Approved)
            .SortByDescending(request => request.ReviewedAt)
            .ToListAsync();
        var coachIds = requests.Select(request => request.CoachId).Distinct().ToList();
        if (coachIds.Count == 0)
            return new List<CoachResponseDto>();

        var coaches = await _coaches.Find(coach => coachIds.Contains(coach.UserId)).ToListAsync();
        var users = await _users.Find(user => coachIds.Contains(user.Id!)).ToListAsync();
        var usersById = users.ToDictionary(user => user.Id!);

        return coaches
            .Where(coach => usersById.ContainsKey(coach.UserId))
            .Select(coach =>
            {
                var user = usersById[coach.UserId];
                return new CoachResponseDto(
                    coach.UserId,
                    user.Username,
                    user.Bio,
                    user.ProfilePictureUrl,
                    coach.CertificationUrl,
                    coach.ApprovedAt,
                    coach.AverageRating,
                    coach.TotalParticipants);
            })
            .ToList();
    }

    public async Task<CoachResponseDto> RateCoachAsync(string athleteId, string coachId, RateCoachDto dto)
    {
        if (dto.Rating is < 1 or > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");
        if (athleteId == coachId)
            throw new InvalidOperationException("You cannot rate yourself.");

        var coach = await _coaches.Find(item => item.UserId == coachId).FirstOrDefaultAsync();
        if (coach == null)
            throw new KeyNotFoundException("Coach not found.");

        var approvedRelationship = await _trainingRequests.Find(request =>
            request.AthleteId == athleteId &&
            request.CoachId == coachId &&
            request.Status == TrainingRequestStatus.Approved).AnyAsync();
        if (!approvedRelationship)
            throw new UnauthorizedAccessException("You can rate a coach only after an approved training relationship.");

        var rating = await _coachRatings.Find(item => item.CoachId == coachId && item.AthleteId == athleteId)
            .FirstOrDefaultAsync();
        if (rating == null)
        {
            rating = new CoachRating { CoachId = coachId, AthleteId = athleteId, Rating = dto.Rating };
            await _coachRatings.InsertOneAsync(rating);
        }
        else
        {
            rating.Rating = dto.Rating;
            rating.UpdatedAt = DateTime.UtcNow;
            await _coachRatings.ReplaceOneAsync(item => item.Id == rating.Id, rating);
        }

        var average = await _coachRatings.Find(item => item.CoachId == coachId)
            .ToListAsync();
        coach.AverageRating = average.Count == 0 ? 0 : average.Average(item => item.Rating);
        await _coaches.ReplaceOneAsync(item => item.Id == coach.Id, coach);

        var user = await GetUserAsync(coachId)
            ?? throw new KeyNotFoundException("Coach user not found.");
        return new CoachResponseDto(
            coach.UserId,
            user.Username,
            user.Bio,
            user.ProfilePictureUrl,
            coach.CertificationUrl,
            coach.ApprovedAt,
            coach.AverageRating,
            coach.TotalParticipants);
    }

    public async Task<CoachApplicationResponseDto> SubmitCoachApplicationAsync(string userId, SubmitCoachApplicationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CertificationUrl))
            throw new ArgumentException("CertificationUrl is required.");

        var user = await GetUserAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (user.Roles.Any(role => role.Equals("Coach", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("You are already an approved coach.");

        var existing = await _applications.Find(a => a.UserId == userId).FirstOrDefaultAsync();
        if (existing?.Status == CoachApplicationStatus.Pending)
            throw new InvalidOperationException("Your coach application is already pending.");

        var application = new CoachApplication
        {
            Id = existing?.Id,
            UserId = userId,
            CertificationUrl = dto.CertificationUrl,
            Status = CoachApplicationStatus.Pending,
            ReviewNote = null,
            ReviewedBy = null,
            SubmittedAt = DateTime.UtcNow,
            ReviewedAt = null
        };

        if (existing == null)
            await _applications.InsertOneAsync(application);
        else
            await _applications.ReplaceOneAsync(a => a.Id == existing.Id, application);

        return MapApplication(application);
    }

    public async Task<CoachApplicationResponseDto?> GetMyCoachApplicationAsync(string userId)
    {
        var application = await _applications.Find(a => a.UserId == userId)
            .SortByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync();
        return application == null ? null : MapApplication(application);
    }

    public async Task<List<CoachApplicationResponseDto>> GetPendingCoachApplicationsAsync(string adminId)
    {
        await EnsureAdminAsync(adminId);
        var applications = await _applications.Find(a => a.Status == CoachApplicationStatus.Pending).ToListAsync();
        return applications.Select(MapApplication).ToList();
    }

    public async Task<CoachApplicationResponseDto> ReviewCoachApplicationAsync(
        string applicationId, string adminId, ReviewCoachApplicationDto dto)
    {
        await EnsureAdminAsync(adminId);
        var application = await _applications.Find(a => a.Id == applicationId).FirstOrDefaultAsync();
        if (application == null)
            throw new KeyNotFoundException("Coach application not found.");
        if (application.Status != CoachApplicationStatus.Pending)
            throw new InvalidOperationException("This coach application has already been reviewed.");

        application.Status = dto.Approved ? CoachApplicationStatus.Approved : CoachApplicationStatus.Rejected;
        application.ReviewNote = dto.ReviewNote;
        application.ReviewedBy = adminId;
        application.ReviewedAt = DateTime.UtcNow;
        await _applications.ReplaceOneAsync(a => a.Id == application.Id, application);

        if (dto.Approved)
        {
            var update = Builders<User>.Update.AddToSet(u => u.Roles, "Coach");
            await _users.UpdateOneAsync(u => u.Id == application.UserId, update);

            var coach = new Coach
            {
                UserId = application.UserId,
                CertificationUrl = application.CertificationUrl,
                ApprovedBy = adminId,
                ApprovedAt = application.ReviewedAt!.Value
            };
            await _coaches.ReplaceOneAsync(
                c => c.UserId == coach.UserId,
                coach,
                new ReplaceOptions { IsUpsert = true });
        }

        return MapApplication(application);
    }

    public async Task<TrainingRequestResponseDto> CreateTrainingRequestAsync(
        string athleteId, CreateTrainingRequestDto dto)
    {
        var athlete = await GetUserAsync(athleteId);
        if (athlete == null)
            throw new KeyNotFoundException("Athlete not found.");

        var coach = await GetUserAsync(dto.CoachId);
        if (coach == null)
            throw new KeyNotFoundException("Coach not found.");
        if (!coach.Roles.Any(role => role.Equals("Coach", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The selected user is not an approved coach.");
        if (athleteId == dto.CoachId)
            throw new InvalidOperationException("You cannot send a training request to yourself.");

        var pending = await _trainingRequests.Find(r =>
            r.AthleteId == athleteId && r.CoachId == dto.CoachId && r.Status == TrainingRequestStatus.Pending)
            .AnyAsync();
        if (pending)
            throw new InvalidOperationException("A training request is already pending for this coach.");

        var request = new TrainingRequest
        {
            AthleteId = athleteId,
            CoachId = dto.CoachId,
            Message = dto.Message
        };
        await _trainingRequests.InsertOneAsync(request);
        return MapTrainingRequest(request);
    }

    public async Task<List<TrainingRequestResponseDto>> GetCoachTrainingRequestsAsync(string coachId)
    {
        await EnsureCoachAsync(coachId);
        var requests = await _trainingRequests.Find(r => r.CoachId == coachId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
        return requests.Select(MapTrainingRequest).ToList();
    }

    public async Task<TrainingRequestResponseDto> ReviewTrainingRequestAsync(
        string requestId, string coachId, ReviewTrainingRequestDto dto)
    {
        await EnsureCoachAsync(coachId);
        var request = await _trainingRequests.Find(r => r.Id == requestId && r.CoachId == coachId)
            .FirstOrDefaultAsync();
        if (request == null)
            throw new KeyNotFoundException("Training request not found.");
        if (request.Status != TrainingRequestStatus.Pending)
            throw new InvalidOperationException("This training request has already been reviewed.");

        request.Status = dto.Approved ? TrainingRequestStatus.Approved : TrainingRequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        await _trainingRequests.ReplaceOneAsync(r => r.Id == request.Id, request);

        if (dto.Approved)
        {
            await _coaches.UpdateOneAsync(
                coach => coach.UserId == request.CoachId,
                Builders<Coach>.Update.Inc(coach => coach.TotalParticipants, 1));
        }

        return MapTrainingRequest(request);
    }

    private async Task<User?> GetUserAsync(string userId) =>
        await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();

    private async Task EnsureAdminAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");
        if (!user.Roles.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Only an admin can review coach applications.");
    }

    private async Task EnsureCoachAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");
        if (!user.Roles.Any(role => role.Equals("Coach", StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Only an approved coach can manage training requests.");
    }

    private static CoachApplicationResponseDto MapApplication(CoachApplication application) =>
        new(application.Id, application.UserId, application.CertificationUrl,
            application.Status.ToString(), application.ReviewNote,
            application.SubmittedAt, application.ReviewedAt);

    private static TrainingRequestResponseDto MapTrainingRequest(TrainingRequest request) =>
        new(request.Id, request.AthleteId, request.CoachId, request.Message,
            request.Status.ToString(), request.CreatedAt, request.ReviewedAt);
}