# ExecPlan 004-cohesion: State-based cohesion

Owner: codex
Status: in_progress
Created: 2026-10-27T07:06:58Z
Updated: 2026-10-27T07:31:09Z

## Purpose and evidence

CodeMetr has no indication of disconnected groups of instance behavior.

## Scope and dependencies

Dependencies: 001.

Report connected groups of instance methods linked by shared instance fields or direct instance method calls, using semantic symbols. Exclude constructors/static behavior/nested types. State applicability explicitly; no field-based score for stateless/data-only types. Do not claim this proves SRP.

## Milestones

- [x] M1: Freeze definitions and regression fixtures with positive/negative controls.
- [x] M2: Implement the bounded change, documentation and scope/provenance output.
- [x] M3: Run regression tests and the published NuGet self guard; resolve findings.
- [ ] M4: Review the measured diff, record dispositions, PR to main and inspect current-head checks and bot reviews before merge.

## Acceptance

Connected, disconnected, overload, shadowing, partial and stateless examples have independently specified expected groups and applicability. Advisory only.

## Execution and recovery

Use the task full clone on `feature/measurement-integrity-and-metrics`. Heavy runs use `~/dev/scripts/run-capped.sh`. New metrics do not introduce mandatory thresholds, rewrite consumer baselines or silently change existing report schema semantics. Any necessary incompatible report change is versioned explicitly. No local package/feed installation as a substitute for the published guard. Keep one type per new C# file. Tests of candidate algorithms are development tests, never evidence that the published guard contains the new features. Before each commit run the published guard; before merge run the complete suite. Record remaining actionable findings with owned follow-up plans.

## Progress, decisions and results

- 2026-10-27T07:06:58Z: Owner authorized plans and implementation, including CodeMetr itself as guard.

- 2026-10-27T07:19:52Z: Definitions and regression controls implemented. Existing declaration reports/guards stay compatible; whole-type guard is explicit opt-in with per-file thresholds. Published 0.0.1 self guard remains separate from development algorithm tests. Final validation and independent diff review underway.

- 2026-10-27T07:25:07Z: 114 regression tests and published 0.0.1 self guard (63 production types, zero findings) pass. Five implementation-review counterexamples corrected; narrow re-review and current-head PR admission pending. PR: https://github.com/codemetr/Codemetr.CSharp/pull/4.

- 2026-10-27T07:31:09Z: Independent correction review accepted bce2a61. Bot-review dispositions and three extra controls implemented; 117 tests and published full/diff guards pass. Integration remains tracked in PR 4; no NuGet release or consumer upgrade is claimed.
