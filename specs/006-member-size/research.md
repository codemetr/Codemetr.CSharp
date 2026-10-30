# Design decisions

The public MemberMetrics record already exposes declaration span (Lines), so it
must retain that meaning. Add init-only CodeLines and StatementCount instead of
changing positional parameters. Roslyn syntax supplies a repeatable static measure;
this is not IL instruction count or runtime execution count.

Use existing OwnSyntax pruning to keep local functions and nested types separate.
Find the executable body before traversing tokens to exclude signatures/attributes.
Count line spans of multiline tokens, with ordinary line positions rather than #line
remapping. Expression bodies have one root operation statement. Empty statements
and structural blocks do not increase statementCount; controls and declarations do.

New nullable rule fields default to disabled and merge through the existing rule
mechanism. Baselines already key on rule/type/member/file; no new baseline format.
Rank hotspots by size to reveal long low-branch members. Existing --top limits each
hotspot list independently. Use measured declaration members rather than repeating
whole-partial-type rows. Do not join unbound external coverage or invent percentages.

Spec Kit 1.2.5 was initialised with the installed CLI and Codex integration. No
extension hooks are configured. Existing .NET/Roslyn versions and dependencies stay.
