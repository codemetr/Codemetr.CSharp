# Codemetr.CSharp agent rules

Global workspace instructions apply. Read `.github/RELEASING.md` before PR or
release work.

## Merge admission

- Use a topic branch and PR to `main`. Never bypass repository protection or
  publish a synthetic successful status for another tool.
- Before merge, inspect current-head checks, review submissions and unresolved
  review threads. A green build alone is insufficient. Wait for enabled bot
  reviews to finish and verify the reviewed commit; a pending review is not an
  empty successful review. For later commits, examine the delta and qualify any
  necessary follow-up review. An earlier review is not automatic approval of
  changes it never examined.
- Both required checks must pass: `build` from GitHub Actions and
  `Codacy Static Code Analysis` from the Codacy app. Missing, pending, failed,
  cancelled or action-required checks prohibit merge. Recheck after every push
  and after updating the target branch.
- Correct valid bot findings. For a false positive, record the exact finding,
  source/commit binding, reasoning, independent supporting evidence and disposition.
  Resolve that specific issue through the analyzer's supported mechanism and wait
  for its fresh successful check. Documentation alone never overrides a red check.
- Do not suppress an engine, exclude a whole workflow, remove a required check,
  dismiss an entire review, or change thresholds just to get a green result.
- The historical OIDC false positive is recorded in
  `.github/review-decisions/codacy-pr-2-oidc.md`; it is not a blanket exception for
  token-related findings or future workflow changes.
- Keep checks and reviews distinct: a bot review saying no blocking issues does
  not override a failed static-analysis check, and a green check does not dispose
  an outstanding substantive review finding.
