#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="CacheAsideExample"/> instances
/// using System.Text.Json.
/// </summary>
public static class CacheAsideExampleJsonExtensions
{
	private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	/// <summary>
	/// Serializes a <see cref="CacheAsideExample"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The cache aside example to serialize. Must not be null.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the cache aside example.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this CacheAsideExample value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_options) { WriteIndented = true }
			: _options;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="CacheAsideExample"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be null or empty.</param>
	/// <returns>The deserialized cache aside example, or null if the JSON is empty or whitespace.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static CacheAsideExample? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<CacheAsideExample>(json, _options);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="CacheAsideExample"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be null or empty.</param>
	/// <param name="value">Receives the deserialized cache aside example if successful, otherwise null.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
	public static bool TryFromJson(string json, out CacheAsideExample? value)
	{
		ArgumentNullException.ThrowIfNull(json);
		if (string.IsNullOrWhiteSpace(json))
		{
			throw new ArgumentException(
				"JSON string cannot be empty or whitespace.",
				nameof(json));
		}

		try
		{
			value = JsonSerializer.Deserialize<CacheAsideExample>(json, _options);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}