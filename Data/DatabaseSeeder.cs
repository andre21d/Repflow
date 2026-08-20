using MongoDB.Bson;
using MongoDB.Driver;
using Repflow.Api.Models;

namespace Repflow.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IMongoDatabase database)
    {
        var now = DateTime.UtcNow;
        var users = BuildUsers(now);
        var coaches = BuildCoaches(users, now);
        var exercises = BuildExercises();
        var communities = BuildCommunities(users, now);
        var communityMembers = BuildCommunityMembers(users, communities, now);
        var privateRequests = BuildPrivateRequests(users, communities, now);
        var challenges = BuildChallenges(users, communities, now);
        var challengeParticipants = BuildChallengeParticipants(users, challenges);
        var applications = BuildCoachApplications(users, now);
        var trainingRequests = BuildTrainingRequests(users, coaches, now);
        var ratings = BuildCoachRatings(users, coaches, now);
        var physicalData = BuildPhysicalData(users, exercises, now);
        var userExercises = BuildUserExercises(users, exercises, now);
        var sessions = BuildSessions(users, userExercises, now);
        var templates = BuildTemplates(users, exercises, now);
        var templateDays = BuildTemplateDays(templates, users, exercises);
        var plans = BuildPlans(users, coaches, now);
        var planDays = BuildPlanDays(plans, users, exercises, now);
        var posts = BuildPosts(users, communities, now);
        var likes = BuildLikes(users, posts, now);
        var comments = BuildComments(users, posts, now);
        var follows = BuildFollows(users, now);

        await UpsertAsync(database.GetCollection<User>("Users"), users);
        await UpsertAsync(database.GetCollection<UserPhysicalData>("UserPhysicalData"), physicalData);
        await UpsertAsync(database.GetCollection<Coach>("Coaches"), coaches);
        await UpsertAsync(database.GetCollection<CoachApplication>("CoachApplications"), applications);
        await UpsertAsync(database.GetCollection<TrainingRequest>("TrainingRequests"), trainingRequests);
        await UpsertAsync(database.GetCollection<CoachRating>("CoachRatings"), ratings);
        await UpsertAsync(database.GetCollection<Exercise>("Exercises"), exercises);
        await UpsertAsync(database.GetCollection<UserExercise>("UserExercises"), userExercises);
        await UpsertAsync(database.GetCollection<UserSession>("UserSessions"), sessions);
        await UpsertAsync(database.GetCollection<WorkoutTemplate>("WorkoutTemplates"), templates);
        await UpsertAsync(database.GetCollection<WorkoutPlanDayTemplate>("WorkoutPlanDayTemplates"), templateDays);
        await UpsertAsync(database.GetCollection<WorkoutPlan>("WorkoutPlans"), plans);
        await UpsertAsync(database.GetCollection<WorkoutPlanDay>("WorkoutPlanDays"), planDays);
        await UpsertAsync(database.GetCollection<Community>("Communities"), communities);
        await UpsertAsync(database.GetCollection<CommunityMember>("CommunityMembers"), communityMembers);
        await UpsertAsync(database.GetCollection<PrivateCommunityRequest>("PrivateCommunityRequests"), privateRequests);
        await UpsertAsync(database.GetCollection<Challenge>("Challenges"), challenges);
        await UpsertAsync(database.GetCollection<ChallengeParticipant>("ChallengeParticipants"), challengeParticipants);
        await UpsertAsync(database.GetCollection<Post>("Posts"), posts);
        await UpsertAsync(database.GetCollection<Like>("Likes"), likes);
        await UpsertAsync(database.GetCollection<Comment>("Comments"), comments);
        await UpsertAsync(database.GetCollection<Follow>("Follows"), follows);
    }

    private static List<User> BuildUsers(DateTime now) => Enumerable.Range(0, 20).Select(index => new User
    {
        Id = Id(1, index),
        Username = index < 10 ? $"coach{index + 1}" : $"athlete{index - 9}",
        Email = $"seed-user-{index + 1}@repflow.test",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Repflow123!"),
        Bio = index < 10 ? $"Certified coach {index + 1}" : $"Repflow athlete {index - 9}",
        ProfilePictureUrl = "",
        IsEmailVerified = true,
        Roles = index < 10
            ? index == 0 ? new List<string> { "Coach", "Admin" } : new List<string> { "Coach" }
            : new List<string>(),
        CreatedAt = now.AddDays(-100 - index),
        streak = index + 1
    }).ToList();

    private static List<Coach> BuildCoaches(List<User> users, DateTime now) => Enumerable.Range(0, 10).Select(index => new Coach
    {
        Id = Id(2, index),
        UserId = users[index].Id!,
        CertificationUrl = "string",
        ApprovedBy = users[0].Id!,
        ApprovedAt = now.AddDays(-80 - index),
        AverageRating = 5 - (index % 5),
        TotalParticipants = 1
    }).ToList();

    private static List<CoachApplication> BuildCoachApplications(List<User> users, DateTime now) => Enumerable.Range(0, 10).Select(index => new CoachApplication
    {
        Id = Id(3, index),
        UserId = users[index].Id!,
        CertificationUrl = "string",
        Status = CoachApplicationStatus.Approved,
        ReviewNote = "Approved seed application",
        ReviewedBy = users[0].Id!,
        SubmittedAt = now.AddDays(-90 - index),
        ReviewedAt = now.AddDays(-80 - index)
    }).ToList();

    private static List<TrainingRequest> BuildTrainingRequests(List<User> users, List<Coach> coaches, DateTime now) => Enumerable.Range(0, 10).Select(index => new TrainingRequest
    {
        Id = Id(4, index),
        AthleteId = users[10 + index].Id!,
        CoachId = coaches[index].UserId,
        Message = $"Seed training request {index + 1}",
        Status = TrainingRequestStatus.Approved,
        CreatedAt = now.AddDays(-40 - index),
        ReviewedAt = now.AddDays(-35 - index)
    }).ToList();

    private static List<CoachRating> BuildCoachRatings(List<User> users, List<Coach> coaches, DateTime now) => Enumerable.Range(0, 10).Select(index => new CoachRating
    {
        Id = Id(5, index),
        CoachId = coaches[index].UserId,
        AthleteId = users[10 + index].Id!,
        Rating = 5 - (index % 5),
        UpdatedAt = now.AddDays(-20 - index)
    }).ToList();

    private static List<Exercise> BuildExercises() => Enumerable.Range(0, 10).Select(index => new Exercise
    {
        Id = Id(6, index),
        Name = new[] { "Barbell Bench Press", "Back Squat", "Deadlift", "Overhead Press", "Pull Up", "Barbell Row", "Bicep Curl", "Tricep Extension", "Lunge", "Plank" }[index],
        Description = $"Seed exercise description {index + 1}",
        MainMuscle = (Muscle)(index % 12),
        SecondaryMuscles = new List<Muscle> { (Muscle)((index + 1) % 12) }
    }).ToList();

    private static List<UserPhysicalData> BuildPhysicalData(List<User> users, List<Exercise> exercises, DateTime now) => users.Select((user, index) => new UserPhysicalData
    {
        Id = Id(7, index),
        UserId = user.Id!,
        HeightCm = 165 + index,
        HeightIsPrivate = index % 2 == 0,
        WeightsIsPrivate = index % 3 == 0,
        Sex = index % 2 == 0 ? Sex.Male : Sex.Female,
        SexIsPrivate = index % 2 == 0,
        Birthday = new DateTime(1990 + index % 10, 1 + index % 12, 1 + index % 20),
        BirthdayIsPrivate = index % 3 == 0,
        Weights = new List<WeightEntry>
        {
            new() { WeightKg = 65 + index, AddedAt = now.AddDays(-30) },
            new() { WeightKg = 66 + index, AddedAt = now.AddDays(-1) }
        },
        PersonalRecordsIsPrivate = index % 2 == 0,
        PersonalRecords = new List<PersonalRecord>
        {
            new() { ExerciseId = exercises[index % exercises.Count].Id!, MaxWeightKg = 40 + index * 2, Date = now.AddDays(-index - 1) }
        }
    }).ToList();

    private static List<UserExercise> BuildUserExercises(List<User> users, List<Exercise> exercises, DateTime now) => Enumerable.Range(0, 20).Select(index =>
    {
        var athleteIndex = index / 2;
        return new UserExercise
        {
            Id = Id(8, index),
            UserId = users[10 + athleteIndex].Id!,
            ExerciseId = exercises[index % exercises.Count].Id!,
            Reps = 5 + index % 8,
            Sets = 3,
            Weight = 40 + index * 2,
            RecordedAt = now.AddDays(-index - 1),
            IsPr = index % 2 == 0
        };
    }).ToList();

    private static List<UserSession> BuildSessions(List<User> users, List<UserExercise> userExercises, DateTime now) => Enumerable.Range(0, 10).Select(index => new UserSession
    {
        Id = Id(9, index),
        UserId = users[10 + index].Id!,
        Description = $"Seed workout session {index + 1}",
        Muscles = new List<Muscle> { (Muscle)(index % 12) },
        UserExerciseIds = new List<string> { userExercises[index * 2].Id!, userExercises[index * 2 + 1].Id! },
        Date = now.Date.AddDays(-index - 1),
        TotalDurationMinutes = 40 + index
    }).ToList();

    private static List<WorkoutTemplate> BuildTemplates(List<User> users, List<Exercise> exercises, DateTime now) => Enumerable.Range(0, 10).Select(index => new WorkoutTemplate
    {
        Id = Id(10, index),
        UserId = users[index].Id!,
        Name = $"Seed template {index + 1}",
        DurationDays = 7,
        IsGeneral = false,
        IsArchived = false,
        CreatedAt = now.AddDays(-index)
    }).ToList();

    private static List<WorkoutPlanDayTemplate> BuildTemplateDays(List<WorkoutTemplate> templates, List<User> users, List<Exercise> exercises) => templates.Select((template, index) => new WorkoutPlanDayTemplate
    {
        Id = Id(11, index),
        WorkoutTemplateId = template.Id!,
        UserId = users[index].Id!,
        Order = 1,
        Name = $"Seed template day {index + 1}",
        IsRestDay = false,
        Exercises = new List<PlannedExercise>
        {
            new() { ExerciseId = exercises[index].Id!, ExerciseName = exercises[index].Name, Order = 1, PlannedSets = 3, PlannedReps = 8, PlannedWeight = 40 + index }
        }
    }).ToList();

    private static List<WorkoutPlan> BuildPlans(List<User> users, List<Coach> coaches, DateTime now) => Enumerable.Range(0, 10).Select(index => new WorkoutPlan
    {
        Id = Id(12, index),
        OwnerUserId = users[10 + index].Id!,
        CreatedByUserId = coaches[index].UserId,
        CoachId = coaches[index].UserId,
        Name = $"Seed plan {index + 1}",
        DurationDays = 7,
        Status = WorkoutPlanStatus.Accepted,
        StartDate = now.Date.AddDays(-index),
        IsArchived = false,
        CreatedAt = now.AddDays(-index)
    }).ToList();

    private static List<WorkoutPlanDay> BuildPlanDays(List<WorkoutPlan> plans, List<User> users, List<Exercise> exercises, DateTime now) => plans.Select((plan, index) => new WorkoutPlanDay
    {
        Id = Id(13, index),
        WorkoutPlanId = plan.Id!,
        UserId = users[10 + index].Id!,
        Order = 1,
        Date = now.Date.AddDays(-index),
        Name = $"Seed plan day {index + 1}",
        IsRestDay = false,
        Exercises = new List<PlannedExercise>
        {
            new() { ExerciseId = exercises[index].Id!, ExerciseName = exercises[index].Name, Order = 1, PlannedSets = 3, PlannedReps = 8, PlannedWeight = 40 + index }
        },
        Completed = false
    }).ToList();

    private static List<Community> BuildCommunities(List<User> users, DateTime now) => Enumerable.Range(0, 10).Select(index => new Community
    {
        Id = Id(14, index),
        OwnerId = users[index].Id!,
        Name = $"Seed community {index + 1}",
        Description = $"Seed community description {index + 1}",
        AdminIds = new List<string> { users[index].Id! },
        ImageUrl = "",
        MembersCount = 2,
        CreatedAt = now.AddDays(-index),
        IsPrivate = index % 2 == 0,
        ChallengeIds = new List<string> { Id(16, index) }
    }).ToList();

    private static List<CommunityMember> BuildCommunityMembers(List<User> users, List<Community> communities, DateTime now) => Enumerable.Range(0, 20).Select(index => new CommunityMember
    {
        Id = Id(15, index),
        CommunityId = communities[index / 2].Id!,
        UserId = users[(index / 2 + index % 2 * 10) % 20].Id!,
        JoinedAt = now.AddDays(-index)
    }).ToList();

    private static List<PrivateCommunityRequest> BuildPrivateRequests(List<User> users, List<Community> communities, DateTime now) => Enumerable.Range(0, 10).Select(index => new PrivateCommunityRequest
    {
        Id = Id(17, index),
        CommunityId = communities[index].Id!,
        UserId = users[10 + index].Id!,
        RequestedAt = now.AddDays(-index),
        Status = index % 2 == 0 ? Requeststatus.Pending : Requeststatus.Rejected
    }).ToList();

    private static List<Challenge> BuildChallenges(List<User> users, List<Community> communities, DateTime now) => Enumerable.Range(0, 10).Select(index => new Challenge
    {
        Id = Id(16, index),
        CreatorId = users[index].Id!,
        CommunityId = communities[index].Id!,
        Name = $"Seed challenge {index + 1}",
        Description = $"Seed challenge description {index + 1}",
        CreatedAt = now.AddDays(-index),
        StartDate = now.AddDays(-10),
        EndDate = now.AddDays(20),
        TotalParticipants = 1,
        Goal = 100 + index,
        Progress = 25 + index
    }).ToList();

    private static List<ChallengeParticipant> BuildChallengeParticipants(List<User> users, List<Challenge> challenges) => Enumerable.Range(0, 10).Select(index => new ChallengeParticipant
    {
        Id = Id(18, index),
        ChallengeId = challenges[index].Id!,
        UserId = users[10 + index].Id!,
        GoalParticipation = 10 + index,
        LastCheckInDate = DateTime.UtcNow.Date.AddDays(-index)
    }).ToList();

    private static List<Post> BuildPosts(List<User> users, List<Community> communities, DateTime now) => Enumerable.Range(0, 10).Select(index => new Post
    {
        Id = Id(19, index),
        AuthorId = users[10 + index].Id!,
        CommunityId = communities[index].Id!,
        Content = $"Seed post content {index + 1}",
        MediaUrls = new List<string>(),
        LikesCount = 1,
        CommentsCount = 1,
        CreatedAt = now.AddHours(-index)
    }).ToList();

    private static List<Like> BuildLikes(List<User> users, List<Post> posts, DateTime now) => Enumerable.Range(0, 10).Select(index => new Like
    {
        Id = Id(20, index),
        PostId = posts[index].Id!,
        UserId = users[index].Id!,
        CreatedAt = now.AddHours(-index)
    }).ToList();

    private static List<Comment> BuildComments(List<User> users, List<Post> posts, DateTime now) => Enumerable.Range(0, 10).Select(index => new Comment
    {
        Id = Id(21, index),
        PostId = posts[index].Id!,
        AuthorId = users[index].Id!,
        ParentCommentId = null,
        Content = $"Seed comment {index + 1}",
        CreatedAt = now.AddMinutes(-index)
    }).ToList();

    private static List<Follow> BuildFollows(List<User> users, DateTime now) => Enumerable.Range(0, 10).Select(index => new Follow
    {
        Id = Id(22, index),
        FollowerId = users[index].Id!,
        FollowingId = users[10 + index].Id!,
        Status = "Accepted",
        CreatedAt = now.AddDays(-index)
    }).ToList();

    private static async Task UpsertAsync<T>(IMongoCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            var document = item.ToBsonDocument();
            await collection.ReplaceOneAsync(
                new BsonDocument("_id", document["_id"]),
                item,
                new ReplaceOptions { IsUpsert = true });
        }
    }

    private static string Id(int group, int index) => $"{group:X2}{index:X2}{new string('0', 20)}";
}
