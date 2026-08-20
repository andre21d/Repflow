using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class PostService : IPostService
    {
        private readonly IMongoCollection<Post> _posts;
        private readonly IMongoCollection<Like> _likes;
        private readonly IMongoCollection<Comment> _comments;  
        private readonly IMongoCollection<Follow> _follows;
        private readonly IMongoCollection<CommunityMember> _communityMembers; 
        private readonly IMongoCollection<UserSession> _userSessions;
        private readonly IMongoCollection<Community> _communities;
        private readonly INotificationService _notificationService;

        public PostService(IMongoDatabase database)
        {
            _posts = database.GetCollection<Post>("Posts");
            _likes = database.GetCollection<Like>("Likes");
            _comments = database.GetCollection<Comment>("Comments");  
            _follows = database.GetCollection<Follow>("Follows");
            _communityMembers = database.GetCollection<CommunityMember>("CommunityMembers");
            _userSessions = database.GetCollection<UserSession>("UserSessions");
            _communityMembers = database.GetCollection<CommunityMember>("CommunityMembers"); 
            _communities = database.GetCollection<Community>("Communities");
        }

        public PostService(IMongoDatabase database, INotificationService notificationService) 
            : this(database)
        {
            _notificationService = notificationService;
        }

        public async Task<PostResponseDto> CreatePostAsync(string userId, CreatePostDto dto)
        {
            var post = new Post
            {
                AuthorId = userId,
                CommunityId = dto.CommunityId,
                Content = dto.Content,
                MediaUrls = dto.MediaUrls ?? new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            await _posts.InsertOneAsync(post);

            return MapToResponseDto(post, isLikedByCurrentUser: false);
        }

        public async Task<SessionPostResponseDto> CreateSessionPostAsync(string userId, CreateSessionPostDto dto)
        {
            var sessionExists = await _userSessions.Find(session =>
                session.Id == dto.UserSessionId && session.UserId == userId).AnyAsync();
            if (!sessionExists)
                throw new KeyNotFoundException("Session not found.");

            var post = new Post
            {
                AuthorId = userId,
                CommunityId = dto.CommunityId,
                UserSessionId = dto.UserSessionId,
                Content = dto.Content,
                MediaUrls = dto.MediaUrls ?? new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            await _posts.InsertOneAsync(post);

            return new SessionPostResponseDto(
                post.Id!, post.AuthorId, post.CommunityId, post.UserSessionId!, post.Content,
                post.MediaUrls, post.LikesCount, post.CommentsCount, post.CreatedAt);
        }

        public async Task<List<PostResponseDto>> GetAllPostsAsync()
        {
            var posts = await _posts.Find(_ => true)
                                     .SortByDescending(p => p.CreatedAt)
                                     .ToListAsync();

            return posts.Select(p => MapToResponseDto(p, isLikedByCurrentUser: false)).ToList();
        }

        public async Task<PostResponseDto?> GetPostByIdAsync(string id)
        {
            var post = await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
            return post == null ? null : MapToResponseDto(post, isLikedByCurrentUser: false);
        }

        public async Task<bool> DeletePostAsync(string postId, string userId)
        {
            var result = await _posts.DeleteOneAsync(p => p.Id == postId && p.AuthorId == userId);
            
            if (result.DeletedCount > 0)
            {
                await _likes.DeleteManyAsync(l => l.PostId == postId);
                await _comments.DeleteManyAsync(c => c.PostId == postId);
                return true;
            }

            return false;
        }

        public async Task<bool> ToggleLikeAsync(string postId, string userId)
        {
            var existingLike = await _likes.Find(l => l.PostId == postId && l.UserId == userId).FirstOrDefaultAsync();

            if (existingLike == null)
            {
                await _likes.InsertOneAsync(new Like { PostId = postId, UserId = userId });
                
                var update = Builders<Post>.Update.Inc(p => p.LikesCount, 1);
                await _posts.UpdateOneAsync(p => p.Id == postId, update);

                // Send notification on like
                var post = await _posts.Find(p => p.Id == postId).FirstOrDefaultAsync();
                if (post != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        recipientUserId: post.AuthorId,
                        triggeredById: userId,
                        type: "Like",
                        targetId: postId,
                        content: "Liked your post"
                    );
                }

                return true; 
            }
            else
            {
                await _likes.DeleteOneAsync(l => l.Id == existingLike.Id);

                var update = Builders<Post>.Update.Inc(p => p.LikesCount, -1);
                await _posts.UpdateOneAsync(p => p.Id == postId, update);
                return false; 
            }
        }

        public async Task<List<PostResponseDto>> GetFeedPostsAsync(string userId)
        {
            var followingUserIds = await _follows.Find(f => f.FollowerId == userId && f.Status == "Accepted")
                .Project(f => f.FollowingId)
                .ToListAsync();
                
            followingUserIds.Add(userId);

            var joinedCommunityIds = await _communityMembers.Find(cm => cm.UserId == userId)
                .Project(cm => cm.CommunityId)
                .ToListAsync();

            var filterBuilder = Builders<Post>.Filter;
            
            var filter = filterBuilder.Or(
                filterBuilder.In(p => p.AuthorId, followingUserIds),
                filterBuilder.In(p => p.CommunityId, joinedCommunityIds)
            );

            var posts = await _posts.Find(filter)
                .SortByDescending(p => p.CreatedAt)
                .ToListAsync();

            return await MapPostsWithLikesAsync(posts, userId);
        }

        public async Task<CommentResponseDto> AddCommentAsync(string postId, string userId, CreateCommentDto dto)
        {
            var comment = new Comment
            {
                PostId = postId,
                AuthorId = userId,
                Content = dto.Content,
                ParentCommentId = dto.ParentCommentId,
                CreatedAt = DateTime.UtcNow
            };

            await _comments.InsertOneAsync(comment);

            var update = Builders<Post>.Update.Inc(p => p.CommentsCount, 1);
            await _posts.UpdateOneAsync(p => p.Id == postId, update);

            // Send notification on comment
            var post = await _posts.Find(p => p.Id == postId).FirstOrDefaultAsync();
            if (post != null)
            {
                await _notificationService.CreateNotificationAsync(
                    recipientUserId: post.AuthorId,
                    triggeredById: userId,
                    type: "Comment",
                    targetId: postId,
                    content: "Comment on your post"
                );
            }

            return new CommentResponseDto(
                comment.Id!,
                comment.PostId,
                comment.AuthorId,
                comment.Content,
                comment.ParentCommentId,
                comment.CreatedAt
            );
        }

        public async Task<(bool IsSuccess, string? ErrorMessage, int StatusCode, List<PostResponseDto>? Posts)> GetCommunityPostsAsync(string communityId, string requestingUserId)
        {
            var community = await _communities.Find(c => c.Id == communityId).FirstOrDefaultAsync();
            if (community == null)
                return (false, "Community not found", 404, null);

            if (community.IsPrivate)
            {
                var isMember = await _communityMembers.Find(m => m.CommunityId == communityId && m.UserId == requestingUserId).AnyAsync();
                if (!isMember && community.OwnerId != requestingUserId)
                    return (false, "This community is private, you must be a member to view posts", 403, null);
            }

            var filter = Builders<Post>.Filter.Eq(p => p.CommunityId, communityId);
            var posts = await _posts.Find(filter)
                                    .SortByDescending(p => p.CreatedAt)
                                    .ToListAsync();

            var response = await MapPostsWithLikesAsync(posts, requestingUserId);
            return (true, null, 200, response);
        }

        public async Task<List<PostResponseDto>> GetUserPostsAsync(string userId, string requestingUserId)
        {
            var myPrivateCommunityIds = await _communityMembers.Find(m => m.UserId == requestingUserId)
                .Project(m => m.CommunityId)
                .ToListAsync();

            var builder = Builders<Post>.Filter;
            var filter = builder.Eq(p => p.AuthorId, userId) & (
                builder.Eq(p => p.CommunityId, null) | 
                builder.In(p => p.CommunityId, myPrivateCommunityIds)
            );

            var posts = await _posts.Find(filter)
                                    .SortByDescending(p => p.CreatedAt)
                                    .ToListAsync();

            return await MapPostsWithLikesAsync(posts, requestingUserId);
        }

        private async Task<List<PostResponseDto>> MapPostsWithLikesAsync(List<Post> posts, string userId)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            
            var userLikedPostIds = await _likes.Find(l => l.UserId == userId && postIds.Contains(l.PostId))
                .Project(l => l.PostId)
                .ToListAsync();

            var likedSet = new HashSet<string>(userLikedPostIds);

            return posts.Select(p => MapToResponseDto(p, likedSet.Contains(p.Id!))).ToList();
        }

        private static PostResponseDto MapToResponseDto(Post post, bool isLikedByCurrentUser)
        {
            return new PostResponseDto(
                post.Id!,
                post.AuthorId,
                post.CommunityId,
                post.Content,
                post.MediaUrls ?? new List<string>(),
                post.LikesCount,
                post.CommentsCount,
                isLikedByCurrentUser, 
                post.CreatedAt
            );
        }
    }
}