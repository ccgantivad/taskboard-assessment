# TaskBoard contributor guidance

Read [requirements](docs/requirements.md), [API contract](docs/api-contract.md),
[decisions](docs/decisions.md) and database/*.sql before changing behavior.
Use the root [README](README.md) for setup.

## Implementation constraints

- .NET 10 controllers, EF Core 10, SQL Server 2022; one layered API project:
  Controllers -> Services -> specific Repositories, plus Data, Models and DTOs.
- TaskItem maps to dbo.Tasks. Canonical SQL scripts own schema setup; keep Fluent
  mappings aligned. Do not add migrations or EnsureCreated without an explicit
  change to the setup plan.
- Angular 22 standalone, strict TypeScript and conventional typed reactive forms.
  Use Node 24 LTS >=24.15.0 and preserve package-lock.json.
- Require an existing task assignee; trim/validate input against SQL lengths.
  Tasks start Pending. Accept only exact Pending, InProgress and Done strings.
  Only Pending -> Done is forbidden; same-state PUT is idempotent.
- Keep transition validation in services and preserve original-status conflict
  detection and its documented limitation. Return DTOs and ProblemDetails.
- Combine user/status/priority filters in SQL; order CreatedAt descending, then
  Id descending. Store object JSON in NVARCHAR(MAX), validate it in SQL Server,
  and filter priority with native JSON_VALUE. Use parameterized SQL, async I/O
  and CancellationToken.

## Verification and scope

Inspect Git status and preserve unrelated work. Review the diff and run checks
appropriate to the change. Documentation-only changes need link/reference and
whitespace checks, not unchanged application test suites. Use independent
read-only review for requirement/correctness gaps. Never claim unrun checks passed.

From the repository root:

~~~text
dotnet build TaskBoard.sln
dotnet test TaskBoard.sln
npm --prefix frontend ci
npm --prefix frontend run build
npm --prefix frontend test -- --watch=false
~~~

Run SQL integration tests against actual SQL Server in dedicated test databases;
do not erase the named Docker volume. Verification records:
[users](docs/verification/users.md), [tasks](docs/verification/tasks.md),
[frontend](docs/verification/frontend.md).

Keep scope limited; avoid generic repositories, MediatR, NgRx, microservices and
unrequested infrastructure. Do not read or print .env, stored secrets, tokens or
passwords. Do not publish, push, alter existing tags or change global tooling
settings without explicit authorization.
