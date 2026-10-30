# Public contract

JSON adds members[].codeLines, members[].statementCount and memberHotspots[].
The two rule IDs are memberCodeLines and memberStatements. Configuration names
are maxMemberCodeLines and maxMemberStatements; both default to null (disabled).
Existing Lines remains the complete declaration span, including signature.

Statement count is syntactic: controls and local declarations count, blocks,
empty statements and local-function declarations do not; child statements inside
controls and lambda blocks count. An expression-bodied member counts one root
statement; expression lambdas add no separate statement. Code lines union all
non-brace body-token physical line spans, including multiline literal content.

Hotspots include all measured executable member kinds. They preserve declaration
file, line and overloaded name, use deterministic ordering, and honour type/file
selection and --top. They are present for analyze/check/baseline; --top 0 suppresses
both lists. No coverage field is manufactured. New rules retain the CLI exit-code
and baseline contracts and inclusive boundary semantics.
