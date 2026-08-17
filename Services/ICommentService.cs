using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface ICommentService
    {
        Task<CommentResponseDto?> GetCommentByIdAsync(string commentId);
        Task<List<CommentResponseDto>> GetCommentsByPostIdAsync(string postId, int page = 1, int pageSize = 10);
    }
}