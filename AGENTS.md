# TaskBoard assessment

Build the exact fullstack assessment within a 24-hour budget. Read
`docs/requirements.md`, `docs/api-contract.md` and `database/*.sql` first.

## Stack and structure

- .NET 10 Web API with controllers, EF Core 10 and SQL Server 2022.
- One backend project: Controllers, Services, Repositories, Data, Models, DTOs.
- Use `TaskItem` for the C# entity mapped to SQL table `Tasks`.
- Angular 22 standalone, strict TypeScript and conventional reactive forms.
- Node 24 LTS >= 24.15.0; use the generated package lockfile.
- SQL schema scripts are the canonical database setup for this exercise.
  Keep EF Fluent mappings aligned. Do not add `EnsureCreated` or migrations
  that compete with the script without deliberately changing the setup plan.

## Required behavior

- Create/list users. Create/list tasks with required existing assignee.
- Create tasks in Pending. Reject blank/whitespace titles.
- The only forbidden state transition is Pending -> Done. Other valid-enum
  transitions are allowed by the documented interpretation. Same-state PUT is
  idempotent. Enforce the exact case-sensitive strings Pending, InProgress and
  Done; reject unknown, differently cased and numeric values. Do not assume
  JsonStringEnumConverter alone enforces case sensitivity.
- Filter tasks by user and status; sort CreatedAt descending, then Id descending.
- Store object metadata in NVARCHAR(MAX); SQL Server must validate JSON.
- Priority filtering must execute in SQL Server using native JSON functions.
  Do not load all rows and filter JSON in C#.
- Use request/response DTOs, parameterized SQL, ProblemDetails and async I/O.
- Match API lengths/nullability with database constraints.

## Workflow

1. Inspect the repository and tooling before changes.
2. Implement one vertical slice and its meaningful checks at a time.
3. Explain changed files, commands actually run, results and remaining gaps.
4. Read the diff and run build/tests before reporting a slice complete.
5. Use independent read-only review for requirement and correctness gaps.
6. Never claim a command or runtime test passed unless it was actually run.

## Commands after scaffolding

From repository root:

```text
dotnet build TaskBoard.sln
dotnet test TaskBoard.sln
dotnet run --project backend/TaskBoard.Api --urls http://localhost:5000
npm --prefix frontend ci
npm --prefix frontend run build
npm --prefix frontend test -- --watch=false
```

Keep command paths synchronized if the final scaffold differs. Run database
integration checks against SQL Server, because SQLite/in-memory do not prove
SQL Server JSON behavior. Do not erase the named Docker volume during review.

## Scope

Use specific repositories only where useful. Keep controllers thin and the
transition rule in the service/domain. Avoid JWT, microservices, Kubernetes,
NgRx, generic repositories and MediatR within this assessment. Add optional
features only after every mandatory acceptance check passes.

Do not read or print .env, user-secrets, tokens or passwords. Do not publish,
push a repository or change global tooling settings without an explicit task.
