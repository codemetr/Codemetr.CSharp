# Codemetr.CSharp Constitution

## Existing governance

This document records the repository's existing AGENTS.md and release rules for
Spec Kit; it introduces no new numeric gate or publication authority.

## Principles

1. Preserve documented measurement definitions, public constructor signatures,
   schema-1 fields, rule identities and baseline ratchets. Additive metrics have
   no new default guard thresholds.
2. Specify observable behaviour before implementation. Execute behavioural failing
   tests before adding it, then retain regression coverage and deterministic output.
3. Keep static metrics independent of test execution and runtime dependencies.
   Missing dynamic measurements are unavailable, never zero or successful.
4. Preserve source ownership, partial declarations, generated/inactive code limits,
   and explicit analysis provenance. Do not hide new debt or weaken guards.
5. Changes use a topic branch and PR. The actual current-head build and Codacy
   checks, enabled bot reviews and unresolved threads govern merge admission.
   Publish only through the existing release process when authorised.

## Workflow

Use Specify, Clarify, Plan, Tasks, Analyze, Implement and validation for the user
request. The request authorises these phases. Changes to governance require an
explicit maintainer decision; this codification does not override AGENTS.md.

Version: 1.0.0 | Codified: 2026-10-30T11:01:29Z
