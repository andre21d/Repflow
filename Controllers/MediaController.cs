using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repflow.Api.DTOs;
using Repflow.Api.Services;

namespace Repflow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] UploadSingleMediaDto dto)
        {
            try
            {
                var fileUrl = await _mediaService.SaveFileAsync(dto.File, "profiles");
                var fullUrl = $"{Request.Scheme}://{Request.Host}{fileUrl}";

                return Ok(new MediaUploadResponseDto(fullUrl, dto.File.FileName));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload-post-media")]
        public async Task<IActionResult> UploadPostMedia([FromForm] IFormFileCollection files)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { message = "يرجى اختيار ملف واحد على الأقل" });
                }

                var fileList = files.ToList();
                var fileUrls = await _mediaService.SaveFilesAsync(fileList, "posts");
                var fullUrls = fileUrls.Select(url => $"{Request.Scheme}://{Request.Host}{url}").ToList();

                return Ok(new MultipleMediaUploadResponseDto(fullUrls.Count, fullUrls));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}