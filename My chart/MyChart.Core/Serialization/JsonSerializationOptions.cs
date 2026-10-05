using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyChart.Core.Serialization;

/// <summary>
/// T3.07: JSON, UTF-8, indented; numbers with round-trip "R" and invariant culture;
/// time = ISO-8601 UTC.
/// </summary>
public static class JsonSerializationOptions
{
    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            // Unknown fields ignored on load (default for System.Text.Json)
        };
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        o.Converters.Add(new InvariantDoubleConverter());
        o.Converters.Add(new UtcDateTimeOffsetConverter());
        return o;
    }
}

/// <summary>Numbers with "R" format, invariant culture.</summary>
file sealed class InvariantDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()!;
            return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        return reader.GetDouble();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value.ToString("R", CultureInfo.InvariantCulture));
    }
}

file sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString()!;
        return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
    }
}
