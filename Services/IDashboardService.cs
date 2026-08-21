using Repflow.Api.DTOs;

namespace Repflow.Api.Services;

public interface IDashboardService
{
    Task<DashboardStatisticsDto> GetStatisticsAsync(string adminId);
    Task SetUserBlockedAsync(string adminId, string userId, bool blocked);
    Task SetPostBlockedAsync(string adminId, string postId, bool blocked);
    Task CreateAdminAsync(string superAdminId, CreateAdminDto dto);
    Task<List<AdminSummaryDto>> GetAdminsAsync(string superAdminId);
    Task DeleteAdminAsync(string superAdminId, string adminId);
}
