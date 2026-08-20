using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
public class Coach
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;
    public string CertificationUrl { get; set; } = null!;
    public string ApprovedBy { get; set; } = null!;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public double AverageRating { get; set; }
    public int TotalParticipants { get; set; }
}

public class CoachRating
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CoachId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string AthleteId { get; set; } = null!;

    public int Rating { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CoachApplication
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;
    public string CertificationUrl { get; set; } = null!;
    public CoachApplicationStatus Status { get; set; } = CoachApplicationStatus.Pending;
    public string? ReviewNote { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public enum CoachApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

public class TrainingRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string AthleteId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CoachId { get; set; } = null!;
    public string? Message { get; set; }
    public TrainingRequestStatus Status { get; set; } = TrainingRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public enum TrainingRequestStatus
{
    Pending,
    Approved,
    Rejected
}
}