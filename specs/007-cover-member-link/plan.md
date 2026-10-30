# Follow-up plan

Status: ready
Owner: repository maintainer, Alexander Bleissem
Dependency: merge feature 006; inspect the current public Cover export and provenance
contract before selecting a format. No release or schema assumption is pre-approved.

1. Read Cover export/source-binding fields and establish a frozen example with digest.
2. Complete research.md with a compatible join schema or an explicit upstream export
   prerequisite, and choose path/identity normalisation with collision diagnostics.
3. Clarify any missing source hashes/provenance; do not infer compatibility from
   similar method names. Complete contracts, tasks and consistency analysis.
4. Execute CL-001–006 failing fixtures before implementation. Validate a real export
   plus stale/ambiguous/unavailable cases. Update examples and both marketplace skills.
5. Run both repositories' checks and complete reviewed PRs under their existing rules.

Next action: maintainer inspects Cover's report contract and records whether its
existing source evidence supports CL-002. If not, add a bounded export prerequisite
before implementing the join. Acceptance is defined in spec.md; no open finding
from feature 006 is represented as complete merely by reserving this directory.
