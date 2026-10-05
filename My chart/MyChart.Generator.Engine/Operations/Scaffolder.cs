using System.Text;
using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.03 Scaffolder — produces file contents for PLUGIN_COMPONENT_FOLDER_STANDARD.
/// Keys are relative paths under the component folder (or under Components/ for CodeTool).
/// </summary>
public static class Scaffolder
{
    /// <summary>
    /// Returns map of relativePath → file content (LF, no BOM).
    /// </summary>
    public static IReadOnlyDictionary<string, string> Build(ScaffoldRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.ComponentId))
            throw new ArgumentException("ComponentId required", nameof(req));

        var id = req.ComponentId;
        var name = string.IsNullOrWhiteSpace(req.Name) ? id : req.Name;
        var typeId = req.TypeId ?? $"drawing.{id}";
        var iconKey = req.IconKey ?? $"Icon.{id}";
        var toolId = req.ToolId ?? $"Tool.{id}";
        var anchors = req.Anchors;

        return req.Kind switch
        {
            ScaffoldKind.DataToolClick => BuildDataTool(id, name, typeId, iconKey, toolId, anchors, clickThenText: false),
            ScaffoldKind.DataToolClickThenText => BuildDataTool(id, name, typeId, iconKey, toolId, anchors: 1, clickThenText: true),
            ScaffoldKind.CodeToolClick => BuildCodeTool(id, name, typeId, iconKey, toolId, anchors),
            ScaffoldKind.IconVector => BuildIcon(id, name, iconKey),
            _ => throw new ArgumentOutOfRangeException(nameof(req))
        };
    }

    private static Dictionary<string, string> BuildDataTool(
        string id, string name, string typeId, string iconKey, string toolId, int anchors, bool clickThenText)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        var manifest = new ComponentManifest
        {
            ComponentId = id,
            Kind = "DataTool",
            Name = name,
            Version = "1.0.0",
            ContractVersion = "1.0",
            ToolId = toolId,
            TypeId = typeId,
            IconKey = iconKey,
            Category = "Drawing",
            Anchors = anchors,
            Active = true,
            Order = 0
        };
        files["Manifest.json"] = ManifestIO.Write(manifest);
        files["Definition.json"] = clickThenText
            ? DefinitionClickThenText()
            : DefinitionClickLine(anchors);
        files["Documentation.txt"] = $"# {name}\n\nScaffolded DataTool. Edit Definition.json as needed.\n";
        return files;
    }

    private static Dictionary<string, string> BuildCodeTool(
        string id, string name, string typeId, string iconKey, string toolId, int anchors)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        var manifest = new ComponentManifest
        {
            ComponentId = id,
            Kind = "CodeTool",
            Name = name,
            Version = "1.0.0",
            ContractVersion = "1.0",
            ToolId = toolId,
            TypeId = typeId,
            IconKey = iconKey,
            Category = "Drawing",
            Anchors = anchors,
            Active = true,
            Order = 0
        };
        files["Manifest.json"] = ManifestIO.Write(manifest);
        files["Documentation.txt"] = $"# {name}\n\nScaffolded CodeTool. Owner-edited sources: {id}Tool.cs, Painter, HitTester.\n";
        files[$"{id}Tool.cs"] = CodeToolCs(id, toolId, anchors);
        files[$"{id}Painter.cs"] = CodePainterCs(id, typeId);
        files[$"{id}HitTester.cs"] = CodeHitTesterCs(id, typeId);
        files[$"{id}Registration.g.cs"] = CodeRegistrationCs(id);
        return files;
    }

    private static Dictionary<string, string> BuildIcon(string id, string name, string iconKey)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var manifest = new ComponentManifest
        {
            ComponentId = id,
            Kind = "Icon",
            Name = name,
            Version = "1.0.0",
            ContractVersion = "1.0",
            IconKey = iconKey,
            Category = "Icon",
            Anchors = 0,
            Active = true,
            Order = 0
        };
        files["Manifest.json"] = ManifestIO.Write(manifest);
        files["Geometry.txt"] = "M2 2 L14 14\n";
        return files;
    }

    /// <summary>Minimal valid Definition: one Line across anchors 0..N-1 (N=2 uses 0-1).</summary>
    public static string DefinitionClickLine(int anchors)
    {
        if (anchors < 2) anchors = 2;
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"schema\": \"mychart.tooldef\",");
        sb.AppendLine("  \"vocabularyVersion\": \"1.0\",");
        sb.AppendLine($"  \"anchors\": {anchors},");
        sb.AppendLine("  \"workflow\": \"Click\",");
        sb.AppendLine("  \"shapes\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"kind\": \"Line\",");
        sb.AppendLine("      \"from\": { \"x\": { \"anchor\": 0 }, \"y\": { \"anchor\": 0 } },");
        sb.AppendLine($"      \"to\": {{ \"x\": {{ \"anchor\": {anchors - 1} }}, \"y\": {{ \"anchor\": {anchors - 1} }} }},");
        sb.AppendLine("      \"extend\": \"none\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString().Replace("\r\n", "\n");
    }

    public static string DefinitionClickThenText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"schema\": \"mychart.tooldef\",");
        sb.AppendLine("  \"vocabularyVersion\": \"1.0\",");
        sb.AppendLine("  \"anchors\": 1,");
        sb.AppendLine("  \"workflow\": \"ClickThenText\",");
        sb.AppendLine("  \"shapes\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"kind\": \"Text\",");
        sb.AppendLine("      \"at\": { \"x\": { \"anchor\": 0 }, \"y\": { \"anchor\": 0 } },");
        sb.AppendLine("      \"text\": \"text\",");
        sb.AppendLine("      \"size\": 12,");
        sb.AppendLine("      \"align\": \"left\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString().Replace("\r\n", "\n");
    }

    private static string CodeToolCs(string id, string toolId, int anchors)
    {
        return $$"""
            using MyChart.Core.Contracts.Plugins;
            using MyChart.Core.Models.Drawing;
            using MyChart.Core.Plugins.Base;

            namespace MyChart.Plugins.Components.{{id}};

            /// <summary>Scaffolded CodeTool — owner may edit.</summary>
            public sealed class {{id}}Tool : ClickToolBase
            {
                public override string ToolId => "{{toolId}}";
                public override int RequiredClicks => {{anchors}};

                public override ToolResult OnPointer(PointerEvent e) => ToolResult.None;
                public override ToolResult OnKey(KeyEvent e) => ToolResult.None;
            }

            """.Replace("\r\n", "\n");
    }

    private static string CodePainterCs(string id, string typeId)
    {
        return $$"""
            using MyChart.Core.Contracts.Plugins;
            using MyChart.Core.Models.Drawing;

            namespace MyChart.Plugins.Components.{{id}};

            /// <summary>Scaffolded painter — owner may edit.</summary>
            public sealed class {{id}}Painter : IDrawingPainter
            {
                public string TypeId => "{{typeId}}";

                public void Paint(DrawingObject obj, IRenderContext ctx)
                {
                    // Owner implements paint.
                }
            }

            """.Replace("\r\n", "\n");
    }

    private static string CodeHitTesterCs(string id, string typeId)
    {
        return $$"""
            using MyChart.Core.Contracts.Plugins;
            using MyChart.Core.Models.Drawing;

            namespace MyChart.Plugins.Components.{{id}};

            /// <summary>Scaffolded hit tester — owner may edit.</summary>
            public sealed class {{id}}HitTester : IDrawingHitTester
            {
                public string TypeId => "{{typeId}}";

                public HitResult HitTest(DrawingObject obj, double x, double y, IChartMapper mapper)
                    => HitResult.None;
            }

            """.Replace("\r\n", "\n");
    }

    private static string CodeRegistrationCs(string id)
    {
        return $$"""
            // <auto-generated> DO NOT EDIT
            using MyChart.Core.Contracts.Plugins;

            namespace MyChart.Plugins.Components.{{id}};

            /// <summary>Generated registration for {{id}}.</summary>
            public static class {{id}}Registration
            {
                public static void Register(IPluginHost host)
                {
                    // Wired by catalog generator (PG3.05).
                }
            }

            """.Replace("\r\n", "\n");
    }
}
