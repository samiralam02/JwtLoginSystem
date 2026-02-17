using MySql.Data.MySqlClient;
using JwtLoginSystem.Data;
using JwtLoginSystem.Models;

namespace JwtLoginSystem.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseContext _dbContext;

        public UserRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Check if user exists by email
        public async Task<bool> UserExistsAsync(string email)
        {
            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        // Save new user to database
        public async Task<int> CreateUserAsync(User user)
        {
            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO Users (Email, PasswordHash, FullName, DateOfBirth, CreatedAt)
                VALUES (@Email, @PasswordHash, @FullName, @DateOfBirth, @CreatedAt);
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            var userId = Convert.ToInt32(await command.ExecuteScalarAsync());
            return userId;
        }

        // Get user by email (for login)
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT Id, Email, PasswordHash, FullName, DateOfBirth FROM Users WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Email = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                FullName = reader.GetString(3),
                DateOfBirth = reader.GetDateTime(4)
            };
        }
    }
}