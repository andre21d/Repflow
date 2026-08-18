using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Repflow.Api.Services
{
    public class MediaService : IMediaService
    {
        private readonly IWebHostEnvironment _environment;
        
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private readonly string[] _allowedVideoExtensions = { ".mp4", ".mov", ".avi", ".mkv" };

        public MediaService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("empty file");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            bool isImage = _allowedImageExtensions.Contains(extension);
            bool isVideo = _allowedVideoExtensions.Contains(extension);

            if (!isImage && !isVideo)
                throw new InvalidOperationException("image or video only");

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public async Task<List<string>> SaveFilesAsync(List<IFormFile> files, string folderName)
        {
            var uploadedUrls = new List<string>();

            if (files == null || files.Count == 0)
                return uploadedUrls;

            foreach (var file in files)
            {
                var fileUrl = await SaveFileAsync(file, folderName);
                uploadedUrls.Add(fileUrl);
            }

            return uploadedUrls;
        }

        public bool DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            var relativePath = fileUrl.TrimStart('/');
            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRootPath, relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        }
    }
}