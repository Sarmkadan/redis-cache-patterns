// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Text;

namespace RedisCachePatterns.Formatters;

/// <summary>
/// XML output formatter with support for single objects and collections
/// Generates properly formatted XML with optional schema information
/// </summary>
public class XmlFormatter : IOutputFormatter
{
    public string ContentType => "application/xml";
    private readonly bool _includeDeclaration;
    private readonly bool _indent;

    public XmlFormatter(bool includeDeclaration = true, bool indent = true)
    {
        _includeDeclaration = includeDeclaration;
        _indent = indent;
    }

    public string Format(object data)
    {
        try
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new XmlSerializer(data.GetType());
                var settings = new XmlWriterSettings
                {
                    Indent = _indent,
                    OmitXmlDeclaration = !_includeDeclaration,
                    Encoding = Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(ms, settings))
                {
                    serializer.Serialize(writer, data);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
        catch (Exception ex)
        {
            return CreateErrorXml($"Serialization failed: {ex.Message}");
        }
    }

    public string Format<T>(IEnumerable<T> data)
    {
        try
        {
            var list = data.ToList();
            var sb = new StringBuilder();

            if (_includeDeclaration)
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            sb.AppendLine($"<{typeof(T).Name}Collection>");

            foreach (var item in list)
            {
                using (var ms = new MemoryStream())
                {
                    var serializer = new XmlSerializer(typeof(T));
                    var settings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        ConformanceLevel = ConformanceLevel.Fragment
                    };

                    using (var writer = XmlWriter.Create(ms, settings))
                    {
                        serializer.Serialize(writer, item);
                    }

                    var itemXml = Encoding.UTF8.GetString(ms.ToArray());
                    sb.Append(_indent ? "  " : "").AppendLine(itemXml);
                }
            }

            sb.AppendLine($"</{typeof(T).Name}Collection>");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return CreateErrorXml($"Collection serialization failed: {ex.Message}");
        }
    }

    private string CreateErrorXml(string message)
    {
        var sb = new StringBuilder();
        if (_includeDeclaration)
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

        sb.AppendLine("<Error>");
        sb.AppendLine($"  <Message>{XmlEscape(message)}</Message>");
        sb.AppendLine($"  <Timestamp>{DateTime.UtcNow:O}</Timestamp>");
        sb.AppendLine("</Error>");

        return sb.ToString();
    }

    private string XmlEscape(string text)
    {
        return text.Replace("&", "&amp;")
                  .Replace("<", "&lt;")
                  .Replace(">", "&gt;")
                  .Replace("\"", "&quot;")
                  .Replace("'", "&apos;");
    }
}
