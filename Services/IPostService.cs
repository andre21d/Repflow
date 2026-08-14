using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface IPostService
    {
        Task<PostResponseDto> CreatePostAsync(string userId, CreatePostDto dto);
        Task<List<PostResponseDto>> GetAllPostsAsync();
        Task<PostResponseDto?> GetPostByIdAsync(string id);
        Task<List<PostResponseDto>> GetFeedPostsAsync(string userId);
        Task<bool> DeletePostAsync(string postId, string userId);
        Task<bool> ToggleLikeAsync(string postId, string userId);
        Task<CommentResponseDto> AddCommentAsync(string postId, string userId, CreateCommentDto dto);
    }
}