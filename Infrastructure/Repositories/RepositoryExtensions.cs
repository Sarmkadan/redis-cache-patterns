#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Extension methods for <see cref="IRepository{T}"/> providing common repository operations.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Attempts to get an entity by its identifier, returning null if not found.
    /// This is an alias for <see cref="IRepository{T}.GetByIdAsync(int)"/> for convenience.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The identifier of the entity to retrieve.</param>
    /// <returns>The entity if found; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository, int id) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        return repository.GetByIdAsync(id);
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

    /// <summary>
    /// Returns all entities that satisfy the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">A function to test each entity for a condition.</param>
    /// <returns>A collection of entities matching the predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static async Task<IEnumerable<T>> WhereAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await repository.GetAllAsync();
        return all.Where(predicate);
    }

    /// <summary>
    /// Returns the single entity that satisfies the specified predicate, or throws if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">A function to test each entity for a condition.</param>
    /// <returns>The single matching entity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no entity or multiple entities match the predicate.</exception>
    public static async Task<T> SingleAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await repository.GetAllAsync();
        return all.Single(predicate);
    }

    /// <summary>
    /// Returns the single entity that satisfies the specified predicate, or null if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">A function to test each entity for a condition.</param>
    /// <returns>The single matching entity, or null if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when multiple entities match the predicate.</exception>
    public static async Task<T?> SingleOrDefaultAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await repository.GetAllAsync();
        return all.SingleOrDefault(predicate);
    }

    /// <summary>
    /// Returns the only entity of the specified type, or throws if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <returns>The only entity of type T.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no entity or multiple entities of type T exist.</exception>
    public static async Task<T> SingleAsync<T>(this IRepository<T> repository) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);

        var all = await repository.GetAllAsync();
        return all.Single();
    }

    /// <summary>
    /// Returns the only entity of the specified type, or null if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <returns>The only entity of type T, or null if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static async Task<T?> SingleOrDefaultAsync<T>(this IRepository<T> repository) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);

        var all = await repository.GetAllAsync();
        return all.SingleOrDefault();
    }
}