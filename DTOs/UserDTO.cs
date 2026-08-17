namespace Repflow.Api.DTOs;

public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? ProfilePictureUrl { get; init; }
}

public record UpdateProfileDto
{
    public string? Bio { get; init; }
    public string? ProfilePictureUrl { get; init; }
}