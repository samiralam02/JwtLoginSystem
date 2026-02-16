using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using JwtLoginSystem.Data;
using JwtLoginSystem.Models;
using JwtLoginSystem.Models.DTOs;
namespace JwtLoginSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly DatabaseContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthService(DatabaseContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            var today = DateTime.UtcNow;
            var age = today.Year - registerDto.DateOfBirth.Year;
            
            if (registerDto.DateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            if (age >= 65)
            {
                throw new InvalidOperationException("Registration is only allowed for users below 65 years of age");
            }

            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@Email", registerDto.Email);
            
            var userExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            if (userExists)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var insertQuery = @"
                INSERT INTO Users (Email, PasswordHash, FullName, DateOfBirth, CreatedAt) 
                VALUES (@Email, @PasswordHash, @FullName, @DateOfBirth, @CreatedAt)";

            using var insertCommand = new MySqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@Email", registerDto.Email);
            insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insertCommand.Parameters.AddWithValue("@FullName", registerDto.FullName);
            insertCommand.Parameters.AddWithValue("@DateOfBirth", registerDto.DateOfBirth);
            insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await insertCommand.ExecuteNonQueryAsync();

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
            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT Id, Email, PasswordHash, FullName, DateOfBirth FROM Users WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", loginDto.Email);

            using var reader = await command.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
            {
                return null; 
            }

            var user = new User
            {
                Id = reader.GetInt32(0),              
                Email = reader.GetString(1),          
                PasswordHash = reader.GetString(2),   
                FullName = reader.GetString(3),     
                DateOfBirth = reader.GetDateTime(4) 
            };
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                ExpiresAt = expiresAt
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

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
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}