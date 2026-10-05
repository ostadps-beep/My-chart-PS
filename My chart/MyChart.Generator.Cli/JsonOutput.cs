using System.Text.Json;
using System.Text.Json.Serialization;
using MyChart.Generator.Engine.Operations;

namespace MyChart.Generator.Cli;

/// <summary>PG3.08 JSON result shape: {ok, operation, plan, errors, warnings}.</summary>
public sealed class CliResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = "";

    [JsonPropertyName("plan")]
    public List<CliPlanItem> Plan { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<CliError> Errors { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();

    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }
}

public sealed class CliPlanItem
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
}

public sealed class CliError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(CliResult result)
        => JsonSerializer.Serialize(result, Options);

    public static CliResult FromTransaction(string operation, TransactionResult tx)
    {
        var r = new CliResult
        {
            Ok = tx.Ok,
            Operation = operation,
            ExitCode = tx.ExitCode
        };

        foreach (var e in tx.Plan.Entries)
        {
            r.Plan.Add(new CliPlanItem
            {
                Op = e.Op.ToString().ToLowerInvariant(),
                Path = e.Path
            });
        }

        if (!tx.Ok && tx.ErrorCode is not null)
        {
            r.Errors.Add(new CliError
            {
                Code = tx.ErrorCode,
                Message = tx.ErrorMessage ?? tx.ErrorCode
            });
            if (r.ExitCode == 0)
                r.ExitCode = 1;
        }

        return r;
    }
}
