# PR 4: metric and release review dispositions

Review: Codacy review 5663518181, source commit
`27721055f09ee1eca00c8f73f5b2fa9df451e0af`; inline positions later show
`bce2a61f74f81fde9afb49d71d69a70b63a6f00a`. The findings below are disposed
individually. This note does not override required current-head checks.

## 4386060872: restore before release smoke invocation

The claim that the release path never restores the tool is a false positive.
At the reviewed source, `Guard own sources` invokes `bash scripts/check-metrics.sh`
before `Pack tool` and `Smoke-test published guard`. That script runs
`dotnet tool restore --configfile .config/codemetr.nuget.config` before using the
tool. The configuration clears inherited feeds and permits only NuGet.org; the
manifest pins 0.0.1. No shell step changes the working directory. The independent
`SelfGuardWorkflowTests.BothWorkflowsRestorePublishedGuardBeforeUsingIt` protects
step order, feed, pin and production-source check. The same script has succeeded
locally on the candidate. No restore bypass or workflow suppression is needed.

## 4386060874: self edges

Coupling intentionally counts *other* types, excluding self, as documented in the
existing `ClassCoupling` definition. A recursive data field is not a dependency
between components. Namespace self edges are likewise not cross-namespace cycles.
Changing that graph policy would turn ordinary self references into architecture
warnings without justification. A paired regression verifies this exclusion.

The internal generic SCC primitive now additionally recognizes an explicitly
supplied one-node self-loop and returns the real witness `A -> A`; its mathematical
boundary is independently tested. Production dependency graphs retain the
intentional distinct-type scope. No default threshold or suppression changed.

## 4386060879: incomplete partial declarations

The source reviewed already retains parse-failing identifiable partial parts as
unmeasured; `InvalidAndInactiveCounterpartsRemainVisibleAsLimits` at 2772105 tests
that case and an inactive branch. It reports context inactive paths explicitly.

The useful remaining boundary is wholly disabled or too malformed source, where
Roslyn cannot provide a reliable matching declaration identity. The candidate now
adds `completeness.contextFilesWithLimits` for every context file with syntax
errors or inactive code, even without an attributable type. A regression checks
both wholly disabled and unrecognizable sources. No guessed type name, synthetic
measured counterpart or claim of preprocessor completeness is introduced.

## 4386060883: graph traversal duplication

Keep these traversals separate. `StrongComponents` computes directed SCCs using
finish order and reverse edges, then finds a closed witness. Cohesion computes
undirected connected groups while removing already visited method vertices.
They use similar stack loops but have different contracts and invariants. A
shared policy-driven traversal would hide those distinctions and add indirection.
The workspace explicitly rejects abstraction solely for DRY. This is an advisory
clone observation, not a correctness failure; both algorithms have independent
controls, including a 5,000-node directed cycle. No analyzer engine or rule is
disabled, and the production self guard continues to pass.
