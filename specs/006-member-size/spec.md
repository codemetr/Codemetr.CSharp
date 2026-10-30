# Feature 006: Member size and static hotspots

Status: specified and clarified
Owner: repository maintainer, Alexander Bleissem

## User stories and acceptance

US1: A developer identifies long straight-line methods even when complexity is low.
A multiline body reports its own code lines and syntactic statements, excluding
comments, whitespace and separately measured local functions.
US2: A team opts into member-size limits without changing existing verdicts.
Equality passes; exceedances produce stable per-member rule identities and use
existing overrides and baseline ratchets.
US3: A developer finds individual methods through JSON and text hotspots with
stable source locations and existing complexity metrics, even without guard failures.
US4: Users receive matching examples, definitions and marketplace instructions.

## Requirements

- FR-001 Add member codeLines and statementCount without changing existing lines,
  complexity, constructor/deconstruction signatures or default rule thresholds.
- FR-002 Count only body code; ignore braces/comments/trivia, include all physical
  lines occupied by a non-brace token. Count non-empty, non-block StatementSyntax
  nodes; separately measured local declarations/bodies are excluded. Expression
  bodies contribute one statement. Lambda body statements belong to their owner.
- FR-003 Apply ownership consistently to methods, constructors, destructors,
  operators, accessors, local functions, top-level statements and extension members.
  Abstract/extern/auto-accessors remain unmeasured. Partial provenance stays intact.
- FR-004 Add nullable inclusive maxMemberCodeLines/maxMemberStatements opt-in
  rules; preserve overrides, hints and baseline new/worsened/stale behaviour.
- FR-005 Add structured memberHotspots plus text output using existing --top,
  ranked by code lines, statements, cognitive complexity, cyclomatic complexity,
  then ordinal file/type/member and line; preserve existing type hotspots.
- FR-006 Maintain schema 1 and old report fields and exit conventions. Report no
  coverage values without measurement; do not automatically invoke Cover.
- FR-007 Update runnable examples, README, measurement definitions, changelog,
  marketplace analysis/guard guidance, references, versions and validation.
- FR-008 Prepare a separately owned concrete Spec Kit Cover-link follow-up with
  source binding, identity matching and missing-data acceptance criteria.

## Scope decision

The owner selected static hotspots now and a separate Cover-link follow-up. No
unknown remains. No default threshold, new runtime package, automatic release or
consumer tool-pin upgrade is included. The marketplace must distinguish development
features from the latest published tool until publication is authorised.

## Success criteria

SC-001 Hand-calculated syntax fixtures prove FR-001–003, including multiline tokens.
SC-002 CLI tests prove opt-in guards, overrides, ratchets, JSON/text, filters and --top.
SC-003 Full build/tests and the unchanged published self guard pass.
SC-004 Examples and marketplace validation agree with the new documented contract.
