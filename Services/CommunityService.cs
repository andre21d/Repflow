using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Repflow.Api.Models;

public class CommunitiesService
{
     private readonly IMongoCollection<Community> _communities;
        private readonly IConfiguration _configuration;

        public CommunitiesService(IMongoDatabase database, IConfiguration configuration)
        {
            _communities = database.GetCollection<Community>("Communities");
            _configuration = configuration;
        }
        public async Task<List<Community>> GetAllAsync()=>
            await _communities.Find(_=> true).ToListAsync();
        
//           public async Task<Community> GetByIdAsync()=>
//             await _communities.Find(c=> c.Id == id).FirstOrDe;
   }