using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string UserId { get; set; } = default!;        // المستلم
        public string TriggeredById { get; set; } = default!; // الفاعل (صاحب اللايك/التعليق/الرسالة)
        public string Type { get; set; } = default!;          // "Like", "Comment", "Message"
        public string TargetId { get; set; } = default!;      // PostId أو MessageId
        public string Content { get; set; } = default!;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}