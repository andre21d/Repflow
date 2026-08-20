namespace Repflow.Api.DTOs
{
    public record NotificationResponseDto(
        string Id,
        string TriggeredById,
        string Type,
        string TargetId,
        string Content,
        bool IsRead,
        DateTime CreatedAt
    );
}