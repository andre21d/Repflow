using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Repflow.Api.Models
{
    public enum Muscle
    {
        Chest,
        Back,
        Shoulders,
        Biceps,
        Triceps,
        Forearms,
        Abdominals,
        Glutes,
        Quadriceps,
        Hamstrings,
        Calves,
        FullBody
    }

    public class Exercise
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;

        [BsonRepresentation(BsonType.String)]
        public Muscle MainMuscle { get; set; }

        [BsonRepresentation(BsonType.String)]
        public List<Muscle> SecondaryMuscles { get; set; } = new();
    }
}
