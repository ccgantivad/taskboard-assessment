# Technical decisions for the timed assessment

These are proposed decisions. Keep this file aligned with the actual code and
record changes when evidence justifies a different choice.

| Decision | Reason | Tradeoff / revisit trigger |
| --- | --- | --- |
| One layered API project | Makes responsibilities visible and keeps delivery manageable | Split projects when independent deployment or stronger boundaries become necessary |
| Controller -> Service -> specific Repository -> EF/SQL | Keeps transport, rules and persistence understandable | EF already provides a unit of work; avoid a generic abstraction without a concrete need |
| Canonical SQL creation script and matching EF mappings | Meets evaluator's runnable SQL deliverable and prevents competing setup paths | Production needs a versioned migration/deployment strategy |
| JSON remains NVARCHAR(MAX), validated in SQL | Directly meets requested storage and database behavior | Flexible metadata needs validation/versioning as it grows |
| Native JSON priority filter in database | Demonstrates required SQL capability and avoids in-memory filtering | Add a measured computed-column/index strategy only if data size justifies it |
| Only Pending -> Done forbidden | Implements the exact explicit business rule | Confirm further workflow restrictions with the business rather than inventing them |
| New tasks Pending; same-state update idempotent | Predictable creation and repeated PUT behavior | Document these assumptions in README |
| Original status used as a concurrency token on every status update | Prevents overwriting a different state from the one validated, without changing the canonical schema | Returns 409 on conflict; does not detect changes away and back to the original status or provide general row versioning |
| Script-driven local SQL Server in Docker | Reproducible setup and actual SQL JSON verification | Requires compatible x64 host and adequate Docker resources |
| Focus on mandatory 24-hour scope | Produces complete evidence and leaves time for review | JWT, pagination, deployment and concurrency hardening remain explicit next steps |

## Lead responsibilities

Agree contracts before parallel work, assign file ownership, identify blockers
early and require executable evidence. Review rule placement, data integrity and
error behavior before UI polish. Keep delivery commands and decision records
current. Explain where the implementation has limits without claiming those
limits have already been solved.

## Production follow-up

Status updates now compare the persisted status with the state validated by the
service. For stronger detection of intervening changes, evaluate a rowversion
and a version-aware API contract. Add
authentication/authorization, a least-privilege database login, validated TLS,
pagination, operational telemetry and versioned database changes according to
actual deployment needs. These are discussion points, not implemented features.
