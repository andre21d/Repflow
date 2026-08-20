using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public class PlannedExercise
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ExerciseId { get; set; } = null!;
        public string ExerciseName { get; set; } = null!;
        public int Order { get; set; }
        public int PlannedSets { get; set; }
        public int PlannedReps { get; set; }
        public double PlannedWeight { get; set; }
    }

    public class WorkoutTemplate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int DurationDays { get; set; }
        public bool IsGeneral { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WorkoutPlanDayTemplate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string WorkoutTemplateId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;
        public int Order { get; set; }
        public string Name { get; set; } = null!;
        public bool IsRestDay { get; set; }
        public List<PlannedExercise> Exercises { get; set; } = new();
    }

    public class WorkoutPlan
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string OwnerUserId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByUserId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CoachId { get; set; }
        public string Name { get; set; } = null!;
        public int DurationDays { get; set; }
        public WorkoutPlanStatus Status { get; set; } = WorkoutPlanStatus.Draft;
        public DateTime? StartDate { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WorkoutPlanDay
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string WorkoutPlanId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = null!;
        public int Order { get; set; }
        public DateTime? Date { get; set; }
        public string Name { get; set; } = null!;
        public bool IsRestDay { get; set; }
        public List<PlannedExercise> Exercises { get; set; } = new();
        public bool Completed { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserSessionId { get; set; }
    }

    public enum WorkoutPlanStatus
    {
        Draft,
        PendingAcceptance,
        Rejected,
        Accepted,
        Active,
        Completed,
        Cancelled,
        Archived
    }
}