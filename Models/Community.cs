using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class Community
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string OwnerId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } ="";

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> AdminIds { get; set; } = new();
        public String? ImageUrl { get; set; }="";
        public int MembersCount { get; set; } = 1;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPrivate { get; set; } = false;
        public List<string> ChallengeIds { get; set; } = new();
    }
    public class CommunityMember
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string CommunityId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
    public class PrivateCommunityRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string CommunityId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public Requeststatus Status { get; set; } = Requeststatus.Pending;
    }
    public enum Requeststatus
    {
        Pending,
        Approved,
        Rejected
    }
}