// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entities with specialized queries
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
    {
        lock (_lock)
        {
            return _data.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        lock (_lock)
        {
            return _data.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        lock (_lock)
        {
            return _data.Where(u => u.IsActive).ToList();
        }
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
    {
        lock (_lock)
        {
            return _data.Where(u => u.Role == role).ToList();
        }
    }
}
