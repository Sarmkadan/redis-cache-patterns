#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Tests.Domain;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="Product"/>.
/// </summary>
public static class ProductJsonExtensions
{
	/// <summary>
	/// JSON serializer options with camelCase naming convention for consistent serialization.
	/// </summary>
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <summary>
	/// Serializes a <see cref="Product"/> instance to a JSON string using camelCase property naming.
	/// </summary>
	/// <param name="value">The instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the instance.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this Product value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
			: _jsonSerializerOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="Product"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>A deserialized instance, or <see langword="null"/> if the JSON represents a null value.</returns>
	/// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/>, empty, or whitespace.</exception>
	/// <exception cref="JsonException">The JSON is invalid or cannot be deserialized to a <see cref="Product"/> instance.</exception>
	public static Product? FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		return JsonSerializer.Deserialize<Product>(json, _jsonSerializerOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="Product"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized instance if successful, otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if deserialization succeeded; otherwise <see langword="false"/>.</returns>
	/// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/>, empty, or whitespace.</exception>
	public static bool TryFromJson(string json, out Product? value)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		value = JsonSerializer.Deserialize<Product>(json, _jsonSerializerOptions);
		return value is not null;
	}
}