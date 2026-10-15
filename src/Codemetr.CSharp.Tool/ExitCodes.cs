namespace Codemetr.CSharp.Tool;

/// <summary>Process exit codes, the same convention as the repository guards: 0 clean, 1 findings, 2 input or usage error.</summary>
internal static class ExitCodes
{
    public const int Passed = 0;
    public const int Findings = 1;
    public const int InputError = 2;
}
