using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services;

public interface IUserPhysicalDataService
{
    Task<UserPhysicalDataResponseDto> GetAsync(string requesterId, string userId);
    Task<UserPhysicalDataResponseDto> UpdateAsync(string requesterId, string userId, UpdatePhysicalDataDto dto);
    Task<UserPhysicalDataResponseDto> AddWeightAsync(string requesterId, string userId, AddWeightDto dto);
    Task UpdatePersonalRecordsAsync(string userId, IEnumerable<UserExercise> exercises);
    Task RecalculatePersonalRecordsAsync(string userId, IEnumerable<string> exerciseIds);
}