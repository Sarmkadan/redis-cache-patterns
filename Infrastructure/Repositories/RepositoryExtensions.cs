#nullable enable

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Extension methods for <see cref="Repository{T}"/> providing common repository operations
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Attempts to get an entity by its identifier, returning null if not found instead of throwing.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The identifier of the entity to retrieve.</param>
    /// <returns>The entity if found; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository, int id) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        return await repository.GetByIdAsync(id);
    }

    /// <summary>
    /// Determines whether any entity exists in the repository that satisfies the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">A function to test each entity for a condition.</param>
    /// <returns><see langword="true"/> if any entities match the predicate; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static async Task<bool> AnyAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await repository.GetAllAsync();
        return all.Any(predicate);
    }

    /// <summary>
    /// Returns the first entity that satisfies the specified predicate, or <see langword="null"/> if no such entity exists.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">A function to test each entity for a condition.</param>
    /// <returns>The first matching entity; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await repository.GetAllAsync();
        return all.FirstOrDefault(predicate);
    }
}