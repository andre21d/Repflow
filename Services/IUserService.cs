using Microsoft.AspNetCore.Http;
using Repflow.Api.DTOs;

namespace Repflow.Api.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<string> UploadProfilePictureAsync(string userId, IFormFile file, string hostUrl);
    Task<bool> ToggleAccountPrivacyAsync(string userId, bool isPrivate);
}