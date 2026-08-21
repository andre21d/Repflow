using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string AuthorId { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string? CommunityId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserSessionId { get; set; }

        public string Content { get; set; } = string.Empty;

        public List<string> MediaUrls { get; set; } = new(); 

        public int LikesCount { get; set; } = 0;

        public int CommentsCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public bool IsBlocked { get; set; } = false;
        public DateTime? BlockedAt { get; set; }
        public string? BlockedBy { get; set; }
    }
}