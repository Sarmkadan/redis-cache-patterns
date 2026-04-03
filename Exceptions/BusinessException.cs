#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Exceptions;

/// <summary>
/// Base exception for business logic errors
/// </summary>
public class BusinessException : Exception
{
    public string? ErrorCode { get; set; }

    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string resourceType, int id) : base($"{resourceType} with ID {id} not found", "NOT_FOUND")
    {
    }

    public NotFoundException(string message) : base(message, "NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a validation error occurs
/// </summary>
public class ValidationException : BusinessException
{
    public Dictionary<string, List<string>> Errors { get; set; } = new();

    public ValidationException(string message) : base(message, "VALIDATION_ERROR")
    {
    }

    public ValidationException(Dictionary<string, List<string>> errors) : base("Validation errors occurred", "VALIDATION_ERROR")
    {
        Errors = errors;
    }

    public void AddError(string field, string error)
    {
        if (!Errors.ContainsKey(field))
            Errors[field] = new List<string>();
        Errors[field].Add(error);
    }
}

/// <summary>
/// Exception thrown when insufficient inventory is available
/// </summary>
public class InsufficientInventoryException : BusinessException
{
    public int Requested { get; set; }
    public int Available { get; set; }

    public InsufficientInventoryException(int requested, int available)
        : base($"Insufficient inventory. Requested: {requested}, Available: {available}", "INSUFFICIENT_INVENTORY")
    {
        Requested = requested;
        Available = available;
    }
}

/// <summary>
/// Exception thrown when concurrent access conflicts occur
/// </summary>
public class ConcurrencyException : BusinessException
{
    public ConcurrencyException(string message) : base(message, "CONCURRENCY_CONFLICT")
    {
    }
}
