using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class UserSession
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;

        public string? Description { get; set; }
        public List<Muscle> Muscles { get; set; } = new();
        public List<string> UserExerciseIds { get; set; } = new();
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public int TotalDurationMinutes { get; set; }
    }
}