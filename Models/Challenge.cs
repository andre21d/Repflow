using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class Challenge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatorId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string CommunityId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } ="";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
        public DateTime StartDate { get; set; } 
        public DateTime EndDate { get; set; } 
        public int TotalParticipants { get; set; } = 0;
        public Double Goal { get; set; } = 0;
        public Double Progress { get; set; } = 0;
    }
    public class ChallengeParticipant
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string ChallengeId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;
        public double GoalParticipation { get; set; } = 0;
        public DateTime LastCheckInDate { get; set; } = DateTime.MinValue;
    }
}
