using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using JwtLoginSystem.Models;
using JwtLoginSystem.Models.DTOs;
using JwtLoginSystem.Repositories;

namespace JwtLoginSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            var today = DateTime.UtcNow;
            var age = today.Year - registerDto.DateOfBirth.Year;
            if (registerDto.DateOfBirth.Date > today.AddYears(-age)) age--;

            if (age >= 65)
                throw new InvalidOperationException("Registration is only allowed for users below 65 years of age");

            var userExists = await _userRepository.UserExistsAsync(registerDto.Email);
            if (userExists)
                throw new InvalidOperationException("User with this email already exists");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                FullName = registerDto.FullName,
                DateOfBirth = registerDto.DateOfBirth
            };

            await _userRepository.CreateUserAsync(user);

            return new AuthResponseDto
            {
                Token = string.Empty,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                ExpiresAt = DateTime.MinValue
            };
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);

            if (user == null)
                return null; 

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return null; 

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FullName", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}