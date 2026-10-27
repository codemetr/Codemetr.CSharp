# Inspect the 0.1.0 measurements

Use the published `Codemetr.CSharp.Tool` **0.1.0** and .NET 10. No tool source
checkout or local package feed is required. From this repository root:

```bash
codemetr-csharp --version
codemetr-csharp analyze src --root examples/metrics --format json
codemetr-csharp analyze src --root examples/metrics --mutation-report synthetic-mutation.json --format json
codemetr-csharp check src --root examples/metrics --include-metrics --format json
```

An existing global installation can be updated deliberately with
`dotnet tool update --global Codemetr.CSharp.Tool --version 0.1.0`. Respect a
consuming repository's pinned version; do not silently replace it. This example
opts into whole-partial-type checks in its own configuration. It changes no
production thresholds or baselines. The root repository's local tool manifest
remains pinned to 0.0.1 for its separate self guard; `dotnet tool run` from that
root therefore selects the old self guard, not this example's required version.

## What to inspect

- `types` has three declaration rows; `wholeTypes` combines the two `Ledger`
  parts and retains `LedgerPeer` as the second logical type.
- `Ledger.AddIfPositive` has cyclomatic complexity 2 and cognitive complexity 1.
  `ReadBalance` remains a separate executable member in the other partial file.
- The dependency graph contains `Ledger` → `LedgerPeer` → `Ledger`, including
  a type-cycle witness. Both types share a namespace, so this is not an
  inter-namespace cycle. The cycle is intentional teaching input, not a design
  recommendation or a runtime call trace.
- The two ordinary `Ledger` methods use its `balance` field and form one
  instance-state cohesion group. `LedgerPeer`, which has no explicit instance
  field and fewer than two ordinary instance methods, is not applicable.
- `completeness` discloses source-analysis limits; it does not certify that a
  real project, its package references or every build configuration was loaded.

The mutation file is **synthetic scoring input**, not the output of a Stryker
run on this sample. Its killed, survived and ignored statuses demonstrate a
50% imported score: one detected / two scored mutants. The ignored mutant is
excluded. Report identity and status counts remain visible; importing a report
does not run mutation tests or attest current source coverage. A pending mutant
or empty denominator would prevent a final score.

The advisory cycle, cognitive and cohesion measurements and imported mutation
score do not create new default failure thresholds. A passing `check` means the
configured static guard passed, not that the sample architecture or tests are
proven good. See [precise definitions](../docs/measurement-definitions.md).
