#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Service handling user operations with integrated caching strategy
/// </summary>
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<UserService> _logger;
    private const string USER_CACHE_KEY = "user:{0}";
    private const string USER_LIST_CACHE_KEY = "users:all";
    private const string ACTIVE_USERS_CACHE_KEY = "users:active";

    public UserService(IUserRepository repository, ICacheService cache, ILogger<UserService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        var cacheKey = string.Format(USER_CACHE_KEY, userId);

        // Cache-aside pattern: try cache first, then database
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(userId),
            TimeSpan.FromHours(1)
        );
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var cacheKey = $"user:username:{username}";
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByUsernameAsync(username),
            TimeSpan.FromHours(1)
        );
    }

    public async Task<User> CreateUserAsync(User user)
    {
        if (!user.IsValidEmail())
            throw new ValidationException("Invalid email address");

        var existingUser = await _repository.GetByUsernameAsync(user.Username);
        if (existingUser != null)
            throw new ValidationException("Username already exists");

        // Write-through: save to DB and then cache
        var createdUser = await _cache.WriteAsync(
            string.Format(USER_CACHE_KEY, user.Id),
            user,
            async () => await _repository.AddAsync(user),
            TimeSpan.FromHours(1)
        );

        // Invalidate list cache
        await InvalidateUserListCacheAsync();

        _logger.LogInformation("User created: {UserId}", createdUser.Id);
        return createdUser;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        var existingUser = await GetUserByIdAsync(user.Id);
        if (existingUser == null)
            throw new NotFoundException(nameof(User), user.Id);

        var updated = await _cache.WriteAsync(
            string.Format(USER_CACHE_KEY, user.Id),
            user,
            async () => await _repository.UpdateAsync(user),
            TimeSpan.FromHours(1)
        );

        // Invalidate related caches
        await _cache.RemoveAsync($"user:username:{existingUser.Username}");
        await InvalidateUserListCacheAsync();

        _logger.LogInformation("User updated: {UserId}", user.Id);
        return updated;
    }

    public async Task<bool> DeactivateUserAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            return false;

        user.Deactivate();
        await UpdateUserAsync(user);

        _logger.LogInformation("User deactivated: {UserId}", userId);
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            return false;

        var deleted = await _repository.DeleteAsync(userId);
        if (deleted)
        {
            // Invalidate caches
            await _cache.RemoveAsync(string.Format(USER_CACHE_KEY, userId));
            await _cache.RemoveAsync($"user:username:{user.Username}");
            await InvalidateUserListCacheAsync();
            _logger.LogInformation("User deleted: {UserId}", userId);
        }

        return deleted;
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var result = await _cache.GetOrLoadAsync(
            ACTIVE_USERS_CACHE_KEY,
            async () => await _repository.GetActiveUsersAsync(),
            TimeSpan.FromMinutes(30)
        );
        return result ?? [];
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        var cacheKey = $"users:role:{role}";
        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByRoleAsync(role),
            TimeSpan.FromMinutes(30)
        );
        return result ?? [];
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        var result = await _cache.GetOrLoadAsync(
            USER_LIST_CACHE_KEY,
            async () => await _repository.GetAllAsync(),
            TimeSpan.FromMinutes(30)
        );
        return result ?? [];
    }

    public async Task<bool> AuthenticateAsync(string username, string passwordHash)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null || user.PasswordHash != passwordHash)
            return false;

        user.SetLastLogin();
        await UpdateUserAsync(user);
        return true;
    }

    private async Task InvalidateUserListCacheAsync()
    {
        await _cache.RemoveAsync(USER_LIST_CACHE_KEY);
        await _cache.RemoveAsync(ACTIVE_USERS_CACHE_KEY);
        await _cache.RemoveByPatternAsync("users:role:*");
    }
}
