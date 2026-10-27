# ExecPlan 005-mutation-import-and-self-guard: Mutation report import and published self guard

Owner: codex
Status: ready
Created: 2026-10-27T07:06:58Z
Updated: 2026-10-27T07:06:58Z

## Purpose and evidence

Static metrics cannot establish test effectiveness; current CI guards itself using an unpublished local candidate.

## Scope and dependencies

Dependencies: 001–004.

Import Stryker mutation-testing-elements JSON through an explicit CLI option; validate supported schema, finite status set and score denominator, retain report provenance and scope. Do not execute tests or claim coverage outside report. Add official NuGet-only pinned tool manifest and repeatable local self-guard; CI uses this published tool. Candidate regression tests are separate from the published guard.

## Milestones

- [ ] M1: Freeze definitions and regression fixtures with positive/negative controls.
- [ ] M2: Implement the bounded change, documentation and scope/provenance output.
- [ ] M3: Run regression tests and the published NuGet self guard; resolve findings.
- [ ] M4: Review the measured diff, record dispositions, PR to main and inspect current-head checks and bot reviews before merge.

## Acceptance

Import fixtures cover killed/survived/no-coverage/timeout/ignored/compile/runtime errors, empty denominator and malformed input. Published NuGet guard passes own production sources without relaxing thresholds. All regression tests pass.

## Execution and recovery

Use the task full clone on `feature/measurement-integrity-and-metrics`. Heavy runs use `~/dev/scripts/run-capped.sh`. New metrics do not introduce mandatory thresholds, rewrite consumer baselines or silently change existing report schema semantics. Any necessary incompatible report change is versioned explicitly. No local package/feed installation as a substitute for the published guard. Keep one type per new C# file. Tests of candidate algorithms are development tests, never evidence that the published guard contains the new features. Before each commit run the published guard; before merge run the complete suite. Record remaining actionable findings with owned follow-up plans.

## Progress, decisions and results

- 2026-10-27T07:06:58Z: Owner authorized plans and implementation, including CodeMetr itself as guard.
