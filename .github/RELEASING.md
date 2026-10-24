# Branch protection and NuGet releases

## Protect main (repository administrator)

Workflow files alone do not enforce branch protection. In GitHub, open
**Settings > Rules > Rulesets**, create a branch ruleset targeting `main`, and:

- Set enforcement to **Active** and leave the bypass list empty.
- Enable **Require a pull request before merging**. Required approvals can be
  zero for a solo maintainer; requiring an approval needs another reviewer.
- Enable **Require status checks to pass** and select the `build` job from the
  `ci` workflow (GitHub Actions). Run CI once if the check is not yet selectable.
- Enable **Require branches to be up to date before merging**.
- Enable **Block force pushes** and **Restrict deletions**.

Do not enable "Restrict updates": it would also prevent normal PR merges unless
a bypass is allowed. CI runs for every pull request without path filters, for
pushes to `main`, and for merge queues (`merge_group`). Keep the `build` job name
stable, or update the required status check when renaming it.

These settings prevent direct pushes, including by administrators without a
bypass. Administrators can still change the ruleset itself. Availability depends
on the repository visibility and GitHub plan.

## One-time NuGet setup

1. On nuget.org, create an API key scoped to pushing new versions of
   `Codemetr.CSharp.Tool`. For the first publication, ensure the key also permits
   creating that package. The account must own or be allowed to create the ID.
2. In GitHub **Settings > Environments**, create an environment named `nuget`.
3. Restrict deployment branches to `main`. Optionally require a deployment
   reviewer if another maintainer is available.
4. Add the API key as environment secret `NUGET_API_KEY`. Rotate it before expiry;
   never put it in the repository or workflow inputs.

## Publish a version

1. Merge the intended changes into `main` through a passing PR.
2. Open **Actions > release > Run workflow**, select `main`, and enter a new
   version, for example `0.1.0` or `0.2.0-rc.1`.
3. The workflow builds and tests the solution, checks its own sources, packs only
   the tool, installs it locally for a smoke test, and pushes it to nuget.org.

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
