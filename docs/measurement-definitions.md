# Logical types and advisory metrics

These additions do not change existing rule ids, JSON `types` declaration rows or
baseline keys. New JSON fields are additive under report schema 1. These features are available in tool 0.1.0. The pinned 0.0.1 self guard does not
contain these additions; regression tests exercise the 0.1.0 implementation
separately from that published self guard.

## Whole partial types and guard admission

`wholeTypes` groups all-partial declaration sets by their Roslyn symbol in the
configured synthetic compilation. It retains declaration file/line/eligibility
and each executable member's actual file. A non-partial duplicate is not merged.
A selected type pulls in available measurable partial counterparts from context;
unrelated types in those counterpart files are not selected. Generated, excluded
and parse-failing counterparts are disclosed as unmeasured and contribute no
measured value. Missing files, oversized context, inactive branches, different
project options and missing package references remain limitations. Source-only
analysis cannot establish that same-named partial declarations in separate
projects belong to one real project. Narrow `context` accordingly.

Aggregate definitions:

- Executable members concatenate; total cyclomatic complexity sums, maximum takes
  the maximum. Public partial definition/implementation pairs count once.
- Coupling unions distinct names; constructor arity takes the maximum. Explicit
  fields and public method counts combine eligible parts.
- `lines` sums eligible declaration spans, including repeated partial headers.
  Logical lines use `(file, line)` identity for own non-brace tokens. Halstead and
  MI are recomputed from eligible own tokens. Neither physical size nor MI is
  invariant under adding repeated declaration syntax.
- Inheritance is taken from the shared symbol. Each declaration remains available
  through the original `types` collection for comparison.

Existing guards remain per declaration by default. To explicitly adopt whole-type
checks, configure `"aggregatePartialTypes": true` in `codemetr.json`. This changes
measurement policy and requires reviewing its impact on existing debt; never
recreate an established baseline merely to admit the new values. Whole-type
thresholds are checked at every eligible declaration file using that file's
existing overrides. Member thresholds apply only to members actually declared in
that file. Thus adding or renaming a canonical anchor cannot select another
file's member threshold. Type-level findings can occur at several contributing
files; that is intentional, not independent debt to sum. Counterpart files and
selected type identities enter comparison scope together. Existing hidden-code
and inactive-code baseline protections remain in force.

`completeness` exposes compilation-error count/ids (syntax and semantic diagnostics) for the context,
unresolved coupling names, unmeasured partial declarations and inactive files.
`contextFilesWithLimits` also lists syntax-error/inactive context files whose
unmeasured declarations cannot safely be attributed to any selected type. This
avoids guessing identities from malformed or disabled source text.
Semantic errors are advisory because project/package references are unavailable.
An empty unresolved list is not a claim of project completeness. `summary.skipped`
continues to count generated/excluded selected files. Future project-aware
resolution must be a separately specified change, not namespace guessing.

## Directed dependencies and cycles

`dependencies` is the induced graph of selected, uniquely named source types.
An edge requires successful symbol binding, not Roslyn candidate fallback.
Unresolved names stay explicit. Types present only in available context are listed
separately; resolved references outside that source context are platform references
under the current analysis model. Package identity is not inferred from namespace.
Duplicate full names are disclosed and excluded as graph vertices. Nested types
remain separate vertices. Generic references use original definitions.

`fanIn` counts distinct incoming selected types; `fanOut` counts outgoing selected
types. Incoming edges from unselected types are not counted. Filtering can hide
cycles and reduce fan-in, so neither zero means global independence. Namespace
edges collapse type edges to their declared namespaces and omit self edges.
Both type and namespace cycles report strongly connected groups and one real
closed edge path per group. Iterative Kosaraju traversal avoids stack growth with
graph size. These are source graphs, not runtime DI, plugin or event topology.
Definitions of directed coupling: [NDepend](https://www.ndepend.com/docs/code-metrics).

## Cognitive complexity

`members[].cognitiveComplexity` is a documented C# heuristic inspired by
[SonarSource's model](https://www.sonarsource.com/docs/CognitiveComplexity.pdf),
not a claim of exact Sonar analyzer compatibility or objective readability.
The starting value is zero. Each `if`, loop, catch, switch statement/expression
and ternary contributes one plus its current control nesting. An `else if`
contributes one without another nesting penalty; a plain `else` contributes one.
A switch contributes once rather than once per case. A `goto` contributes one.
Each contiguous same-operator `&&`/`||` expression sequence contributes one;
operator changes start a new sequence. Parentheses around a sequence do not reset
it. Binary `and`/`or` patterns use the same syntactic operator-change rule.

Child bodies/arms of these control constructs increase nesting; `try` and
`finally` do not. Lambda bodies increase nesting without a standalone increment.
Local functions are measured separately and excluded from their enclosing
member, matching existing member ownership. Recursion, higher-order semantic
flow, implicit compiler state machines and arbitrary jumps are not inferred;
`return`, `break`, `continue`, null-coalescing and null-conditional syntax do not
add increments. This metric has no default threshold and does not change existing
cyclomatic or nesting calculations.

## State-based cohesion

`cohesion` reports connected groups of implemented ordinary instance methods,
linked by a shared explicitly declared own instance field or direct implicit/this
method calls. Symbol identity distinguishes overloads and shadowed locals. Calls
and fields accessed on another receiver do not connect the current instance.
Constructors, operators, static members, inherited state, accessors and implicit
backing fields are outside this variant. Local functions/lambdas contribute to
their enclosing method's state references. `nameof` is not state access or a call.

At least one explicit own instance field and two implemented instance methods are
required for applicability. Stateless/data-only/auto-property-only types receive
`applicable: false`, not a failing or perfect score. Several groups are a review
prompt, not proof of an SRP violation. This connected-group variant is not labelled
as an implementation of a particular LCOM formula and imposes no guard threshold.

## Mutation report import

`--mutation-report FILE` imports the scoring subset of
[mutation-testing-elements JSON](https://github.com/stryker-mutator/mutation-testing-elements/blob/master/packages/report-schema/src/mutation-testing-report-schema.json),
versions `1` and `2` (up to three numeric version components). It accepts known
mutant statuses, a files object and arrays of mutants with unique nonempty string
ids per file. It validates this scoring subset, not every optional or unrelated
schema field. The importer neither runs Stryker nor verifies the report against
current source. The report's exact SHA256, supplied path, version, scope and all
status counts remain visible. Input larger than 32 MiB is rejected.

`Killed` and `Timeout` are detected; `Survived` and `NoCoverage` are undetected.
`CompileError`, `RuntimeError`, `Ignored` and `Pending` do not enter the denominator.
Score is `100 * detected / (detected + undetected)`; no denominator means `null`,
not 100. Any pending mutant also makes the score `null`: a partial run must not
appear to be a final success. Unknown statuses/versions, duplicate ids and malformed
JSON fail with input exit 2. Imported scores are advisory and cannot change the
static guard verdict. Equivalent mutants, timeouts and report selection still
require interpretation; the importer does not establish test quality by itself.

## Self guard

Run `bash scripts/check-metrics.sh` under the workspace's capped runner locally.
The script restores the pinned 0.0.1 tool from an official-NuGet-only configuration
and checks every production source with existing default thresholds, without a
baseline or relaxed overrides. Both CI and release use this same published guard.
Candidate unit/CLI regression tests test new algorithms; they do not replace it.
A future consumer upgrade requires publishing a new immutable package version
through the normal passing PR and release procedure. No consumers are switched
to an unpublished development build.


## Member size

Development feature: additive `members[].codeLines` and `statementCount` preserve
the positional MemberMetrics constructor and existing `lines` declaration span.
Code lines count distinct physical lines occupied by non-brace body tokens. Ignore
signatures, attributes, comments, trivia and braces; include every line within a
multiline token, even a blank line inside a literal. This is lexical body size,
not an IL-instruction or runtime execution count. A semicolon-only body occupies
a code line but contributes no counted statement.

Statement count includes each StatementSyntax except blocks, empty statements
and local-function declarations. Controls and declarations count alongside their
child statements. Else clauses are not statements. Expression-bodied members add
one root statement; expression lambdas do not add a separate statement. Statements
in lambda blocks belong to the containing member. Local functions and nested
types are pruned and measured separately, including nested top-level functions.
Auto-accessors, abstract/extern declarations and initialisers remain outside the
executable-member inventory. Partial members retain declaration-file provenance.

Nullable rules `maxMemberCodeLines` and `maxMemberStatements` are disabled by
default, inclusive when configured, and use existing file/type overrides and
baseline ratchets. Their IDs are `memberCodeLines` and `memberStatements`.
Opting in is a deliberate policy decision, not an automatic upgrade consequence.

`memberHotspots` is a structured list independent of `types` inclusion. It exposes
type/member/kind/file/line, codeLines, statementCount, cyclomaticComplexity,
cognitiveComplexity, nestingDepth and parameterCount. Rank descending by body
code lines, statements, cognitive and cyclomatic complexity; ties use ordinal
file, type, member and then line. --top independently limits both existing type
hotspots and this list; zero suppresses both. File/type selection limits its scope.
No coverage is inferred or joined. Empty lists mean no selected executable members
(or --top 0), not proof that a project has no risks. Short code does not prove SRP.
