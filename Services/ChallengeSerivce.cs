using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;
using Repflow.Api.Services;
 namespace Repflow.Api.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly IMongoCollection<Challenge> _challenges;
        private readonly IMongoCollection<ChallengeParticipant> _challengeParticipants;
        public ChallengeService(IMongoDatabase database)
        {
            _challenges = database.GetCollection<Challenge>("Challenges");
            _challengeParticipants = database.GetCollection<ChallengeParticipant>("ChallengeParticipants");
        }

        public Task<CommunityResponseDto> CreateChallengeAsync(string userId, CreateChallengeDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<CommunityResponseDto> CreateChallengeAsync(string userId, CreateCommunityDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<CommunityResponseDto?> GetChallengeByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> JoinChallengeAsync(string communityId, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<string> updateParticipant(string challengeId, string userId, double goalParticipation)
        {
            throw new NotImplementedException();
        }
    }
}    