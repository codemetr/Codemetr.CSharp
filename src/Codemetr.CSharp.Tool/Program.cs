using System.Text.Json;

namespace Codemetr.CSharp.Tool;

internal static class Program
{
    public static async Task<int> Main(string[] args) => await RunAsync(args, Console.In, Console.Out, Console.Error);

    public static async Task<int> RunAsync(string[] args, TextReader stdin, TextWriter stdout, TextWriter stderr)
    {
        if (args is ["--version"]) { await stdout.WriteLineAsync(CodemetrService.Version); return ExitCodes.Passed; }
        if (args is [] or ["--help"] or ["-h"]) { await stdout.WriteLineAsync(CliOptions.Usage); return args.Length == 0 ? ExitCodes.InputError : ExitCodes.Passed; }
        CliOptions options;
        try { options = CliOptions.Parse(args, stdin); }
        catch (Exception e) when (e is ArgumentException or IOException)
        {
            await stderr.WriteLineAsync("codemetr-csharp: " + e.Message);
            await stderr.WriteLineAsync("Run 'codemetr-csharp --help' for usage.");
            return ExitCodes.InputError;
        }
        try
        {
            var report = CodemetrService.Run(options.Command, options.Request);
            if (options.Format == CliOptions.Text) TextReport.Write(report, stdout);
            else await stdout.WriteLineAsync(JsonSerializer.Serialize(report, Codemetr.CSharp.CodemetrJson.Options));
            if (report.ParseErrors.Count > 0) return ExitCodes.InputError;
            return options.Command == CodemetrService.Check && !report.Passed ? ExitCodes.Findings : ExitCodes.Passed;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException)
        {
            await stderr.WriteLineAsync("codemetr-csharp: " + e.Message);
            return ExitCodes.InputError;
        }
    }
}
