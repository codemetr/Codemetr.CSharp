# Member-size example (0.2.0)

Use the published `Codemetr.CSharp.Tool` **0.2.0** and .NET 10. Tool 0.1.0 does
not support the new rule fields. Do not silently change a repository's pinned tool
or install from a local package feed. For an approved global installation:

```bash
dotnet tool install --global Codemetr.CSharp.Tool --version 0.2.0
```

From the Codemetr.CSharp repository root, with version 0.2.0 selected:

```bash
codemetr-csharp analyze --root examples/member-size --format json --top 2
codemetr-csharp analyze --root examples/member-size --format text --top 2
codemetr-csharp check --root examples/member-size --include-metrics
```

Paths are relative to --root. Accumulate(int) has 9 body code lines, 9 statements,
cyclomatic complexity 1 and cognitive complexity 0. Short(int) has one code line
and one statement. The long flat method ranks first in memberHotspots despite its
low complexity. Both memberCodeLines and memberStatements exceed this example's
opt-in limit of 8; analyze exits 0, check exits 1. Equal-to-limit values pass.
The example changes no production configuration or baseline.

These are static source measurements. There is no measured test coverage here;
absence of coverage is not zero coverage. Cover joining is a separate planned
feature with explicit identity and source-revision checks.
