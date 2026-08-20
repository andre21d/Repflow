using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models;

public class UserPhysicalData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    public double? HeightCm { get; set; }
    public bool HeightIsPrivate { get; set; } = true;
    public List<WeightEntry> Weights { get; set; } = new();
    public bool WeightsIsPrivate { get; set; } = true;
    public Sex? Sex { get; set; }
    public bool SexIsPrivate { get; set; } = true;
    public DateTime? Birthday { get; set; }
    public bool BirthdayIsPrivate { get; set; } = true;
    public List<PersonalRecord> PersonalRecords { get; set; } = new();
    public bool PersonalRecordsIsPrivate { get; set; } = true;
}

public class WeightEntry
{
    public double WeightKg { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class PersonalRecord
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string ExerciseId { get; set; } = null!;
    public double MaxWeightKg { get; set; }
    public DateTime Date { get; set; }
}

public enum Sex
{
    Male,
    Female
}