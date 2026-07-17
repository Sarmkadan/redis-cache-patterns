using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Extension methods that add convenient, higher-level operations to <see cref="UserRepository"/>.
/// </summary>
public static class UserRepositoryExtensions
{
    /// <summary>
    /// Retrieves a user by its username and throws a <see cref="BusinessException"/> if the user does not exist.
    /// </summary>
    /// <param name="repository">The repository instance.</param>
    /// <param name="username">The username to look up.</param>
    /// <returns>The matching <see cref="User"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="username"/> is <c>null</c> or empty.</exception>
    /// <exception cref="BusinessException">Thrown when no user with the supplied username exists.</exception>
    public static async Task<User> GetByUsernameOrThrowAsync(this UserRepository repository, string username)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrEmpty(username);

        return await repository.GetByUsernameAsync(username).ConfigureAwait(false)
            ?? throw new BusinessException($"User with username '{username}' not found.");
    }

    /// <summary>
    /// Retrieves a user by its email address and throws a <see cref="BusinessException"/> if the user does not exist.
    /// </summary>
    /// <param name="repository">The repository instance.</param>
    /// <param name="email">The email address to look up.</param>
    /// <returns>The matching <see cref="User"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="email"/> is <c>null</c> or empty.</exception>
    /// <exception cref="BusinessException">Thrown when no user with the supplied email exists.</exception>
    public static async Task<User> GetByEmailOrThrowAsync(this UserRepository repository, string email)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrEmpty(email);

        return await repository.GetByEmailAsync(email).ConfigureAwait(false)
            ?? throw new BusinessException($"User with email '{email}' not found.");
    }

    /// <summary>
    /// Returns the usernames of all active users.
    /// </summary>
    /// <param name="repository">The repository instance.</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> containing the usernames of active users.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <c>null</c>.</exception>
    public static async Task<IReadOnlyList<string>> GetActiveUsernamesAsync(this UserRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var users = await repository.GetActiveUsersAsync().ConfigureAwait(false);
        return users.Select(u => u.Username).ToArray();
    }

    /// <summary>
    /// Retrieves users that belong to the specified role name.
    /// </summary>
    /// <param name="repository">The repository instance.</param>
    /// <param name="roleName">The name of the role (case-insensitive).</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of users that have the given role.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="roleName"/> is <c>null</c> or empty.
    /// </exception>
    public static async Task<IReadOnlyList<User>> GetByRoleAsync(this UserRepository repository, string roleName)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var role))
            throw new ArgumentException($"'{roleName}' is not a valid role.", nameof(roleName));

        var users = await repository.GetByRoleAsync(role).ConfigureAwait(false);
        return users.ToArray();
    }
}