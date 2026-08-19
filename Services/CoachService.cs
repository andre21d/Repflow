using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services;

public class CoachService : ICoachService
{
    private readonly IMongoCollection<CoachApplication> _applications;
    private readonly IMongoCollection<TrainingRequest> _trainingRequests;
    private readonly IMongoCollection<User> _users;

    public CoachService(IMongoDatabase database)
    {
        _applications = database.GetCollection<CoachApplication>("CoachApplications");
        _trainingRequests = database.GetCollection<TrainingRequest>("TrainingRequests");
        _users = database.GetCollection<User>("Users");
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