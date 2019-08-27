// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents a product in the inventory system
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public double Rating { get; set; } = 0.0;
    public int ReviewCount { get; set; } = 0;

    [JsonIgnore]
    public List<OrderItem> OrderItems { get; set; } = new();

    public bool IsLowStock() => StockQuantity <= ReorderLevel;

    public bool IsAvailable() => IsActive && StockQuantity > 0;

    public void UpdateStock(int quantity)
    {
        if (StockQuantity + quantity < 0)
            throw new InvalidOperationException("Cannot reduce stock below zero");
        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative");
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, string category, string? imageUrl = null)
    {
        Name = name ?? Name;
        Description = description ?? Description;
        Category = category ?? Category;
        if (imageUrl != null) ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRating(double rating, int reviewCount)
    {
        if (rating < 0 || rating > 5)
            throw new ArgumentException("Rating must be between 0 and 5");
        Rating = rating;
        ReviewCount = reviewCount;
    }

    public decimal CalculateDiscount(decimal discountPercent)
    {
        return Price * (1 - discountPercent / 100);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"{Name} (SKU: {Sku}) - ${Price:F2}";
}
