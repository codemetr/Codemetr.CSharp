# ExecPlan 001-measurement-integrity: Measurement integrity and partial types

Owner: codex
Status: in_progress
Created: 2026-10-27T07:06:58Z
Updated: 2026-10-27T07:25:07Z

## Purpose and evidence

Existing declaration-only measurements permit type debt to be reduced by splitting partial declarations. Unresolved references and semantic errors are not summarized.

## Scope and dependencies

Dependencies: none.

Group actual partial declarations by Roslyn symbol. Measure all available context parts together; retain file and line provenance per member and declaration. Preserve non-partial duplicate diagnostics. Report unresolved coupling, semantic error counts, inactive code and partial scope. Never interpret unavailable source or symbols as proof of completeness.

## Milestones

- [x] M1: Freeze definitions and regression fixtures with positive/negative controls.
- [x] M2: Implement the bounded change, documentation and scope/provenance output.
- [x] M3: Run regression tests and the published NuGet self guard; resolve findings.
- [ ] M4: Review the measured diff, record dispositions, PR to main and inspect current-head checks and bot reviews before merge.

## Acceptance

Split/unsplit equivalents agree on member counts, coupling and complexity; a file-list selection includes its available partial counterparts. Excluded/generated counterparts remain explicitly visible. Existing baseline disappearance protections pass.

## Execution and recovery

Use the task full clone on `feature/measurement-integrity-and-metrics`. Heavy runs use `~/dev/scripts/run-capped.sh`. New metrics do not introduce mandatory thresholds, rewrite consumer baselines or silently change existing report schema semantics. Any necessary incompatible report change is versioned explicitly. No local package/feed installation as a substitute for the published guard. Keep one type per new C# file. Tests of candidate algorithms are development tests, never evidence that the published guard contains the new features. Before each commit run the published guard; before merge run the complete suite. Record remaining actionable findings with owned follow-up plans.

## Progress, decisions and results

- 2026-10-27T07:06:58Z: Owner authorized plans and implementation, including CodeMetr itself as guard.

- 2026-10-27T07:19:52Z: Definitions and regression controls implemented. Existing declaration reports/guards stay compatible; whole-type guard is explicit opt-in with per-file thresholds. Published 0.0.1 self guard remains separate from development algorithm tests. Final validation and independent diff review underway.

- 2026-10-27T07:25:07Z: 114 regression tests and published 0.0.1 self guard (63 production types, zero findings) pass. Five implementation-review counterexamples corrected; narrow re-review and current-head PR admission pending. PR: https://github.com/codemetr/Codemetr.CSharp/pull/4.
