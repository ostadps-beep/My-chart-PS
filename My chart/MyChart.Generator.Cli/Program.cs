namespace MyChart.Generator.Cli;

internal static class Program
{
    private static int Main(string[] args)
        => CliRunner.Run(args, Console.Out, Console.Error);
}
