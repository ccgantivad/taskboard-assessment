# Users slice verification

The API consumes `ConnectionStrings:DefaultConnection` through standard ASP.NET
configuration, including the API project's user-secrets in Development. Never
commit a connection string. Local Docker SQL Server is published on
`tcp:127.0.0.1,1433`; use database `TaskBoard` for the development API.

Run from the repository root:

```text
dotnet restore TaskBoard.sln
dotnet build TaskBoard.sln
dotnet test TaskBoard.sln
dotnet run --project backend/TaskBoard.Api --urls http://localhost:5000
```

The SQL integration fixture consumes the same configuration normally, then
targets a generated `TaskBoardTests_<guid>` database. The configured SQL login
needs permission to create and drop that dedicated database. Tests execute a
copy of the canonical `database/001_create.sql`, changing only the database name;
no migrations or EF schema creation are used. Cleanup drops only the generated
test database, never the development database or Docker volume. Connection
failures fail tests rather than silently skipping them.

`POST /api/users` accepts name and email, trims both before validation, and
returns 201 with a user DTO and no Location header. `GET /api/users` returns
200 with an array ordered by ID. Duplicate emails are allowed. Validation
failures return 400 ValidationProblemDetails; unexpected failures return generic
500 ProblemDetails with a trace ID, including in Development.

Service tests can run without SQL Server:

```text
dotnet test TaskBoard.sln --filter FullyQualifiedName~UserServiceTests
```

SQL integration tests cover creation, normalized storage, duplicate emails,
length boundaries, invalid and malformed input without writes, list results,
and safe unexpected-error responses. HTTP smoke data should use a unique
synthetic name and an `example.test` email; preserve existing seed records.

## Verified locally on 7 November 2026

- Restore and build succeeded with zero warnings and errors.
- All 22 tests passed: seven service cases and 15 SQL-backed integration cases;
  none skipped. Initial connection failures were resolved by correcting local
  configuration, without changing application code or reading stored secrets.
- HTTP smoke: valid POST returned 201 without Location; GET returned 200;
  invalid POST returned 400 ProblemDetails without increasing the user count.
- Direct SQL confirmed the normalized synthetic user was persisted. The two
  original seed users and one task remain intact; one synthetic smoke user is
  retained in the development database. No generated test databases remain.
