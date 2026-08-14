using System.ComponentModel.DataAnnotations;

namespace Repflow.Api.DTOs
{
    public record RegisterDto(
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "يجب أن يكون الاسم بين 3 و 20 حرف")]
        string Username,
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        string Email,
        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "يجب أن لا تقل كلمة المرور عن 6 أحرف")]
        string Password
    );

    public record LoginDto(
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        string Email,
        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        string Password
    );

    public record AuthResponseDto(string Token, string Username, string Email);

    public record VerifyEmailDto(
        [Required(ErrorMessage = "رمز التأكيد مطلوب")]
        string Token
    );

    public record ForgotPasswordDto(
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        string Email
    );

    public record ResetPasswordDto(
        [Required(ErrorMessage = "رمز إعادة التعيين مطلوب")]
        string Token,
        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "يجب أن لا تقل كلمة المرور عن 6 أحرف")]
        string NewPassword
    );
}