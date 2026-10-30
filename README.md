# Codemetr.CSharp

[![ci](https://github.com/codemetr/Codemetr.CSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/codemetr/Codemetr.CSharp/actions/workflows/ci.yml)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/f2556b0ce71d40fb80dee252a944ca53)](https://app.codacy.com/gh/codemetr/Codemetr.CSharp/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

C# code metrics per **type and member**, built on Roslyn, with a configurable guard and a
ratchet baseline. One JSON report per run, stable rule ids and exit codes, so the same command
works on a developer machine, in a pre-commit hook and in CI.

| Package | Content |
| --- | --- |
| `Codemetr.CSharp` | Library: analyser, rules, guard evaluation, baseline |
| `Codemetr.CSharp.Tool` | .NET tool `codemetr-csharp` |

Requires .NET 10.

Install version **0.2.0** from nuget.org without a source checkout. See
[branch protection and releases](.github/RELEASING.md) for the manual publication
workflow. Tool and marketplace-plugin versions are independent.

## Install and run

```bash
dotnet tool install --global Codemetr.CSharp.Tool --version 0.2.0

codemetr-csharp analyze src --format text          # metrics + hotspots, always exit 0 unless input error
codemetr-csharp check src                          # guard, JSON report, exit 1 on findings
codemetr-csharp check --type OrderService          # one class (simple, full or suffix name)
codemetr-csharp baseline                           # first run: record current debt; later: only tighten it
git -c core.quotePath=false diff --name-only --no-renames HEAD \
  | codemetr-csharp check --files-from -           # quick feedback on changed files
```

| Exit code | Meaning |
| --- | --- |
| 0 | passed |
| 1 | new or worsened violations, or a baseline entry that must be tightened |
| 2 | input or usage error: syntax error in a source, missing path, invalid configuration |

The exit code is the verdict. With exit 2, stdout may be empty (usage errors) or carry a report
(syntax errors). A source with a syntax error is reported and never measured, so a broken file
cannot produce optimistic metrics.

## Logical types and additional advisory measurements

Version 0.1.0 adds whole-partial-type measurements and analysis limits,
directed dependencies and cycle witnesses, per-member cognitive complexity,
state-based cohesion groups, and explicit Stryker JSON score import:

```bash
codemetr-csharp analyze src --mutation-report reports/mutation.json
bash scripts/check-metrics.sh  # self guard using the pinned published NuGet tool
```

Definitions, supported syntax, scope restrictions and the opt-in partial guard are
specified in [measurement definitions](docs/measurement-definitions.md). New metrics
are advisory; existing declaration reports, baseline identities and default guard
thresholds remain. Use the [runnable examples](examples/README.md) to inspect these report fields.
The pinned 0.0.1 self guard is a separate quality check of this repository; it
does not exercise the new measurements and is not upgraded by installing 0.2.0.

## Member size and hotspots (0.2.0)

Version 0.2.0 adds `members[].codeLines`, `members[].statementCount` and
structured `memberHotspots` in JSON plus a separate text list. Existing `lines`
retains its declaration-span meaning. `--top` limits each type/member list;
member ordering prioritises body code lines, then statements and complexity.
New rules `maxMemberCodeLines` and `maxMemberStatements` are opt-in and disabled
by default. Older 0.1.0 does not accept them; upgrade consuming pins deliberately.
See the [runnable member-size example](examples/member-size/README.md) and
[precise definitions](docs/measurement-definitions.md#member-size).
No test coverage is inferred from static metrics.

## Metrics

Each type declaration is measured on its own, nested types included. Members belong to the
type that declares them and carry their parameter types, so overloads stay apart (`Save(Order)`,
`.ctor(int, string)`, `Total.get`). Local functions are separate members
(`Method(int).Local(string)`), lambdas count toward the member that contains them.

| Metric | Level | Definition |
| --- | --- | --- |
| Cyclomatic complexity | member | 1 + `if`, loops, `catch`, `case` labels (not `default`), switch arms (not a trailing `_`), `?:`, `?.`, `&&`, `\|\|`, `??`, `??=`, `when`, `and`/`or` patterns |
| Nesting depth | member | deepest control-flow nesting; body statements are depth 0, `else if` stays flat |
| Parameters | member | declared parameters |
| Maintainability index | type | `max(0, (171 - 5.2 ln V - 0.23 CC - 16.2 ln LOC) * 100 / 171)`, V = Halstead volume (token approximation), CC = sum of member complexities, LOC = lines with code tokens other than braces |
| Class coupling | type | distinct types referenced by the type's own code: declarations, `new`, attributes, static and extension calls, and the receiver type of instance member access. Types with a C# keyword (`int`, `string`, `object`, ...), `Nullable<T>`, tuples, type parameters, `nameof` operands and the type itself do not count; framework types such as `IEnumerable<T>` or `DateTime` do. The report lists the names. |
| Depth of inheritance | class | base classes above the class, `System.Object` excluded |
| Size and surface | type | line span, constructor arity (classes only), public methods, instance fields |

Coupling and inheritance are resolved with a semantic model over the `context` directories
(default: the whole root) plus the running .NET platform. Types from other assemblies (NuGet packages, other projects) cannot be
resolved; they still count and appear with a leading `?`, for example `?BackgroundService`.
An unresolved base class counts as one level, unless its name follows the interface convention
(`I` + upper-case letter). A type declared twice in the context (not as `partial`) is reported in
`warnings`, because Roslyn merges such declarations and their metrics mix.

## Configuration

`codemetr.json` in the root (or `--config FILE`). Every value is optional; `//` comments are
allowed, unknown members are rejected.

```jsonc
{
  "rules": {
    "maxTypeLines": 600,
    "maxConstructorParameters": 6,
    "maxPublicMethods": 10,
    "maxInstanceFields": 8,
    "maxCyclomaticComplexity": 25,
    "minMaintainabilityIndex": 15,
    "maxClassCoupling": 40,
    "maxDepthOfInheritance": 3,
    "maxParameters": 7,
    "maxNestingDepth": 4,
    "bannedTypeNameSuffixes": ["Helper", "Helpers", "Util", "Utils", "Utilities", "Manager", "Processor"]
  },
  "overrides": [
    { "files": "src/Legacy/**", "rules": { "maxCyclomaticComplexity": 40 } },
    { "type": "Shop.Orders.OrderImporter", "rules": { "maxClassCoupling": 60 } }
  ],
  "exclude": ["**/Migrations/**"],
  "context": ["."]
}
```

The values shown are the defaults. Limits are inclusive: a value equal to the limit passes.
`context` lists the directories compiled for type resolution on every run, also when only a few
files are measured; coupling and inheritance therefore come out the same for a changed-files run
and a full run. In a repository with several independent code trees, narrow it to the tree that
is measured, so that equally named types of another tree do not interfere.
Overrides apply in order and later ones win. `bin`, `obj`, `.git`, `node_modules`, `*.g.cs`,
`*.designer.cs` and files whose first lines carry the `<auto-generated>` header comment are always
skipped. A `[GeneratedCode]` attribute does not exclude a file.

To measure tests with other limits, prefer an override such as
`{ "files": "tests/**", "rules": { "maxPublicMethods": 40 } }` to `exclude`: excluded code is
invisible to the guard, an override keeps it under the ratchet.

## Baseline

The first `codemetr-csharp baseline` writes `codemetr.baseline.json` with the current violations
and their values; record it from a full run. `check` then reports each violation as `new`,
`worsened` or `baselined`; only the first two fail. A recorded violation that was fixed, improved
or moved to another file is reported as `stale` and fails until the baseline is updated.

Once the file exists, `baseline` **only tightens** it: fixed entries are removed, improved values
lowered, moved entries re-pointed. New or worse violations are refused (exit 1, file unchanged),
so following the tool's own advice can never accept new debt. Accepting new debt is a deliberate
decision outside the tool (re-creating the baseline); in review it shows up as added entries in
the baseline diff, which is what code-owner review of `codemetr.baseline.json` has to catch.

Only a proven fix removes an entry. When a recorded violation is gone, codemetr asks why:

| Situation | Result |
| --- | --- |
| the member now lives in a file that is never measured (excluded, `<auto-generated>` header, `.g.cs`, `bin`/`obj`), e.g. a generated `partial` part | *no longer measured*: fails, never removed by `baseline` |
| the recorded file still exists but is no longer measured | *no longer measured*: fails, never removed |
| file measured, no inactive `#if` code | fixed: `stale`, removed by `baseline` |
| file measured but contains inactive `#if` code, or the type has a part that is never measured | unverified: entry kept, warning |
| file deleted, type found in a measured file of the run, or nowhere, in a run over its directory | fixed (or matched as moved) |
| file deleted, type only in code that is never measured | *no longer measured* |
| file deleted, file-list run, or type in a file outside the run | unverified until a run measures it |

To find moved code, codemetr scans the C# files below the root once by syntax, including
`bin`/`obj` and generated files, but only when a recorded violation has disappeared. A member in
hidden code is matched by its name, whatever the spelling of its parameter types. Folders that are
symbolic links are not followed; while the root contains one, entries of deleted files stay
unverified.

`baseline` writes every safe update even when some entries are refused, so one deliberately
excluded file does not block tightening elsewhere; the run still fails while anything is refused.

A violation counts as moved, not new, when its rule, type and member equal a recorded entry whose
file no longer exists and its value is not worse. Top-level programs (type `Program`) have no
identity across files and are never matched as moved. Pass both the old and the new path (for git:
`--no-renames`) when checking changed files only.

Members are identified by their parameter types as written, so `M(string)` and `M(string?)` are
different members: changing a signature reports the old entry as stale and the member as new.

Staleness and recording are limited to the files and types of the run: checking or recording
only changed files leaves the entries of all other files untouched.

## Report

```json
{
  "schema": 1,
  "tool": "codemetr-csharp",
  "command": "check",
  "passed": false,
  "summary": { "files": 12, "types": 15, "violations": 1, "new": 1, "worsened": 0, "baselined": 0, "stale": 0, "parseErrors": 0, "skipped": 0 },
  "violations": [
    {
      "rule": "cyclomaticComplexity", "status": "new",
      "type": "Shop.Orders.OrderService", "member": "Submit",
      "file": "src/Orders/OrderService.cs", "line": 42, "value": 31, "limit": 25,
      "message": "Shop.Orders.OrderService.Submit: cyclomatic complexity 31 exceeds max 25.",
      "hint": "Extract branches into well-named methods, use guard clauses, or replace condition chains with lookup tables or polymorphism."
    }
  ],
  "stale": [], "parseErrors": [], "hotspots": ["..."]
}
```

`analyze` and `check --include-metrics` add `types` with all metrics, members and coupled type
names. `schema` versions the report: within one schema fields are only added and rule ids are
never renamed. `hotspots` are display strings, not part of the contract; use `types` for data.

## Using it as a gate

- **Local and pre-commit runs** over changed files give fast feedback. They are advisory: they
  read files from the working tree, see only the files listed, and a different `--root`, `--type`,
  `--config` or file list changes what is judged.
- **The gate is a full `check` in CI** on every pull request, with the configuration and baseline
  of the repository. Protect `codemetr.json` and `codemetr.baseline.json` with code-owner review,
  since whoever edits them decides the rules.
- In GitHub Actions: `dotnet tool install --global Codemetr.CSharp.Tool` and
  `codemetr-csharp check --format text`.

## Library

```csharp
using Codemetr.CSharp;

var config = CodemetrConfig.Load(Path.Combine(root, CodemetrConfig.FileName));
var documents = SourceCollector.Collect(root, ["src"], config.Exclude);
var context = SourceCollector.CollectContext(root, config.Context);
var result = CodeAnalyzer.Analyze(documents, context, typeQuery: "OrderService");
var violations = GuardEvaluator.Evaluate(result.Types, config);
```

## Limits

- Metrics are signals for prioritising refactorings, not proof of good design.
- The maintainability index uses a token-level Halstead approximation; compare values within
  one code base rather than with other tools.
- `types` retains per-declaration measurements. `wholeTypes` additionally combines eligible
  partial declarations; see the scope and guard policy in the measurement definitions.
- Line-based values (line span, logical lines, maintainability index) follow the source layout.
- Code in a new file with the `<auto-generated>` header or in an excluded path is not measured at
  all; the report counts such files in `summary.skipped` so that they stay visible in review.
- Debt inside inactive `#if` code stays recorded but cannot be re-measured: the entry remains
  unverified (with a warning) as long as the file contains inactive code.
- Field and property initializers (for example a lambda assigned to a field) are not measured as members.
- Code inside inactive `#if` regions is invisible and reported as a warning; no project files are
  read, so no preprocessor symbols, package references or per-project language settings are applied.
- Top-level statements are reported as type `Program` with kind `top-level`; C# 14 extension
  blocks count as members of their static class (`extension(string).Name()`).

## License

MIT
