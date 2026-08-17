 using MongoDB.Driver;
using Repflow.Api.Models; 
using Repflow.Api.DTOs;        
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

        public AuthService(IMongoDatabase database, IConfiguration configuration)
        {
            _users = database.GetCollection<User>("Users");
            _configuration = configuration;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUsername = await _users
                .Find(u => u.Username.ToLower() == dto.Username.ToLower())
                .FirstOrDefaultAsync();

            if (existingUsername != null) 
                return "Username already exists.";

            var existingEmail = await _users
                .Find(u => u.Email.ToLower() == dto.Email.ToLower())
                .FirstOrDefaultAsync();

            if (existingEmail != null) 
                return "Email already exists.";

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                IsEmailVerified = true,
                EmailVerificationToken = Guid.NewGuid().ToString()
            };

            await _users.InsertOneAsync(newUser);
            
            return "Registration successful. Please check your email to verify your account.";
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return "INVALID_CREDENTIALS"; 
            }

            if (!user.IsEmailVerified)
            {
                return "EMAIL_NOT_VERIFIED";
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username) 
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "1440")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        public async Task<bool> VerifyEmailAsync(string token)
        {
            var user = await _users.Find(u => u.EmailVerificationToken == token).FirstOrDefaultAsync();
            if (user == null) return false;

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null; // تنظيف التوكن بعد الاستخدام

            var result = await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null) return false; 

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(2); // التوكن صالح لساعتين

            await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _users.Find(u => u.PasswordResetToken == dto.Token && u.ResetTokenExpiry > DateTime.UtcNow).FirstOrDefaultAsync();
            if (user == null) return false; 

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null; 
            user.ResetTokenExpiry = null;

            var result = await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return result.ModifiedCount > 0;
        }
    }
}