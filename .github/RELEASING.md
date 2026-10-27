# Branch protection and NuGet releases

## Protect main (repository administrator)

Workflow files alone do not enforce branch protection. In GitHub, open
**Settings > Rules > Rulesets**, create a branch ruleset targeting `main`, and:

- Set enforcement to **Active** and leave the bypass list empty.
- Enable **Require a pull request before merging**. Required approvals can be
  zero for a solo maintainer; requiring an approval needs another reviewer.
- Enable **Require status checks to pass** for both checks and their expected apps:
  - `build` from **GitHub Actions** (app ID `15368`).
  - `Codacy Static Code Analysis` from **Codacy Production** (app ID `56611`).
  A missing or still-running analysis is not a pass. Select the check after it has
  actually reported in this repository; do not accept an identically named check
  from a different publisher.
- Enable **Require branches to be up to date before merging**.
- Enable **Block force pushes** and **Restrict deletions**.

Do not enable "Restrict updates": it would also prevent normal PR merges unless
a bypass is allowed. CI runs for every pull request without path filters, for
pushes to `main`, and for merge queues (`merge_group`). Keep the `build` job name
stable, or update the required status check when renaming it.

## Bot finding disposition before merge

Inspect checks **and** bot reviews for the current head before merging. Wait for
an enabled reviewer to finish; the review may arrive after its static-analysis
check. Recheck after a new commit. Fix a valid finding and obtain fresh checks.

For a false positive, record the exact finding and affected source revision,
explain why it does not apply, and link primary evidence. Use the analyzer's
supported issue-specific ignore/resolution with that reason, then wait for a new
passing check. A repository note alone does not turn a failed check into success.
Never disable the analyzer, exclude a whole workflow, remove the required status,
or spoof a check just to merge. Keep substantive review dispositions separate
from check conclusions and resolve outstanding review threads.

See [the historical OIDC finding](review-decisions/codacy-pr-2-oidc.md).
A job-scoped OIDC permission is not a credential, but that specific conclusion
must not suppress future genuine secrets or overbroad permissions.

These settings prevent direct pushes, including by administrators without a
bypass. Administrators can still change the ruleset itself. Availability depends
on the repository visibility and GitHub plan.

## One-time NuGet setup

1. On nuget.org, open **Trusted Publishing** and create a GitHub Actions policy:
   - Package owner: the NuGet organization `codemetr`.
   - Repository owner: `codemetr`; repository: `Codemetr.CSharp`.
   - Workflow file: `release.yml` (file name only).
   - Environment: `nuget`.
   - Package glob: `Codemetr.CSharp.Tool` (not `*`).
   - Scopes: push new packages and package versions, to allow the first release.
2. In GitHub **Settings > Environments**, use the environment named `nuget`.
3. Restrict deployment branches to `main`. Optionally require a deployment
   reviewer if another maintainer is available.
4. Add environment secret `NUGET_USER`: your personal NuGet profile name, not
   your email address or the organization name. This user must have publishing
   permissions for the package owner and match the policy's NuGet identity.

```powershell
gh secret set NUGET_USER --env nuget --repo codemetr/Codemetr.CSharp
```

The release job requests a GitHub OIDC token and exchanges it through
`NuGet/login` for a temporary NuGet API key immediately before publishing.
No persistent `NUGET_API_KEY` secret is needed. Remove any old API-key secret
and revoke its key on nuget.org if one was previously created. The login action
is pinned to a commit for supply-chain safety.

See [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Publish a version

1. Merge the intended changes into `main` through a passing PR.
2. Open **Actions > release > Run workflow**, select `main`, and enter a new
   version, for example `0.1.0` or `0.2.0-rc.1`.
3. The workflow builds and tests the solution, checks its own sources with the pinned published NuGet tool, packs only
   the tool, smoke-tests the pinned published guard, and pushes the new package to nuget.org.
Candidate behavior is covered by the regression suite; no development tool is
installed from a local package feed.

The supplied version overrides `Directory.Build.props` for the release build
and package. NuGet versions cannot be overwritten: use a new version for every
publication. A duplicate push fails rather than silently succeeding. The
library is bundled in the tool; its separate library package is not published
by this workflow. Publication is manual, never triggered by PRs or every merge.

After NuGet indexing completes, users need the .NET 10 SDK, but no source code:

```bash
dotnet tool install --global Codemetr.CSharp.Tool
codemetr-csharp --help
```

For a specific version (including a prerelease):

```bash
dotnet tool install --global Codemetr.CSharp.Tool --version 0.2.0-rc.1
```

Existing installations can be upgraded with
`dotnet tool update --global Codemetr.CSharp.Tool`.
