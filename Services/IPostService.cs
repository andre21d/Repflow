using Repflow.Api.DTOs;

namespace Repflow.Api.Services
{
    public interface IPostService
    {
        Task<PostResponseDto> CreatePostAsync(string userId, CreatePostDto dto);
        Task<List<PostResponseDto>> GetAllPostsAsync();
        Task<List<PostResponseDto>> GetFeedPostsAsync(string userId);
        Task<PostResponseDto?> GetPostByIdAsync(string id);
        Task<bool> DeletePostAsync(string postId, string userId);
        Task<bool> ToggleLikeAsync(string postId, string userId);
        Task<CommentResponseDto> AddCommentAsync(string postId, string userId, CreateCommentDto dto);
        Task<(bool IsSuccess, string? ErrorMessage, int StatusCode, List<PostResponseDto>? Posts)> GetCommunityPostsAsync(string communityId, string requestingUserId);
        Task<List<PostResponseDto>> GetUserPostsAsync(string userId, string requestingUserId);
    }
}