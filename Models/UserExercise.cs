using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class UserExercise
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string ExerciseId { get; set; } = null!;

        public int Reps { get; set; }
        public int Sets { get; set; }
        public double Weight { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public bool IsPr { get; set; }
    }
}