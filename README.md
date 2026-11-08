# TaskBoard

Fullstack technical assessment with a .NET 10 / EF Core 10 controller-based API,
SQL Server 2022, and Angular 22 standalone components with typed reactive forms.

Users can be created and listed. Tasks support creation, assignment, combined
filters and status updates. Additional metadata is validated as a JSON object,
stored in NVARCHAR(MAX), and filtered inside SQL Server using JSON_VALUE.

## Prerequisites

- Git.
- .NET SDK **10.0.400**; global.json uses rollForward latestPatch.
- Node.js **24 LTS >=24.15.0**; verified with **24.16.0**, npm **11.13.0**.
- Docker Desktop with Linux containers / WSL2 on Windows x64, or equivalent
  supported x64 Docker setup. The SQL Server Linux image requires x86-64.
- An editor with a private .NET user-secrets editor, such as C# Dev Kit in VS Code.

The committed package-lock.json resolves Angular **22.2.5**, CLI **22.2.6**,
TypeScript **6.0.3** and Vitest **5.1.0**. EF Core SQL Server is pinned to
**10.0.11**. A direct Pkcs **9.0.22** override fixes the vulnerable transitive
9.0.11 dependency selected by SqlClient.

Run all commands below from the repository root. Docker / SQL commands are
written for PowerShell. No global Angular CLI installation is required.

## 1. Initialize SQL Server

Create the local environment file:

~~~powershell
Copy-Item .env.example .env
~~~

Edit .env privately and replace its MSSQL_SA_PASSWORD placeholder with a unique
strong local password. Never commit that file or paste the value into logs.
For a simple unquoted Compose environment value, avoid interpolation characters
such as $ and #. Reuse the same actual SQL login credentials in the API's private
configuration below; changing a container environment variable does not reset
the password stored in an already initialized SQL Server volume.

Start SQL Server and wait for the health check:

~~~powershell
docker compose up -d --wait
docker compose ps
~~~

The service publishes only 127.0.0.1:1433, mounts database/ read-only at /scripts
and persists data in the named sqlserver-data volume. Run these scripts
sequentially; stop if any command exits unsuccessfully:

~~~powershell
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/001_create.sql'
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/002_seed.sql'
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/003_json_examples.sql'
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/004_constraint_checks.sql'
~~~

The password expands only inside the container; these commands do not print it.

| Script | Purpose |
| --- | --- |
| database/001_create.sql | Canonical TaskBoard database, Users/Tasks tables, PK/FK, exact-case status and JSON object constraints, named task index |
| database/002_seed.sql | Two synthetic users and one Pending demo task; does not duplicate those seeds on rerun |
| database/003_json_examples.sql | Native JSON queries; optional JSON_MODIFY demonstration is rolled back |
| database/004_constraint_checks.sql | Reject malformed JSON, JSON arrays and orphan tasks; probe writes are rolled back |

SQL scripts are the **only schema setup path**. EF Fluent mappings match them;
there are no migrations or EnsureCreated calls. The creation script preserves
existing tables/data but does not upgrade a pre-existing incompatible schema.

Use docker compose down to stop and preserve data. Do not use down -v unless
you deliberately intend to erase the database volume.

## 2. Configure the API connection privately

The API uses standard ASP.NET configuration and its committed UserSecretsId.
Open the backend/TaskBoard.Api project's user-secrets editor and set
ConnectionStrings:DefaultConnection privately. A shape example, with a placeholder
that must be replaced only in your private editor:

~~~text
Server=tcp:127.0.0.1,1433;Database=TaskBoard;User Id=sa;Password=<your-local-password>;Encrypt=True;TrustServerCertificate=True
~~~

Do not put an actual password in this README, appsettings, frontend files, command
history or Git. user-secrets is a development convenience outside the repository,
not a production secret store. The application consumes it normally; verification
does not list or print stored values.

TrustServerCertificate=True and sqlcmd -C are for this local setup. Production
requires an appropriate application login, validated TLS and private configuration.

## 3. Start the API and frontend

Terminal one:

~~~powershell
dotnet restore TaskBoard.sln
dotnet build TaskBoard.sln
dotnet run --project backend/TaskBoard.Api --urls http://localhost:5000
~~~

Terminal two:

~~~powershell
npm --prefix frontend ci
npm --prefix frontend start
~~~

Open **http://localhost:4200/**. Angular uses relative /api URLs; its development
proxy targets http://127.0.0.1:5000. Keep the explicit API --urls argument:
the generated launch profile otherwise defaults to port 5237.

The production frontend build is in frontend/dist/taskboard/browser/.
Its host must route /api to the backend separately; the development proxy is
not included in static production output. No hosted application is deployed.

## API behavior

| Method | Route | Success |
| --- | --- | --- |
| POST | /api/users | 201 user DTO; no Location header |
| GET | /api/users | 200 user array |
| POST | /api/tasks | 201 task DTO; no Location header |
| GET | /api/tasks?userId=1&status=Pending&priority=High | 200 task array |
| PUT | /api/tasks/{id}/status | 200 updated task DTO |

Names and emails are trimmed before validation; limits are 120 and 254
characters. Email format is validated. Duplicate emails are allowed.

Task titles are required, trimmed and limited to 200 characters. An existing
assignee is mandatory. Tasks start Pending. Status accepts only exact
Pending, InProgress and Done strings; numeric, unknown and differently cased
values are rejected. Only Pending -> Done is forbidden (409). Other transitions
and same-state updates are allowed. Unknown tasks return 404.

Optional filters combine in SQL. Invalid, empty or repeated filter values return
400. Lists sort by CreatedAt descending, then Id descending. DTOs return assignee
name, an explicit UTC timestamp and object-valued additionalData.

Example creation body:

~~~json
{
  "title": "Synthetic assessment task",
  "assignedUserId": 1,
  "additionalData": {
    "priority": "High",
    "dueDate": "2026-11-10",
    "tags": ["review", "sql"],
    "metadata": { "source": "manual-demo" }
  }
}
~~~

Omitted additionalData becomes {}. Supplied metadata must be an object;
priority is Low/Medium/High, dueDate is a valid yyyy-MM-dd date, tags is a string
array, and metadata is a free object. Unknown fields are preserved; duplicate
top-level keys are rejected. The frontend exposes the three optional fields
priority, date and comma-separated tags.

Validation and expected errors use ProblemDetails with trace IDs. Unexpected
faults return generic 500 ProblemDetails without SQL or credential details.

## Architecture and decisions

~~~text
backend/TaskBoard.Api/
  Controllers -> Services -> specific Repositories -> EF Core / SQL Server
  DTOs        request/response boundaries
  Models      User and TaskItem (mapped to dbo.Users and dbo.Tasks)
  Data        DbContext, Fluent mappings and native JSON_VALUE translation
tests/TaskBoard.Tests/
  focused service tests and real SQL Server integration tests
frontend/src/app/
  api/        typed DTOs and HttpClient services
  shared/     validation and ProblemDetails messages
  tasks/      task page, creation form and status control
  users/      compact user creation form
~~~

- One layered API project keeps the timed assessment understandable. Controllers
  stay thin; the service owns transition rules; repositories own persistence.
- Reads use no-tracking DTO projection. Filtering, ordering and JSON priority
  queries execute in SQL Server with parameterized values.
- The frontend uses standalone components, strict TypeScript/templates and
  conventional typed reactive forms. switchMap cancels stale filter responses.
  Duplicate writes are guarded; status changes appear only after API success.
- Status is an EF concurrency token. Every write, including same-state requests,
  compares the persisted status with the one validated by the service. A
  conflicting state at save returns 409 without overwriting it.
- **Concurrency limitation:** the original-status check is not general row
  versioning. It cannot detect an intervening change away from and back to the
  original status. Stronger versioning requires a deliberate schema/API change.
- JSON stays NVARCHAR(MAX), validated by ISJSON(AdditionalData, OBJECT) in SQL.
  It is stored once, never as a doubly encoded JSON string.

See [API contract](docs/api-contract.md) and [technical decisions](docs/decisions.md).

## Native SQL JSON examples

Run in TaskBoard after seeding; these are also in
[database/003_json_examples.sql](database/003_json_examples.sql):

~~~sql
USE TaskBoard;
GO
DECLARE @UserId INT = (SELECT MIN(Id) FROM dbo.Users);
DECLARE @Status NVARCHAR(20) = N'Pending';
DECLARE @Priority NVARCHAR(20) = N'High';

SELECT t.Id, t.Title,
       ISJSON(t.AdditionalData, OBJECT) AS IsValidObject,
       JSON_VALUE(t.AdditionalData, '$.priority') AS Priority,
       JSON_QUERY(t.AdditionalData, '$.tags') AS Tags,
       JSON_QUERY(t.AdditionalData, '$.metadata') AS Metadata
FROM dbo.Tasks AS t
WHERE t.AssignedUserId = @UserId
  AND t.Status = @Status
  AND JSON_VALUE(t.AdditionalData, '$.priority') = @Priority
ORDER BY t.CreatedAt DESC, t.Id DESC;

SELECT t.Id, tag.[value] AS Tag
FROM dbo.Tasks AS t
CROSS APPLY OPENJSON(t.AdditionalData, '$.tags') AS tag
ORDER BY t.Id, tag.[key];
GO
~~~

The API maps JSON_VALUE into its EF query; integration tests capture the
executed SQL to verify native filtering, combined predicates, parameterization
and ordering. The examples alone are not the application-filter evidence.

## Tests and actual verification

~~~powershell
dotnet restore TaskBoard.sln
dotnet build TaskBoard.sln
dotnet test TaskBoard.sln
npm --prefix frontend ci
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
~~~

Recorded results from local implementation verification on 7 November 2026:

| Check | Actual result |
| --- | --- |
| Backend build | Passed, zero warnings/errors |
| Backend tests | **91 passed, 0 failed, 0 skipped** |
| Frontend tests | **23 passed, 0 failed, 0 skipped** across six files |
| Frontend production build | Passed without warnings |
| Dependency audits | No known findings in the recorded audits |
| Real API and Angular-proxy HTTP smoke | Create/list/filter/status and 400/409/404 behavior verified across the backend/frontend checks |
| Direct SQL persistence and constraints | Verified with actual SQL Server |
| Automated browser interaction | Unavailable; no browser walkthrough completed |
| Manual browser verification | **Pending** |
| Fresh-clone end-to-end walkthrough | **Pending** |

Backend tests include seven user service cases, 15 users SQL integration cases
and 69 tasks SQL integration cases. They cover rejected input without writes,
the full transition matrix, JSON filtering/ordering, status conflicts and an
assignee-deletion race. SQL fixtures consume private configuration normally,
create dedicated TaskBoardTests_<guid> databases from the canonical script and
drop only those databases. The test login must have create/drop database
permissions. They do not use SQLite or EF InMemory as SQL Server evidence.

Frontend tests cover validation, query parameters, creation, failed updates,
loading/error/retry/empty/no-user states, duplicate prevention, assignee refresh
and stale filter cancellation.

Browser extension access was unavailable; native Firefox automation also
stopped because URL-policy enforcement is unsupported for that Windows browser.
HTTP checks do not prove rendering, keyboard use or responsive behavior.
Follow the **unexecuted manual checklist** in
[frontend verification](docs/frontend-slice.md) before claiming those checks pass.

Synthetic HTTP smoke records from tasks/frontend verification were removed.
Existing development data was preserved: two seed users, the original Pending
seed task, and a pre-existing synthetic users-smoke user. No generated test
databases remain from the recorded runs; no demo credentials or database data
are included in this repository.

Detailed evidence: [users](docs/users-slice.md), [tasks](docs/tasks-slice.md),
[frontend](docs/frontend-slice.md).

## Delivery scope and remaining limitations

This repository is the source deliverable. The application is not deployed.
Authentication/authorization, pagination, deletion/reassignment UI, production
operations and stronger concurrency versioning are not implemented. Lists are
unpaginated, and metadata has no separate versioning strategy. These are
explicit scope limits rather than claims of production readiness.

Automated browser interaction was unavailable; manual browser verification and
a fresh-clone walkthrough are pending. No submission-v1 tag has been created;
tagging is reserved for after the manual check.

The root README is the runnable setup guide. START-HERE.txt, VALIDATION.txt
and docs/ai-prompts.txt retain historical starter-kit/preparation context;
do not rerun their application-scaffolding steps over this implementation.
