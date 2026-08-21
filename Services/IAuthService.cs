using System.Threading.Tasks;
using Repflow.Api.DTOs; // تأكد أن اسم مجلد الـ DTOs مطابق للي عندك

namespace Repflow.Api.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);
        Task<string> AdminLoginAsync(LoginDto dto);
        Task<bool> VerifyEmailAsync(string token);  
        Task<bool> ForgotPasswordAsync(string email);   
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    }
}