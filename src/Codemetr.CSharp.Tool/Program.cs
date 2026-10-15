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
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException)
        {
            await stderr.WriteLineAsync("codemetr-csharp: " + e.Message);
            await stderr.WriteLineAsync("Run 'codemetr-csharp --help' for usage.");
            return ExitCodes.InputError;
        }
        // A guard must never report success by crashing: every failure is an input or tool error.
        try { return await ExecuteAsync(options, stdout, stderr); }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            await stderr.WriteLineAsync("codemetr-csharp: " + Describe(e));
            return ExitCodes.InputError;
        }
    }

    static async Task<int> ExecuteAsync(CliOptions options, TextWriter stdout, TextWriter stderr)
    {
        var report = CodemetrService.Run(options.Command, options.Request);
        foreach (var warning in report.Warnings ?? []) await stderr.WriteLineAsync("codemetr-csharp: warning: " + warning);
        if (options.Format == CliOptions.Text) TextReport.Write(report, stdout);
        else await stdout.WriteLineAsync(JsonSerializer.Serialize(report, Codemetr.CSharp.CodemetrJson.Options));
        if (report.ParseErrors.Count > 0) return ExitCodes.InputError;
        return options.Command == CodemetrService.Check && !report.Passed ? ExitCodes.Findings : ExitCodes.Passed;
    }

    // Input problems are reported by their message; anything else names its exception type as well.
    static string Describe(Exception failure)
    {
        var cause = failure is AggregateException aggregate ? aggregate.Flatten().InnerExceptions[0] : failure;
        return cause is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException
            ? cause.Message
            : $"unexpected {cause.GetType().Name}: {cause.Message}";
    }
}
