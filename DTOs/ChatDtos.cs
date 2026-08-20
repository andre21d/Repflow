namespace Repflow.Api.DTOs
{
    public record SendMessageDto(string ReceiverId, string Content);

    public record MessageResponseDto(
        string Id,
        string SenderId,
        string ReceiverId,
        string Content,
        bool IsRead,
        DateTime SentAt
    );
}