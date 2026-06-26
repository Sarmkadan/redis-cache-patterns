#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the generic repository pattern
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected List<T> _data = new();
    protected int _nextId = 1;
    protected readonly object _lock = new object();

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            var property = typeof(T).GetProperty("Id");
            if (property == null) return null;
            return _data.FirstOrDefault(x => (int?)property.GetValue(x) == id);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        lock (_lock)
        {
            return _data.ToList();
        }
    }

    /// <inheritdoc/>
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        lock (_lock)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null && idProperty.CanWrite)
            {
                idProperty.SetValue(entity, _nextId++);
            }
            _data.Add(entity);
        }
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        lock (_lock)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null) return entity;

            var id = (int?)idProperty.GetValue(entity);
            var existingIndex = _data.FindIndex(x => (int?)idProperty.GetValue(x) == id);
            if (existingIndex >= 0)
                _data[existingIndex] = entity;
        }
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null) return false;

            var entity = _data.FirstOrDefault(x => (int?)idProperty.GetValue(x) == id);
            return entity != null && _data.Remove(entity);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync()
    {
        lock (_lock)
        {
            return _data.Count;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(int id)
    {
        lock (_lock)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null) return false;
            return _data.Any(x => (int?)idProperty.GetValue(x) == id);
        }
    }
}
