# Proposed API contract

These are design decisions for implementation, not already implemented routes.
Base URL: `http://localhost:5000`. Angular dev server: `http://localhost:4200`.
Configure the API to allow that exact dev origin through CORS or use a local
Angular proxy. Keep production origins separate.

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

Prefer a typed AdditionalDataDto for priority, dueDate and tags, with a free
metadata dictionary. An object-valued JsonElement is an alternative if its shape
is explicitly validated. Do not expose a doubly encoded JSON string. Serialize
once for NVARCHAR(MAX) storage and return an object in the response. Reject a
non-object payload and invalid known metadata fields. Omitted additionalData
may default to `{}`. Free metadata fields stay free.

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
