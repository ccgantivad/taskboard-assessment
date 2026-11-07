# Technical lead review checklist

| Review area | Evidence required before delivery |
| --- | --- |
| Requirement ownership | Every mandatory row in requirements.md has an owner and passing evidence |
| API contract | DTOs, status codes, validation, filtering and 201 Location match actual routes |
| Business state | Direct Pending -> Done rejected with no persisted side effect; repeated PUT predictable |
| Database integrity | PK/FK/index present; actual SQL Server rejects malformed JSON and orphaned tasks |
| SQL JSON | Native queries and application priority filtering run inside SQL Server |
| Frontend | Reactive form, assignee list, filter, status change, empty/loading/error states work |
| Failure handling | Useful ProblemDetails; no leaked internals; unknown task returns 404 |
| Reproducibility | Fresh clone starts using only README, documented versions and scripts |
| Validation honesty | Build/test commands and results recorded; unrun checks identified explicitly |
| Secrets | .env, user-secrets, credentials and tokens absent from staged/public files |
| Decisions | Scope, assumptions, tradeoffs and pending features match actual implementation |
| Risk ownership | Remaining issues have impact, owner and next action; no vague "production ready" claim |

## Review workflow

1. Freeze the contract and give backend/frontend owners non-overlapping paths.
2. Integrate a small completed slice and run its acceptance checks.
3. Ask a separate read-only reviewer to find correctness and delivery gaps.
4. Resolve critical/high findings before polishing optional features.
5. Run the clean setup walkthrough, then review the final staged diff.

For lead defense, explain a concrete choice, its benefit, its limitation and the
condition that would justify changing it. Use implementation evidence rather
than adding architecture solely for the role title.
