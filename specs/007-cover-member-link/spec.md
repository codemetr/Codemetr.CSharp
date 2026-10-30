# Cover-linked member hotspots

Status: ready for design investigation
Owner: repository maintainer, Alexander Bleissem
Origin: feature 006 FR-008 and the owner's explicit scope split.

Bounded outcome: optionally present measured Cover member coverage beside static
size/complexity, without adding a runtime Cover dependency or silently running tests.

CL-001 Require explicit report input, supported schema/tool version and report digest.
CL-002 Bind measurements to source content/revision and reject stale or foreign evidence.
CL-003 Match normalised source paths, full type identity and overload/accessor/local
function identities; diagnose ambiguous and unmatched rows, never guess by name alone.
CL-004 Missing/unmeasured data remains unavailable, distinct from measured zero.
CL-005 Preserve static report/guard behaviour and deterministic ordering when no
coverage report is supplied; do not merge dissimilar metric formulas silently.
CL-006 Test current/stale/foreign/partial reports, overloaded and generic members,
partial types, keyed path collisions, empty evidence and actual zero coverage.

Acceptance: synthetic contract fixtures plus one real source-bound Cover export
prove exact identity matching and unavailable/zero separation. An unchanged static
command remains compatible; no new default policy or test invocation is introduced.
