# Assessment acceptance checklist

This is the implementation checklist derived from the supplied assessment.
The API and Angular application are implemented. Automated and HTTP verification
results are recorded in the slice documents; manual browser verification and a
fresh-clone walkthrough remain pending. Use the root README for current setup.

| Requirement | Acceptance evidence |
| --- | --- |
| Create/list users, name and email | POST/GET /api/users; validation and persistence |
| Create/assign/list tasks | POST/GET /api/tasks with required existing assignedUserId |
| Change status | PUT /api/tasks/{id}/status persists allowed changes |
| Business rule | Pending -> Done fails and persisted status remains Pending |
| Layers and DTOs | Controller -> Service -> Repository -> EF/SQL; entity not exposed |
| Errors | ValidationProblemDetails/ProblemDetails; no internal details leaked |
| SQL model | Users/Tasks PKs, mandatory FK, at least one named index |
| SQL filtering | Filter by user/status; order by UTC creation timestamp |
| JSON | AdditionalData NVARCHAR(MAX), JSON validity constraint, native field query and filtering |
| JSON examples | Runnable ISJSON, JSON_VALUE, JSON_QUERY and OPENJSON examples |
| Angular | Task list, status filter, reactive creation form, user select, status update, basic errors |
| Delivery | Public repository, creation script, README setup/decisions/pending items |

## Documented decisions

- Initial status is Pending; API uses string enums.
- The prompt forbids only Pending -> Done. Other valid-enum transitions are
  allowed; same-state updates succeed without changing business state.
- Assignment happens at task creation. Reassignment is optional.
- Metadata is a JSON object; priority uses Low, Medium or High when supplied.
- Creation dates use UTC; task results sort newest first, Id descending for ties.
- User email validation is required by our API design. Email uniqueness is not
  required by the assessment and is not enforced by the starter schema.
- A small user creation form is recommended for demonstration, though the
  frontend section explicitly requires only the user selection list.
- Authentication, delete, pagination, deployment and JSON_MODIFY are optional.

## Final checks

- Reject title containing only whitespace, missing/unknown assignee and invalid enum.
- Verify Pending -> InProgress -> Done and reject direct Pending -> Done.
- Verify combined filters and JSON priority using actual SQL Server.
- Direct database checks reject malformed JSON and orphaned tasks.
- One browser walkthrough: create user, create task, filter, update status,
  observe a business-rule error and successful persistence after refresh.
- Run from a fresh clone using only README instructions. Record actual results.
