using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs
{
    // DTO طلب رفع صورة مفردة
    public record UploadSingleMediaDto(
        [Required(ErrorMessage = "يرجى اختيار ملف لتحديده")] 
        IFormFile File
    );

    // DTO طلب رفع صور/فيديوهات متعددة
    public record UploadMultipleMediaDto(
        [Required(ErrorMessage = "يرجى اختيار ملف واحد على الأقل")] 
        List<IFormFile> Files
    );

    // DTO استجابة الرفع المفرد
    public record MediaUploadResponseDto(
        string Url,
        string FileName
    );

    // DTO استجابة الرفع المتعدد
    public record MultipleMediaUploadResponseDto(
        int Count,
        List<string> Urls
    );
}