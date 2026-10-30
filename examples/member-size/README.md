# Member-size example (development feature)

This example requires the member-size feature. Published tool 0.1.0 does not
support the new rule fields. Do not run this configuration with that version,
install from a local feed, or silently change a repository's pinned tool.
After an authorised release containing the feature, use that published version.

From the Codemetr.CSharp repository root, with the feature-capable tool selected:

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
