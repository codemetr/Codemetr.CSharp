# Implementation evidence

## Behavioural checks

Member-size tests executed with additive zero-valued properties before measurement:
10 failures and one empty-body pass. Green: all 11 initial size cases passed.
Guard/hotspot tests ran before integration: all six failed on absent rules or
report fields. Green: all 17 combined cases passed. A further executed regression
exposed a local function inside a top-level block absent from the inventory (one
failure), then passed with traversal corrected. Bodyless extern locals are skipped.
This scoped inventory correction is explicit: existing numerical defaults stay,
but previously missed executable members can now produce real existing-rule findings.
It is not claimed that repairing missing inventory preserves every old verdict.

The complete suite passes 138 tests, zero failures/skips, including the executable
example's hand-calculated nine lines/nine statements, opt-in verdict, source/partial
provenance, override and baseline ratchet behaviour, JSON/text, filters and top=0.
No source-only or compilation failure is counted as behavioural Red. Logs are
ignored under artifacts/member-*.log; test names and expected outcomes are retained.

## Review and limitations

The existing public MemberMetrics positional constructor/deconstruction and schema 1
are unchanged. New init properties and optional rules are additive. Defaults remain
disabled; no configuration, self-guard pin, baseline or dependency changed. All size
measurement stays in one syntax traversal responsibility; hotspot ordering/rendering
uses the existing member inventory and preserves type-hotspot output.

Static counts describe syntax, not executed instructions or actual readability.
Multiline token content counts physically; lambda blocks belong to the owner;
local functions remain separate. No dynamic coverage value or source join is
invented. The concrete owner-approved follow-up is specs/007-cover-member-link.

Marketplace source is codemetr/skills branch 006-member-size-guidance. Both static
plugin manifests use 0.3.0, the same portable skills and references. Six structural
packaging tests pass. Claude CLI is unavailable, so live Claude validation and
behavioural host evaluation were not performed. Evaluation cases are instructions,
not measured model results. Published tool 0.1.0 remains pinned in consumer examples;
forward guidance explicitly identifies unsupported member-size features.

Full local restore/build/test and the published 0.0.1 self guard passed. The guard
measured 67 production types/files with zero new/worsened/stale/parse findings.
Current-head hosted checks and bot reviews remain mandatory before merge.

Measured validation record: 2026-10-30T11:08:39Z.
Base: 3906768, with the feature working tree. Sorted path/content SHA-256 over
src/tests/examples excluding bin and obj: `5f146f8edc0f2e437bec90d1c621db8f094c5b47364ab8c8137aa640d708e429`.
Both the full and pre-commit diff guards exited 0. SDK: 10.0.403.


## Hosted validation and delivery

Recorded at 2026-10-30T11:12:39Z.
Marketplace PR codemetr/skills#4 merged as 5df6070 after its validate check passed
and no pending reviews or unresolved threads were present. Both manifests deliver
plugin 0.3.0; this changes guidance, not the consumer tool installation.

Tool PR codemetr/Codemetr.CSharp#7 source head
6550d63e87038bfe5d65b82ae6dbd075c0a1c394 passed the build check and Codacy Static
Code Analysis; Codacy's completed summary reports zero new issues. No requested
reviews or unresolved review threads were present. This final documentation-only
completion commit must pass fresh checks before the PR can merge. No source,
measurement contract or executed example changed since the recorded source digest.
All 12 tasks are complete; source-bound Cover joining has its own owner and concrete
ready follow-up. No tool package publication or live host evaluation is claimed.
