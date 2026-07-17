#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace RedisCachePatterns.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="BusinessException"/> and its derived types.
/// </summary>
public static class BusinessExceptionJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
	};

	/// <summary>
	/// Converts a <see cref="BusinessException"/> to a JSON string representation.
	/// </summary>
	/// <param name="value">The exception to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the exception.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this BusinessException value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions)
			{
				WriteIndented = true,
			}
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Parses a JSON string to create a <see cref="BusinessException"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized exception, or null if the JSON is empty or whitespace.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static BusinessException? FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<BusinessException>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to parse a JSON string to create a <see cref="BusinessException"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized exception if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static bool TryFromJson(string json, out BusinessException? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		value = null;

		try
		{
			value = JsonSerializer.Deserialize<BusinessException>(json, _jsonOptions);
			return value is not null;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}