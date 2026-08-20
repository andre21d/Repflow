using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs
{
    public record CreatePostDto(
        [Required(ErrorMessage = "محتوى المنشور مطلوب")]
        [StringLength(2000, ErrorMessage = "لا يمكن أن يتجاوز النص 2000 حرف")]
        string Content,
        string? CommunityId,
        List<string>? MediaUrls
    );

    public record CreateSessionPostDto(
        [Required(ErrorMessage = "محتوى المنشور مطلوب")]
        [StringLength(2000, ErrorMessage = "لا يمكن أن يتجاوز النص 2000 حرف")]
        string Content,
        [Required] string UserSessionId,
        string? CommunityId,
        List<string>? MediaUrls
    );

    public record SessionPostResponseDto(
        string Id,
        string AuthorId,
        string? CommunityId,
        string UserSessionId,
        string Content,
        List<string> MediaUrls,
        int LikesCount,
        int CommentsCount,
        DateTime CreatedAt
    );

    public record UpdatePostDto(
        [Required(ErrorMessage = "محتوى المنشور مطلوب")]
        [StringLength(2000, ErrorMessage = "لا يمكن أن يتجاوز النص 2000 حرف")]
        string Content
    );

    public record PostResponseDto(
        string Id,
        string AuthorId,
        string? CommunityId,
        string Content,
        List<string> MediaUrls,
        int LikesCount,
        int CommentsCount,
        bool IsLikedByCurrentUser, 
        DateTime CreatedAt
    );

    public record CreateCommentDto(
        [Required(ErrorMessage = "نص التعليق مطلوب")]
        [StringLength(500, ErrorMessage = "لا يمكن أن يتجاوز التعليق 500 حرف")]
        string Content,
        string? ParentCommentId
    );

    public record CommentResponseDto(
        string Id,
        string PostId,
        string AuthorId,
        string Content,
        string? ParentCommentId,
        DateTime CreatedAt
    );
}