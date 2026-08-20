using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface IUserSessionService
    {
        Task<UserSessionResponseDto> CreateAsync(string userId, UserSessionInputDto dto);
        Task<UserSessionResponseDto?> UpdateAsync(string userId, string sessionId, UserSessionInputDto dto);
        Task<List<UserSessionResponseDto>> GetAllAsync(string userId);
        Task<List<UserSessionResponseDto>> GetByDayAsync(string userId, DateTime date);
        Task<List<UserSessionResponseDto>> GetByMonthAsync(string userId, int year, int month);
    }
}