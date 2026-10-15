namespace Codemetr.CSharp.Tests;

/// <summary>Scratch directory below the system temp path, removed on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "codemetr-tests-" + Guid.NewGuid().ToString("N"))).FullName;

    public string Write(string relative, string content)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
