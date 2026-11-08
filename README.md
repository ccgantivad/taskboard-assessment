# TaskBoard — Michael Page Technical Assessment

**Candidate: Cristian Gantiva**

Task management application built with .NET 10, EF Core 10, SQL Server 2022
and Angular 22. It supports user creation, assigned tasks, status updates and
combined user/status/priority filters. Task metadata is stored as a JSON object
in NVARCHAR(MAX); priority filtering executes in SQL Server through JSON_VALUE.

## Prerequisites

- Git and .NET SDK **10.0.400** (global.json uses rollForward latestPatch).
- Node **24 LTS >=24.15.0**; verified with **24.16.0** and npm **11.13.0**.
- Docker with Linux containers on a supported x64 host; Docker Desktop with WSL2
  is suitable for Windows.
- A private .NET user-secrets editor, such as C# Dev Kit in VS Code.

The committed lockfile resolves Angular 22.2.5, CLI 22.2.6, TypeScript 6.0.3 and
Vitest 5.1.0. EF Core SQL Server is pinned to 10.0.11. Use the local CLI through
npm; no global Angular CLI installation is needed.

## Local setup

Run commands from the repository root. Docker commands below use PowerShell.

### Database

~~~powershell
Copy-Item .env.example .env
~~~

Edit .env privately and replace the MSSQL_SA_PASSWORD placeholder with a unique
strong password. For a simple unquoted Compose value, avoid $ and #. Do not
commit .env or put credentials in source files or command history.

~~~powershell
docker compose up -d --wait
docker compose ps
~~~

The service binds 127.0.0.1:1433, mounts database/ at /scripts read-only and uses
the persistent sqlserver-data volume. Run the scripts in order, stopping on any
failure; the password expands only inside the container:

~~~powershell
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/001_create.sql'
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/002_seed.sql'
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/003_json_examples.sql'
docker compose exec -T sqlserver bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i /scripts/004_constraint_checks.sql'
~~~

| Script | Purpose |
| --- | --- |
| [001_create.sql](database/001_create.sql) | Canonical schema, PK/FK, JSON/status constraints and named task index |
| [002_seed.sql](database/002_seed.sql) | Two demo users and one Pending task; repeatable without duplicate seeds |
| [003_json_examples.sql](database/003_json_examples.sql) | ISJSON, JSON_VALUE, JSON_QUERY and OPENJSON examples; JSON_MODIFY demo rolls back |
| [004_constraint_checks.sql](database/004_constraint_checks.sql) | Reject malformed/non-object JSON and orphan tasks; probes roll back |

SQL scripts are the schema setup path; there are no migrations or EnsureCreated
calls. Creation preserves existing tables/data but does not upgrade an
incompatible schema. Use docker compose down to preserve the volume; down -v
erases it. Changing the environment password does not reset the login stored in
an existing volume.

### Private API configuration

In the backend/TaskBoard.Api project's user-secrets editor, set
ConnectionStrings:DefaultConnection using the same SQL login credentials:

~~~text
Server=tcp:127.0.0.1,1433;Database=TaskBoard;User Id=sa;Password=<your-local-password>;Encrypt=True;TrustServerCertificate=True
~~~

Replace the placeholder only in private configuration. The committed
UserSecretsId enables normal ASP.NET configuration loading; actual secrets remain
outside the repository. TrustServerCertificate=True and sqlcmd -C are local
development settings, not production TLS configuration.

### API and frontend

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

Open **http://localhost:4200/**. Relative /api requests use the Angular development
proxy at http://127.0.0.1:5000. The explicit API --urls argument overrides the
generated launch profile's port 5237. Production output is
frontend/dist/taskboard/browser/; a production host must route /api separately.

## API summary

| Method | Route | Success |
| --- | --- | --- |
| POST / GET | /api/users | 201 user DTO / 200 array |
| POST / GET | /api/tasks | 201 task DTO / 200 array |
| PUT | /api/tasks/{id}/status | 200 updated task DTO |

GET /api/tasks accepts optional userId, status and priority query parameters.
Creation returns no Location header. Names, emails and titles are trimmed;
limits are 120, 254 and 200 characters. Email format is validated; duplicate
emails are allowed. Tasks require an existing assignee and start Pending.

Only exact Pending, InProgress and Done strings are accepted. Pending -> Done
returns 409; other transitions and same-state updates are allowed. Unknown tasks
return 404. Invalid fields/filters return 400. ProblemDetails responses contain
trace IDs; unexpected faults return generic 500 messages.

Task DTOs include assignedUserName, a UTC createdAt timestamp and object-valued
additionalData. Optional metadata fields are priority (Low/Medium/High), dueDate
(valid yyyy-MM-dd), tags (string array) and metadata (free object). Omitted
additionalData defaults to {}. Lists sort by CreatedAt descending, then Id
descending. See the [API contract](docs/api-contract.md) for request examples.

## Architecture and decisions

- One API project: Controllers -> Services -> specific Repositories -> EF/SQL.
  DTOs define transport boundaries; TaskItem maps to dbo.Tasks.
- Services own transition rules. No-tracking reads project DTO fields; filters,
  ordering and native JSON_VALUE queries execute in SQL with parameterized values.
- Fluent mappings follow the canonical SQL scripts. JSON is validated with
  ISJSON(AdditionalData, OBJECT) and stored once in NVARCHAR(MAX).
- Angular uses standalone components, strict TypeScript/templates and typed
  reactive forms. switchMap cancels stale filters; submission guards prevent
  duplicate writes. Status changes display only after API confirmation.
- Original status is an EF concurrency token. Conflicting persisted state returns
  409, including on same-state writes. This is not general row versioning and
  cannot detect a change away from and back to the original status.

See [technical decisions](docs/decisions.md) and
[SQL JSON examples](database/003_json_examples.sql).

## Tests and verification

~~~powershell
dotnet test TaskBoard.sln
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
~~~

Recorded verification on 7 November 2026:

| Evidence | Result |
| --- | --- |
| Automated backend tests | **91 passed**, zero failures/skips; real SQL Server integration |
| Automated frontend tests | **23 passed**, zero failures/skips; CLI Vitest setup |
| Backend / frontend builds | Passed without warnings/errors |
| API / development-proxy HTTP checks | Creation, listing, filters, updates and expected errors verified |
| Candidate-performed manual checks | Listing, assigned task creation, status filtering, Pending -> InProgress -> Done, persistence after reload, public repository access: passed |
| Automated browser interaction | Unavailable; no automated walkthrough completed |
| Pending checks | Fresh-clone walkthrough and unreported extended browser checks, including keyboard/responsive verification |

SQL tests create dedicated GUID databases from the canonical script and clean
them up; the test login needs create/drop database permissions. They do not use
SQLite or EF InMemory as evidence of SQL Server behavior.

Detailed results and remaining checks:
[users](docs/verification/users.md),
[tasks](docs/verification/tasks.md),
[frontend](docs/verification/frontend.md).

## Limitations

The application is not deployed. Authentication/authorization, pagination,
deletion/reassignment UI, production operations and stronger concurrency
versioning are outside the implemented scope. Metadata has no separate versioning
strategy. The original-status concurrency limitation and pending checks above
remain explicit. The existing submission-v1 tag preserves the manually verified
submission snapshot; subsequent documentation cleanup does not move that tag.
