# Changelog

## 0.1.0 - unreleased

- Library `Codemetr.CSharp`: Roslyn metrics per type and member (cyclomatic complexity, nesting depth,
  parameters, maintainability index, class coupling with type names, depth of inheritance, size).
- Configurable rules with file and type overrides, exclusions, inclusive limits.
- Ratchet baseline scoped to the analysed files and types; overloads keep separate entries.
- Tool `codemetr-csharp`: `analyze`, `check`, `baseline`; JSON and text reports; exit codes 0/1/2;
  `--files-from` for changed-file runs with the whole root as compilation context.
- Top-level statements and C# 14 extension blocks are measured.
- Warning when a type is declared twice in the compilation context.
