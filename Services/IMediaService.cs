using Microsoft.AspNetCore.Http;

namespace Repflow.Api.Services
{
    public interface IMediaService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        Task<List<string>> SaveFilesAsync(List<IFormFile> files, string folderName);
        bool DeleteFile(string fileUrl);
    }
}