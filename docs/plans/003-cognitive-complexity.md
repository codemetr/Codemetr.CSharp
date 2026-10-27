# ExecPlan 003-cognitive-complexity: Cognitive complexity

Owner: codex
Status: ready
Created: 2026-10-27T07:06:58Z
Updated: 2026-10-27T07:06:58Z

## Purpose and evidence

Cyclomatic complexity alone does not distinguish equally branching but differently nested methods.

## Scope and dependencies

Dependencies: 001.

Implement and document a C# cognitive-complexity heuristic informed by SonarSource, with explicit supported syntax and differences. Measure member flow, nesting, else-if, boolean sequences and local-function boundaries. Keep this advisory, not a new default guard.

## Milestones

- [ ] M1: Freeze definitions and regression fixtures with positive/negative controls.
- [ ] M2: Implement the bounded change, documentation and scope/provenance output.
- [ ] M3: Run regression tests and the published NuGet self guard; resolve findings.
- [ ] M4: Review the measured diff, record dispositions, PR to main and inspect current-head checks and bot reviews before merge.

## Acceptance

Hand-calculated fixtures distinguish nested and flat control flow, else-if, switches, boolean sequences and local functions. No double counting. Existing cyclomatic/nesting metrics stay unchanged.

## Execution and recovery

Use the task full clone on `feature/measurement-integrity-and-metrics`. Heavy runs use `~/dev/scripts/run-capped.sh`. New metrics do not introduce mandatory thresholds, rewrite consumer baselines or silently change existing report schema semantics. Any necessary incompatible report change is versioned explicitly. No local package/feed installation as a substitute for the published guard. Keep one type per new C# file. Tests of candidate algorithms are development tests, never evidence that the published guard contains the new features. Before each commit run the published guard; before merge run the complete suite. Record remaining actionable findings with owned follow-up plans.

## Progress, decisions and results

- 2026-10-27T07:06:58Z: Owner authorized plans and implementation, including CodeMetr itself as guard.
