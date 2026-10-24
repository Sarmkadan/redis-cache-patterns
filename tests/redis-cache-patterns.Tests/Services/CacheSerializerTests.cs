#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Security regression tests for <see cref="CacheSerializer"/>: a Redis payload must never be able to
/// choose the .NET type that gets instantiated during deserialization.
/// </summary>
public class CacheSerializerTests
{
    private const byte UncompressedHeader = 0;

    /// <summary>
    /// Verifies that a <c>$type</c> discriminator embedded in a cached payload is treated as ordinary data:
    /// the declared target type is returned and the type named by <c>$type</c> is never constructed.
    /// </summary>
    [Fact]
    public void Deserialize_WhenPayloadNamesGadgetType_DoesNotInstantiateNamedType()
    {
        GadgetDto.InstantiationCount = 0;
        var serializer = new CacheSerializer(NullLogger<CacheSerializer>.Instance);
        var gadgetTypeName = typeof(GadgetDto).AssemblyQualifiedName;
        var payload = BuildWirePayload($$"""{"$type":"{{gadgetTypeName}}","name":"payload"}""");

        var result = serializer.Deserialize<SafeDto>(payload);

        result.Should().NotBeNull();
        result.Should().BeOfType<SafeDto>();
        result!.Name.Should().Be("payload");
        GadgetDto.InstantiationCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that a <c>$type</c> pointing at a type that cannot be resolved does not change deserialization
    /// into the declared type, so attacker-controlled type names cannot break or redirect the read path.
    /// </summary>
    [Fact]
    public void Deserialize_WhenPayloadNamesUnknownType_ReturnsDeclaredType()
    {
        var serializer = new CacheSerializer(NullLogger<CacheSerializer>.Instance);
        var payload = BuildWirePayload("""{"$type":"System.Diagnostics.Process, System","name":"safe"}""");

        var result = serializer.Deserialize<SafeDto>(payload);

        result.Should().BeOfType<SafeDto>();
        result!.Name.Should().Be("safe");
    }

    /// <summary>
    /// Builds a payload in the wire format written by <see cref="CacheSerializer.Serialize{T}"/> for
    /// uncompressed data: one header byte followed by the UTF-8 JSON body.
    /// </summary>
    /// <param name="json">JSON body to store after the header.</param>
    /// <returns>Raw bytes as they would appear in Redis.</returns>
    private static byte[] BuildWirePayload(string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var payload = new byte[body.Length + 1];
        payload[0] = UncompressedHeader;
        Buffer.BlockCopy(body, 0, payload, 1, body.Length);
        return payload;
    }

    /// <summary>
    /// Declared target type used by the tests; it is the only type the caller asks for.
    /// </summary>
    public sealed class SafeDto
    {
        /// <summary>
        /// Gets or sets the name value.
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Stand-in for an attacker-chosen type; counts constructions so tests can prove it was never created.
    /// </summary>
    public sealed class GadgetDto
    {
        private static int _instantiationCount;

        /// <summary>
        /// Gets the number of times a <see cref="GadgetDto"/> has been constructed.
        /// </summary>
        public static int InstantiationCount
        {
            get => Volatile.Read(ref _instantiationCount);
            set => Volatile.Write(ref _instantiationCount, value);
        }

        /// <summary>
        /// Initializes a new instance and records the construction.
        /// </summary>
        public GadgetDto() => Interlocked.Increment(ref _instantiationCount);

        /// <summary>
        /// Gets or sets the name value.
        /// </summary>
        public string? Name { get; set; }
    }
}
