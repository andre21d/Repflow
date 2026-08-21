using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Post> _posts;
    private readonly IMongoCollection<Community> _communities;
    private readonly IMongoCollection<Coach> _coaches;
    private readonly IMongoCollection<CoachApplication> _applications;
    private readonly IMongoCollection<UserSession> _sessions;
    private readonly IMongoCollection<WorkoutPlan> _plans;
    private readonly IMongoCollection<UserPhysicalData> _physicalData;

    public DashboardService(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
        _posts = database.GetCollection<Post>("Posts");
        _communities = database.GetCollection<Community>("Communities");
        _coaches = database.GetCollection<Coach>("Coaches");
        _applications = database.GetCollection<CoachApplication>("CoachApplications");
        _sessions = database.GetCollection<UserSession>("UserSessions");
        _plans = database.GetCollection<WorkoutPlan>("WorkoutPlans");
        _physicalData = database.GetCollection<UserPhysicalData>("UserPhysicalData");
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync(string adminId)
    {
        await EnsureRoleAsync(adminId, "Admin");
        var counts = await Task.WhenAll(
            _users.CountDocumentsAsync(_ => true),
            _users.CountDocumentsAsync(user => user.IsBlocked),
            _posts.CountDocumentsAsync(_ => true),
            _posts.CountDocumentsAsync(post => post.IsBlocked),
            _communities.CountDocumentsAsync(_ => true),
            _coaches.CountDocumentsAsync(_ => true),
            _applications.CountDocumentsAsync(application => application.Status == CoachApplicationStatus.Pending),
            _sessions.CountDocumentsAsync(_ => true),
            _plans.CountDocumentsAsync(_ => true));

        return new DashboardStatisticsDto(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5], counts[6], counts[7], counts[8]);
    }

    public async Task SetUserBlockedAsync(string adminId, string userId, bool blocked)
    {
        await EnsureRoleAsync(adminId, "Admin");
        if (adminId == userId)
            throw new InvalidOperationException("You cannot block your own account.");

        var target = await _users.Find(user => user.Id == userId).FirstOrDefaultAsync();
        if (target == null)
            throw new KeyNotFoundException("User not found.");
        if (target.Roles.Any(role => role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("A superadmin account cannot be blocked.");
        if (target.Roles.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) && !await HasRoleAsync(adminId, "SuperAdmin"))
            throw new UnauthorizedAccessException("Only a superadmin can block an admin.");

        var update = Builders<User>.Update
            .Set(user => user.IsBlocked, blocked)
            .Set(user => user.BlockedAt, blocked ? DateTime.UtcNow : null)
            .Set(user => user.BlockedBy, blocked ? adminId : null);
        await _users.UpdateOneAsync(user => user.Id == userId, update);
    }

    public async Task SetPostBlockedAsync(string adminId, string postId, bool blocked)
    {
        await EnsureRoleAsync(adminId, "Admin");
        var post = await _posts.Find(item => item.Id == postId).FirstOrDefaultAsync();
        if (post == null)
            throw new KeyNotFoundException("Post not found.");

        var update = Builders<Post>.Update
            .Set(item => item.IsBlocked, blocked)
            .Set(item => item.BlockedAt, blocked ? DateTime.UtcNow : null)
            .Set(item => item.BlockedBy, blocked ? adminId : null);
        await _posts.UpdateOneAsync(item => item.Id == postId, update);
    }

    public async Task CreateAdminAsync(string superAdminId, CreateAdminDto dto)
    {
        await EnsureRoleAsync(superAdminId, "SuperAdmin");
        var duplicate = await _users.Find(user => user.Email.ToLower() == dto.Email.ToLower() || user.Username.ToLower() == dto.Username.ToLower()).AnyAsync();
        if (duplicate)
            throw new InvalidOperationException("Username or email already exists.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsEmailVerified = true,
            Roles = new List<string> { "Admin" },
            CreatedAt = DateTime.UtcNow
        };
        await _users.InsertOneAsync(user);
        await _physicalData.InsertOneAsync(new UserPhysicalData { UserId = user.Id! });
    }

    private async Task EnsureRoleAsync(string userId, string role)
    {
        if (!await HasRoleAsync(userId, role))
            throw new UnauthorizedAccessException($"Only a {role.ToLowerInvariant()} can perform this action.");
    }

    private async Task<bool> HasRoleAsync(string userId, string role)
    {
        var user = await _users.Find(item => item.Id == userId).FirstOrDefaultAsync();
        return user != null && user.Roles.Any(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
