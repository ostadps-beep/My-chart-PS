using MyChart.Core.Plugins.Manifest;
using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.02 ManifestIO VERIFY — write/read round-trip; byte-stable second write; unknown prop E015.</summary>
public class ManifestIOTests
{
    private static ComponentManifest Sample()
        => new()
        {
            ComponentId = "TrendLine",
            Kind = "DataTool",
            Name = "Trend Line",
            Version = "1.0.0",
            ContractVersion = "1.0",
            ToolId = "Tool.TrendLine",
            TypeId = "drawing.TrendLine",
            IconKey = "Icon.TrendLine",
            Category = "Drawing",
            Anchors = 2,
            Active = true,
            Order = 10
        };

    [Fact]
    public void WriteThenRead_EqualsOriginal()
    {
        var m = Sample();
        var json = ManifestIO.Write(m);
        var back = ManifestIO.Read(json);

        Assert.Equal(m.ComponentId, back.ComponentId);
        Assert.Equal(m.Kind, back.Kind);
        Assert.Equal(m.Name, back.Name);
        Assert.Equal(m.Version, back.Version);
        Assert.Equal(m.ToolId, back.ToolId);
        Assert.Equal(m.TypeId, back.TypeId);
        Assert.Equal(m.IconKey, back.IconKey);
        Assert.Equal(m.Anchors, back.Anchors);
        Assert.Equal(m.Active, back.Active);
        Assert.Equal(m.Order, back.Order);
    }

    [Fact]
    public void WritingTwice_GivesIdenticalBytes()
    {
        var m = Sample();
        var a = ManifestIO.Write(m);
        var b = ManifestIO.Write(m);
        Assert.Equal(a, b);
        Assert.EndsWith("\n", a);
        Assert.DoesNotContain("\r", a);
    }

    [Fact]
    public void UnknownProperty_ThrowsE015()
    {
        var json = "{\n  \"componentId\": \"X\",\n  \"kind\": \"DataTool\",\n  \"name\": \"X\",\n  \"version\": \"1.0.0\",\n  \"contractVersion\": \"1.0\",\n  \"category\": \"Drawing\",\n  \"anchors\": 2,\n  \"active\": true,\n  \"order\": 0,\n  \"extraField\": 1\n}\n";
        var ex = Assert.Throws<ManifestException>(() => ManifestIO.Read(json));
        Assert.Equal(ErrorCodes.ManifestSchemaError, ex.Code);
    }

    [Fact]
    public void Sha256_NormalisesLineEndings()
    {
        var lf = "hello\nworld\n";
        var crlf = "hello\r\nworld\r\n";
        Assert.Equal(ManifestIO.Sha256Normalized(lf), ManifestIO.Sha256Normalized(crlf));
    }
}
