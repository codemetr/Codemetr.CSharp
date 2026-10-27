# PR 6: mutation example path

Codacy inline comment `PRRC_kwDOVLYAWM8AAAABBbcnPw`, thread
`PRRT_kwDOVLYAWM6ww28F`, reviewed source
`6ca064a0bf495310c63b7bbeaee22e08ca71ccd8`, `examples/README.md`.

The proposed repository-relative argument is a false positive. The documented
command selects `--root examples/metrics`. `CodemetrService.Run` normalizes that
root and passes it with the mutation argument to `MutationReportReader.Read`.
The latter uses `Path.GetFullPath(path, root)`, so `synthetic-mutation.json`
correctly addresses `examples/metrics/synthetic-mutation.json`. Prefixing the
argument again would instead address a nonexistent nested path.

The independent `MutationImportTests` CLI case supplies a separate temporary
`--root` and a root-relative `report.json`; it imports that report successfully
and returns input exit 2 after the file is invalidated. The 0.1.0 release run
40533556932 passed all 119 tests, with no failures or skips. This is deterministic
regression evidence for path resolution, not a blanket dismissal of bot reviews.

The README now explicitly explains the selected-root semantics. No command,
path-resolution implementation, quality threshold or analyzer was disabled or
changed to avoid the comment. Fresh current-head checks remain mandatory; this
note does not override them. The final runnable-sample check is performed with
the actual package from nuget.org once indexing completes.
