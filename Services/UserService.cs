using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services;

public class UserService : IUserService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IWebHostEnvironment _environment;

    public UserService(IMongoDatabase database, IWebHostEnvironment environment)
    {
        _usersCollection = database.GetCollection<User>("Users");
        _environment = environment;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return null;

        return MapToDto(user);
    }
    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _usersCollection
            .Find(u => u.Username.ToLower() == username.ToLower())
            .FirstOrDefaultAsync();

        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        
        var updateDefinition = new List<UpdateDefinition<User>>();

        if (dto.Bio != null)
            updateDefinition.Add(Builders<User>.Update.Set(u => u.Bio, dto.Bio));

        if (dto.ProfilePictureUrl != null)
            updateDefinition.Add(Builders<User>.Update.Set(u => u.ProfilePictureUrl, dto.ProfilePictureUrl));

        if (updateDefinition.Count == 0)
            return await GetUserByIdAsync(userId);

        var update = Builders<User>.Update.Combine(updateDefinition);
        var result = await _usersCollection.FindOneAndUpdateAsync(
            filter, 
            update, 
            new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After }
        );

        if (result == null) return null;

        return MapToDto(result);
    }

    public async Task<string> UploadProfilePictureAsync(string userId, IFormFile file, string hostUrl)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("الملف غير صالح");

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var photoUrl = $"{hostUrl}/uploads/profiles/{fileName}";

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.ProfilePictureUrl, photoUrl);
        await _usersCollection.UpdateOneAsync(filter, update);

        return photoUrl;
    }

    private static UserDto MapToDto(User user) => new UserDto
    {
        Id = user.Id!,
        Username = user.Username,
        Email = user.Email,
        Bio = user.Bio,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
    
    public async Task<bool> ToggleAccountPrivacyAsync(string userId, bool isPrivate)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.IsPrivate, isPrivate);

        var result = await _usersCollection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}