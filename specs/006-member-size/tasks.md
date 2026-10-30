# Tasks: Member size and static hotspots

## Setup and specification
- [x] T001 Initialise Spec Kit and complete Specify/Clarify/Plan/contracts/checklist (FR-001–008).
- [x] T002 Analyse requirement/task/constitution consistency before code (FR-001–008).

## US1: Member size
- [x] T003 Add additive signatures and executed failing tests in tests/Codemetr.CSharp.Tests/MemberSizeTests.cs (FR-001–003).
- [x] T004 Implement src/Codemetr.CSharp/MemberSize.cs and integrate MemberCollector (FR-001–003).

## US2: Optional guards
- [x] T005 Execute failing guard/ratchet/override tests, then extend GuardRules, RuleIds and GuardEvaluator (FR-004).

## US3: Hotspots
- [x] T006 Execute failing JSON/text/filter/top tests in tests/Codemetr.CSharp.Tests/MemberHotspotTests.cs (FR-005–006).
- [x] T007 Implement MemberHotspot, ReportBuilder/CodemetrReport and text rendering (FR-005–006).

## US4: Adoption and delivery
- [x] T008 Add runnable examples/member-size and update README/measurement definitions/changelog (FR-007).
- [x] T009 Update codemetr/skills analysis, guard and tool references plus packaging validation (FR-007).
- [x] T010 Prepare specs/007-cover-member-link with owner, requirements, plan, next steps and acceptance (FR-008).
- [x] T011 Run full tests/build and unchanged published self guard, record evidence and source digest (FR-001–008).
- [x] T012 Commit/push and open reviewed PRs; merge only if required checks/reviews allow (FR-007).

Dependencies: T001→T002→T003→T004; T005 follows T004; T006 precedes T007.
T008–T010 follow the relevant contracts; T011 follows all implementation tasks.
T012 depends on verification. No parallel agent work is required.
