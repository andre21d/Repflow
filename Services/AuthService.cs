using MongoDB.Driver;
using Repflow.Api.Models; // تأكد أن الـ namespace يطابق مجلد الـ Models عندك
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Repflow.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IConfiguration _configuration;

        // حقن الداتابيز والـ Configuration بالـ Constructor
        public AuthService(IMongoDatabase database, IConfiguration configuration)
        {
            _users = database.GetCollection<User>("Users");
            _configuration = configuration;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return "Email already exists.";
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = passwordHash
            };

            await _users.InsertOneAsync(newUser);
            return "Registration successful.";
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            // 1. البحث عن المستخدم بالداتابيز المحلية والتحقق من الباسوورد
            var user = await _users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return null; // تعني أن البيانات خاطئة
            }

            // 2. تجهيز الـ Claims الخاصة باليوزر (مهمة جداً لاحقاً لمعرفة من قام باللايك أو البوست بالـ Social Media)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            };

            // 3. قراءة وتشفير الـ Secret Key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. بناء التوكن بناءً على الدقائق (1440 دقيقة) كما حددتها بالـ JSON
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryInMinutes"])),
                signingCredentials: creds
            );

            // 5. إرجاع التوكن كـ نص مشفر
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}