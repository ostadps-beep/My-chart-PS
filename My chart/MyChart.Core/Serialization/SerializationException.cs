namespace MyChart.Core.Serialization;

/// <summary>Thrown when a category major version is newer than supported.</summary>
public sealed class SerializationException : Exception
{
    public SerializationException(string message) : base(message) { }
    public SerializationException(string message, Exception inner) : base(message, inner) { }

    public static SerializationException NewerMajorVersion(string schema, int found, int supported)
        => new($"Schema '{schema}' major version {found} is newer than supported {supported}.");
}
