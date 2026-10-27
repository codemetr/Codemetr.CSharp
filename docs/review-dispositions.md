# Measurement metrics review dispositions

Design evidence: commit ffe2264; opposite roles independently read the same source
and plans. The change advocate and status-quo defender both confirmed declaration
splitting changes current metrics; both identified compatibility risks.

| Finding | Disposition and verification |
| --- | --- |
| Preserve declaration report/API | Kept `types`; new `wholeTypes` is additive. Existing default guard unchanged; whole guard explicitly opt-in. Original 74 tests retained. |
| Arbitrary aggregate anchor changes overrides | Whole-type thresholds evaluated per eligible file; member thresholds use actual declaring file. Dedicated conflicting-override test. |
| Counterpart baseline scope | Include eligible counterpart paths together with selected type identities, preventing unrelated counterpart types being treated as measured. Baseline integration test. |
| Hidden/generated/invalid/inactive parts | Disclose unmeasured counterparts, preserve existing disappearance rules; contextual inactive branches explicitly included. Existing and new tests. |
| Formula and partial method pairs | Document layout-dependent line/MI behavior; count public definition/implementation pair once. Regression test. |
| Same-line declarations | Preserve source offsets as well as line/file to avoid mixing equal names in different namespaces. Regression test. |
| Candidate binding not resolved evidence | Dependency graph uses strict successful binding, keeps unresolved references separate and discloses duplicate names. |
| Filtered graph/counterpart scope | Graph labelled as induced selected source graph; direct filtering rebuilds it. Generic/nested/filtered tests. |
| Cohesion not universally applicable | Explicit own-state/method rule; exclude data-only/stateless cases, other receivers and nameof; overload/shadowing fixtures. |
| Mutation score input scope | Hash exact imported bytes; accepted versions/statuses fixed; unknown/duplicate input rejected; pending/empty score null. Import checked before any baseline write. |

No default thresholds were weakened and no consumer baseline was rewritten.
Project-aware package resolution and runtime dependency topology are explicitly
outside the source-only graph contract, not implicitly inferred or promised.

## Implementation review of 2772105

Both opposite roles checked the frozen implementation. Five material counterexamples
were accepted and corrected, with regression controls:

- Aggregate full-directory scope accidentally filtered out deleted/hidden type names.
  Preserve the original directory/type scope; restrict only newly included counterpart
  files by selected logical type. Deleted, renamed, generated and excluded cases fail
  correctly; unrelated counterpart debt is not reclassified.
- A hidden partial definition could subtract the only eligible implementation.
  Deduplicate only among eligible method symbols, including tests with either part hidden.
- Duplicate method signatures could throw during advisory cohesion enrichment.
  Return unavailable cohesion for ambiguous method identities; preserve syntax metrics.
- Object-initializer targets were mistaken for current-instance state. Roslyn operation
  receiver identity now distinguishes a newly constructed object from containing `this`;
  initializer values that explicitly use `this` still connect correctly.
- Partial calls and implementations used different symbols. Normalize both to the
  partial-definition identity, including explicit generic-call tests.

Additionally, the original public four-parameter Analyze overload is retained for
binary compatibility; the exclusions-aware overload has five required parameters.

Validation after corrections: 114 tests pass; official NuGet 0.0.1 self guard passes
63 production source types with zero findings, no baseline and unchanged thresholds.

- 2026-10-27T07:31:09Z: Both independent roles accepted the corrections at bce2a61 with no remaining material blocker. Bot findings are individually disposed in `.github/review-decisions/codacy-pr-4-metrics.md`. Added context-file limits, generic SCC self-loop support and workflow order/feed regression controls. Validation: 117 tests pass; published NuGet full guard passes 64 production types and diff guard passes four changed production types (three excluded test files), zero findings.

- 2026-10-27T07:36:02Z: Last delta is mechanical diagnostic-field precision (`CompilationErrorCount`/`CompilationErrorIds`, matching Roslyn GetDiagnostics) plus a test-helper selection fix and two controls. Production metric algorithms unchanged. Final local suite: 119 passing tests; published full guard: 64 production types, zero findings.
