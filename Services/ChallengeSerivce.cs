
using Repflow.Api.DTOs;
using Repflow.Api.Models;
using Repflow.Api.Services;

using MongoDB.Driver;


namespace Repflow.Api.Services

{
    public class ChallengeService : IChallengeService
    {
        private readonly IMongoCollection<Challenge> _challenges;
        private readonly IMongoCollection<ChallengeParticipant> _challengeParticipants;
        private readonly IMongoCollection<Community> _communities;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<CommunityMember> _communityMembers;
        public ChallengeService(IMongoDatabase database)
        {
            _challenges = database.GetCollection<Challenge>("Challenges");
            _challengeParticipants = database.GetCollection<ChallengeParticipant>("ChallengeParticipants");
            _communities = database.GetCollection<Community>("Communities");    
            _users = database.GetCollection<User>("Users");
            _communityMembers = database.GetCollection<CommunityMember>("CommunityMembers");
        }



        public async Task<ChallengeResponseDto> CreateChallengeAsync(string userId, string communityId, CreateChallengeDto dto)
        {   
            var user = _users.Find(u => u.Id == userId).FirstOrDefault();
            var community = _communities.Find(c => c.Id == communityId).FirstOrDefault();
        
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }
            if (community == null)
            {
                throw new InvalidOperationException("Community not found");
            }
            var communityMember = _communityMembers.Find(m => m.UserId == userId && m.CommunityId == communityId).FirstOrDefault();
            if (communityMember == null)
            {
                throw new UnauthorizedAccessException("User is not a member of the community");
            }
            if (!community.AdminIds.Contains(userId) && community.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("User is not an admin or owner of the community");
            }
            var challenge = new Challenge
            {
                CreatorId = userId,
                CommunityId = communityId,
                Name = dto.Name,
                Description = dto.Description ?? "",
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Goal = dto.Goal
            };
            await _challenges.InsertOneAsync(challenge);
            return await Task.FromResult(MapToResponseDto(challenge));
        }

        public Task<List<ChallengeResponseDto>> GetActiveChallengesByCommunityId(string communityId)
        {
            var currentDate = DateTime.UtcNow;
            var activeChallenges = _challenges.Find(c => c.CommunityId == communityId && c.StartDate <= currentDate && c.EndDate >= currentDate).ToList();
            var responseDtos = activeChallenges.Select(MapToResponseDto).ToList();
            return Task.FromResult(responseDtos);
        }

        public Task<ChallengeResponseDto?> GetChallengeByIdAsync(string id)
        {
            var challenge = _challenges.Find(c => c.Id == id).FirstOrDefault();
            if (challenge == null)
            {
                return Task.FromResult<ChallengeResponseDto?>(null);
            }
            var responseDto = new ChallengeResponseDto(
                Id: challenge.Id,
                Name: challenge.Name,
                Description: challenge.Description,
                CreatorId: challenge.CreatorId,
                CommunityId: challenge.CommunityId,
                StartDate: challenge.StartDate,
                EndDate: challenge.EndDate,
                Goal: challenge.Goal
            );
            return Task.FromResult<ChallengeResponseDto?>(responseDto);
        }

        public Task<List<ChallengeResponseDto>> GetChallengesByCommunityIdAsync(string communityId)
        {
            var challenges = _challenges.Find(c => c.CommunityId == communityId).ToList();
            var responseDtos = challenges.Select(MapToResponseDto).ToList();
            return Task.FromResult(responseDtos);
        }

        public Task<List<ChallengeResponseDto>> GetChallengesByUserIdAsync(string userId)
        {
            var participantChallengeIds = _challengeParticipants.Find(p => p.UserId == userId).Project(p => p.ChallengeId).ToList();
            var challenges = _challenges.Find(c => participantChallengeIds.Contains(c.Id)).ToList();
            var responseDtos = challenges.Select(MapToResponseDto).ToList();
            return Task.FromResult(responseDtos);
        }

        public Task<List<ChallengeResponseDto>> GetChallengesUserCanJoinAsync(string userId)
        {
            var userCommunities = _communityMembers.Find(m => m.UserId == userId).Project(m => m.CommunityId).ToList();
            var challenges = _challenges.Find(c => userCommunities.Contains(c.CommunityId)).ToList();
            var participantChallengeIds = _challengeParticipants.Find(p => p.UserId == userId).Project(p => p.ChallengeId).ToList();
            challenges = challenges.Where(c => !participantChallengeIds.Contains(c.Id)).ToList();
            var currentDate = DateTime.UtcNow;
            challenges = challenges.Where(c => c.StartDate <= currentDate && c.EndDate >= currentDate).ToList();
            var responseDtos = challenges.Select(MapToResponseDto).ToList();
            return Task.FromResult(responseDtos);
        }

        public async Task<string> JoinChallengeAsync(string communityId, string userId,string challengeId)
        {
            var user = _users.Find(u => u.Id == userId).FirstOrDefault();
            var community = _communities.Find(c => c.Id == communityId).FirstOrDefault();
            var challenge = _challenges.Find(c => c.Id == challengeId).FirstOrDefault();
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }
            if (community == null)
            {
                throw new InvalidOperationException("Community not found");
            }
            if (challenge == null)
            {
                throw new InvalidOperationException("Challenge not found");
            }
            var communityMember = _communityMembers.Find(m => m.UserId == userId && m.CommunityId == communityId).FirstOrDefault();
            if (communityMember == null)
            {
                throw new UnauthorizedAccessException("User is not a member of the community");
            }
            var existingParticipant = _challengeParticipants.Find(p => p.UserId == userId && p.ChallengeId == challengeId).FirstOrDefault();
            if (existingParticipant != null)
            {
                throw new InvalidOperationException("User has already joined the challenge");
            }
            
            var participant = new ChallengeParticipant
            {
                UserId = userId,
                ChallengeId = challengeId,
                GoalParticipation = 0
            };
            
            await _challengeParticipants.InsertOneAsync(participant);
            
            return await Task.FromResult("User joined the challenge successfully");
        }

        public Task<string> updateParticipantAsync(string challengeId, string userId, double goalParticipation)
        {
            DateTime currentDate = DateTime.UtcNow;
            var challenge = _challenges.Find(c => c.Id == challengeId).FirstOrDefault();
            if (challenge == null)
            {
                throw new InvalidOperationException("Challenge not found");
            }
            if (currentDate < challenge.StartDate)
            {
                throw new InvalidOperationException("Challenge has not started yet");
            
            }
            if (currentDate > challenge.EndDate)
            {
                throw new InvalidOperationException("Challenge has already ended");
            }
            if (goalParticipation > challenge.Goal)
            {
                throw new InvalidOperationException("Goal participation cannot exceed challenge goal");
            }
            
            var participant = _challengeParticipants.Find(p => p.UserId == userId && p.ChallengeId == challengeId).FirstOrDefault();
            if (participant == null)
            {
                throw new InvalidOperationException("User is not a participant of the challenge");
            }
            if (goalParticipation < 0)
            {
                throw new InvalidOperationException("Goal participation cannot be negative");
            }
            if(participant.GoalParticipation == 0 && goalParticipation > 0)
            {
                participant.GoalParticipation = goalParticipation;
                _challengeParticipants.ReplaceOne(p => p.Id == participant.Id, participant);
                challenge.Goal += goalParticipation;
                _challenges.UpdateOne(c => c.Id == challengeId, Builders<Challenge>.Update.Inc(c => c.Goal, challenge.Goal));
                return Task.FromResult("Participant updated successfully");
            }
            else
            {
                participant.GoalParticipation += goalParticipation;
                _challengeParticipants.ReplaceOne(p => p.Id == participant.Id, participant);
                challenge.Goal += goalParticipation;
                _challenges.UpdateOne(c => c.Id == challengeId, Builders<Challenge>.Update.Inc(c => c.Goal, challenge.Goal));
                return Task.FromResult("Participant updated successfully");
            }
        }
        private ChallengeResponseDto MapToResponseDto(Challenge challenge)
        {
            
            return new ChallengeResponseDto(
                challenge.Id,
                challenge.CreatorId,
                challenge.CommunityId,
                challenge.Name,
                challenge.Description,
                challenge.StartDate,
                challenge.EndDate,
                challenge.Goal
            );
        }
    }
}