# Historical finding: OIDC permission is not a hardcoded token

Disposition recorded on 2026-10-26 after inspecting the actual GitHub check
annotation and release workflow. This is a retrospective finding assessment,
not evidence that the issue was reviewed before the original merge.

## Exact finding

- PR: <https://github.com/codemetr/Codemetr.CSharp/pull/2>.
- Source commit: `08bcb1c533c8e78be27821ad3b97a12b4c91509d`.
- Codacy check run: `120996124003`, conclusion `action_required`.
- Annotation: `.github/workflows/release.yml`, line 23,
  `Hardcoded tokens are a security risk.`
- The annotated YAML entry is `id-token: write` under the publish job's
  `permissions`. The value is a permission level, not a credential.
- The check concluded at `2026-10-24T21:44:15Z`; the PR merged at
  `2026-10-24T21:44:20Z`. Its distinct AI review, submitted after that merge,
  reported no blocking implementation findings. That does not negate the check.

## Source assessment

The permission lets the job request a short-lived GitHub OIDC identity token.
It does not contain a token or grant repository write access. The job is limited
to `main`, uses the `nuget` environment, and exchanges its runtime identity via
the commit-pinned NuGet login action. The temporary publishing key is obtained
at runtime; it is not written into the workflow source.

Primary references:

- [GitHub: OIDC workflow permission](https://docs.github.com/en/actions/reference/security/oidc#workflow-permissions-for-the-requesting-the-oidc-token).
- [NuGet: Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing#github-actions-setup).

Therefore the **hardcoded-token allegation at that exact entry** is a false
positive. This does not assert that every use of OIDC is secure or that the whole
workflow has no other issues. No secret-detector rule, engine, workflow exclusion
or repository-wide threshold is changed by this disposition. No historical Codacy
check has been edited or claimed green. An analyzer-side ignore is not claimed.

## Process repair

The existing main ruleset previously required only `build`. It now also requires
`Codacy Static Code Analysis`, bound to Codacy's app, with no bypass actors.
The unchanged PR and up-to-date requirements remain. A future red finding must be
fixed or resolved specifically in the analyzer after a recorded evidence-based
assessment, followed by a fresh successful check before merge. This note is not
a bypass credential and cannot authorize merging another red check.
