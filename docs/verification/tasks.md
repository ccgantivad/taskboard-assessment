# Tasks API verification

The tasks API follows Controller -> Service -> specific Repository -> EF Core /
SQL Server. `TaskItem` maps to the existing `dbo.Tasks` table, including its
required user FK, named index, UTC default timestamp and JSON object constraint.
The canonical SQL scripts are unchanged. There are no migrations or
`EnsureCreated` calls.

## Behavior

- POST `/api/tasks` trims and validates a title up to 200 characters, requires
  an existing assignee and creates Pending tasks. Success is 201 without Location.
- GET `/api/tasks` combines optional positive `userId`, exact `status` and exact
  `priority` filters. Supplied empty, invalid or repeated filter values return
  400. A valid nonexistent user filter returns 200 with an empty array.
- Lists sort by CreatedAt descending, then Id descending. DTOs include the user
  name, UTC timestamps ending in `Z` and object-valued additionalData.
- PUT `/api/tasks/{id}/status` accepts only Pending, InProgress and Done. The
  service forbids only Pending -> Done (409); all other transitions, including
  repeated same-state requests, succeed with 200. Unknown tasks return 404.
- Validation, missing assignees and assignees removed during creation return
  400. Expected errors and unexpected faults use ProblemDetails with trace IDs;
  unexpected errors retain the generic 500 response.

## Metadata and SQL

Omitting additionalData defaults to `{}`. Explicit values must be JSON objects.
Optional known fields are validated: priority is Low/Medium/High, dueDate is a
valid `yyyy-MM-dd` date, tags is an array of strings, and metadata is a free JSON
object. Other properties are preserved. Duplicate top-level property names are
rejected so SQL JSON_VALUE and the response cannot disagree about priority.
Storage uses the object's raw JSON once in NVARCHAR(MAX).

The repository maps SQL Server's native JSON_VALUE function. Filtering, joins,
ordering and scalar projection execute in SQL; JSON is deserialized only after
the filtered result arrives. The integration test captures the executed command
and checks JSON_VALUE, combined SQL predicates, ordering and parameterized values.

## Concurrent updates

Status is an EF concurrency token without a schema change. Every status write,
including same-state PUT, uses the status read by the service in its SQL WHERE
predicate. A changed or removed row produces 409 rather than overwriting state
that differs from the validated state. Deterministic tests change InProgress to
Pending after the read and verify that stale Done and same-state InProgress
updates both fail, leaving Pending persisted.

This guards the current status value. It is not a general row version and cannot
detect an intervening change away from and back to the original status. A future
schema/versioning strategy would be needed for that stronger guarantee.

## Commands and actual results: 7 November 2026

From the repository root:

```text
dotnet restore TaskBoard.sln
dotnet build TaskBoard.sln
dotnet test TaskBoard.sln --no-build --no-restore --logger "console;verbosity=minimal"
dotnet run --project backend/TaskBoard.Api --no-build --urls http://localhost:5000
```

- Restore succeeded. Final build: zero warnings and errors.
- All 91 tests passed, none failed or skipped: seven user service tests,
  15 users SQL integration tests and 69 tasks SQL integration tests.
- The tasks checks cover valid creation, field/metadata rejection without rows,
  every transition and persisted state, exact statuses, unknown tasks, filter
  validation, native JSON filtering, date/ID ordering, empty lists, status races
  and the assignee-deletion race. SQL tests reuse dedicated GUID databases
  initialized from the canonical script; they never target development data.
- HTTP smoke returned 201 on create, 200 on combined-filter lists and allowed /
  same-state updates, 409 on Pending -> Done, 400 on invalid creation and filters,
  and 404 on unknown update. Fresh GETs and direct SQL verified persisted state.
- The synthetic HTTP task was deleted after verification. The original Pending
  seed task and both seed users remain unchanged. No generated test databases
  remain; the Docker volume was preserved.
- This verification covered the backend. Frontend results are recorded in
  [frontend verification](frontend.md).
