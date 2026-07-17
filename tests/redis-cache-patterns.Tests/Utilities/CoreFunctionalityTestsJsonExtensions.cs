#nullable enable

using System;
using System.Text.Json;

namespace RedisCachePatterns.Tests.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extension methods for <see cref="CoreFunctionalityTests"/>.
/// </summary>
public static class CoreFunctionalityTestsJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <summary>
	/// Serializes the <see cref="CoreFunctionalityTests"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="JsonException">Thrown when serialization fails.</exception>
	public static string ToJson(this CoreFunctionalityTests value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
			: _jsonSerializerOptions;
		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="CoreFunctionalityTests"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized instance, or null if the JSON represents a null value.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static CoreFunctionalityTests? FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		return JsonSerializer.Deserialize<CoreFunctionalityTests>(json, _jsonSerializerOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="CoreFunctionalityTests"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized instance if successful; otherwise, null.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	public static bool TryFromJson(string json, out CoreFunctionalityTests? value)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		try
		{
			value = JsonSerializer.Deserialize<CoreFunctionalityTests>(json, _jsonSerializerOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}