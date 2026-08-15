using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class Follow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string FollowerId { get; set; } = null!; 
        [BsonRepresentation(BsonType.ObjectId)]
        public string FollowingId { get; set; } = null!; 
        public string Status { get; set; } = "Accepted";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}