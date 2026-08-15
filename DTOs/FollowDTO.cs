namespace Repflow.Api.DTOs
{
    public record UserFollowDto(
        string UserId,
        string Username
    );

    public record FollowStatusDto(
        bool IsFollowing,
        string Message
    );
}