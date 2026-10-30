# Implementation plan

Status: done (implementation and validation complete; checked PR delivery)
Owner: repository maintainer, Alexander Bleissem

Existing .NET 10, Roslyn, xUnit and published self guard. No new package dependency.
Constitution check: additive API/schema and disabled default rules preserve
compatibility. Existing main protection, reviews and guard configuration stay.

1. Add compilable additive metric/guard signatures, then execute hand-calculated
   failing tests. Implement body-only MemberSize and integrate MemberCollector,
   including top-level statements without double-counting local functions.
2. Execute guard and CLI tests before rule/report integration. Extend GuardRules,
   GuardEvaluator and RuleIds. Add structured MemberHotspot plus separate rendering;
   retain type hotspot code and stable constructor signatures.
3. Exercise partial ownership, overrides, ratchets, selection, top=0, multiline
   literals, lambda bodies, accessors and extension blocks. Refactor under green tests.
4. Add executable example with a long flat member, opt-in configuration and documented
   expected values. Update README, definitions and changelog.
5. Update codemetr/skills on its own branch and validate portable shared files,
   both manifests, tests and examples. Do not pretend the published 0.1.0 has new
   flags/fields or change consumer pins. Prepare concrete Cover-link follow-up.
6. Run full restore/build/test/published self guard and diff guard; document measured
   evidence, commit/push, open PRs and obey current-head checks/reviews before merge.
