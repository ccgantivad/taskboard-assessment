# API contract

The users and tasks backend routes below are implemented and verified locally.
Start the API explicitly with `dotnet run --project backend/TaskBoard.Api --urls
http://localhost:5000`. The generated launch profile alone defaults to 5237;
the explicit command selects the verified integration port 5000. Angular runs
at `http://localhost:4200` and proxies relative `/api` requests to
`http://127.0.0.1:5000`. Production hosting must route `/api` to the API separately.

| Method and route | Request | Success |
| --- | --- | --- |
| POST /api/users | name, email | 201 with UserDto |
| GET /api/users | none | 200 UserDto[] |
| POST /api/tasks | title, assignedUserId, additionalData | 201 with TaskDto |
| GET /api/tasks | optional userId, status, priority query parameters | 200 TaskDto[] |
| PUT /api/tasks/{id}/status | status | 200 updated TaskDto |

Add GET `/api/users/{id}` and GET `/api/tasks/{id}` only if using those routes as
the 201 Location targets. Otherwise return 201 without inventing an unavailable
resource URL. Empty filtered lists return 200 with `[]`.

## Request examples

```json
{ "name": "Cristian", "email": "cristian@example.test" }
```

```json
{
  "title": "Review SQL examples",
  "assignedUserId": 1,
  "additionalData": {
    "priority": "High",
    "dueDate": "2026-11-08",
    "tags": ["sql", "assessment"],
    "metadata": { "source": "manual" }
  }
}
```

```json
{ "status": "InProgress" }
```

The implemented request uses an object-valued JsonElement. Omitted additionalData
defaults to `{}`; explicit null, arrays and scalars are rejected. Optional known
fields are priority (`Low`, `Medium`, `High`), dueDate (valid `yyyy-MM-dd`), tags
(string array), and metadata (free JSON object). Duplicate top-level keys are
rejected; unknown fields are preserved. Storage contains JSON once and responses
return an object. The frontend exposes priority, dueDate and comma-separated tags.

GET example:
`/api/tasks?userId=1&status=Pending&priority=High`

Validate status body and query values against the exact strings Pending,
InProgress and Done using case-sensitive comparison. Reject numeric input,
unknown strings and differently cased strings. A request DTO with a string and
an explicit ordinal allowlist is a simple option. JsonStringEnumConverter with
allowIntegerValues:false rejects integers but still accepts names without
regard to case, so it does not enforce this contract by itself. An invalid query
must not silently become an absent filter. TaskDto includes id, title, status,
assignedUserId, assignedUserName, createdAt and additionalData. Use ISO 8601 UTC.

## Error contract

| Condition | Status |
| --- | --- |
| Invalid title, fields, enum, query or unknown assignee | 400 |
| Task not found for status update | 404 |
| Forbidden Pending -> Done transition | 409 |
| Concurrent status change detected during update | 409 |
| Unhandled server fault | 500 with generic message and traceId |

Use application/problem+json with type, title, status, detail and traceId;
validation errors may additionally include an errors dictionary. The frontend
must read a ProblemDetails response and show a useful message.

## Data access

Use AsNoTracking and DTO projection for reads. Keep filter composition on the
database side. For priority, either map JSON_VALUE as an EF DbFunction or use
a deliberately parameterized SQL query that returns Tasks. Verify translation
against SQL Server. SQL examples alone do not prove the application filter runs
in the database. Never concatenate user values into SQL.

Only the service/domain owns status-transition validation; the UI mirrors the
rule for usability. A database FK remains necessary even after checking user
existence in the service. Map expected FK conflicts to consistent errors. Email
uniqueness is not required and is not enforced by the starter schema.

Match these limits: name 120, email 254 and title 200 characters. The schema uses
an exact-case status constraint; only Pending, InProgress and Done are valid.
