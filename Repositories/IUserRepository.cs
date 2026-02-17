using JwtLoginSystem.Models;

namespace JwtLoginSystem.Repositories
{
    public interface IUserRepository
    {
        Task<bool> UserExistsAsync(string email);
        Task<int> CreateUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
    }
}