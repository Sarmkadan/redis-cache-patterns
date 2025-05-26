// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents a user in the system with authentication and profile information
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    [JsonIgnore]
    public List<Order> Orders { get; set; } = new();

    public string GetFullName() => $"{FirstName} {LastName}".Trim();

    public void UpdateProfile(string firstName, string lastName, string? phone = null, string? address = null)
    {
        FirstName = firstName ?? FirstName;
        LastName = lastName ?? LastName;
        if (phone != null) Phone = phone;
        if (address != null) Address = address;
    }

    public void SetLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public bool IsValidEmail()
    {
        return !string.IsNullOrWhiteSpace(Email) && Email.Contains("@");
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public override bool Equals(object? obj)
    {
        return obj is User user && user.Id == Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

public enum UserRole
{
    User = 0,
    Moderator = 1,
    Administrator = 2
}
