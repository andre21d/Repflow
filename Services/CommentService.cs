using MongoDB.Driver;
using Repflow.Api.DTOs;
using Repflow.Api.Models;

namespace Repflow.Api.Services
{
    public class CommentService : ICommentService
    {
        private readonly IMongoCollection<Comment> _comments;

        public CommentService(IMongoDatabase database)
        {
            _comments = database.GetCollection<Comment>("Comments");
        }

        public async Task<CommentResponseDto?> GetCommentByIdAsync(string commentId)
        {
            var comment = await _comments.Find(c => c.Id == commentId).FirstOrDefaultAsync();
            if (comment == null) return null;

            return new CommentResponseDto(
                comment.Id!,
                comment.PostId,
                comment.AuthorId,
                comment.Content,
                comment.ParentCommentId,
                comment.CreatedAt
            );
        }

        public async Task<List<CommentResponseDto>> GetCommentsByPostIdAsync(string postId, int page = 1, int pageSize = 10)
        {
            var comments = await _comments
                .Find(c => c.PostId == postId)
                .SortByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return comments.Select(c => new CommentResponseDto(
                c.Id!,
                c.PostId,
                c.AuthorId,
                c.Content,
                c.ParentCommentId,
                c.CreatedAt
            )).ToList();
        }
    }
}