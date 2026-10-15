namespace Codemetr.CSharp;

/// <summary>
/// SDK-style projects declare these global usings in generated obj files, which are never analysed.
/// Supplying them keeps common BCL names bindable; duplicates with explicit usings are harmless here.
/// </summary>
internal static class ImplicitUsings
{
    public const string Source = """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Net.Http;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """;
}
