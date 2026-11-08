# TaskBoard

A small task-management assessment: .NET 10 controllers and EF Core 10 with SQL
Server 2022, plus Angular 22 standalone components and typed reactive forms.

## Local prerequisites

- .NET SDK 10.0.400 (global.json rolls forward within its patch band).
- Node 24 LTS >=24.15.0; verified here with 24.16.0 and npm 11.13.0.
- Docker with Linux containers on x64. Keep the named SQL Server volume.
- Configure local credentials privately; never commit .env or user-secrets.

## Database and configuration

Follow [START-HERE.txt](START-HERE.txt), steps 1â€“3, for private local credential
setup, Docker startup, and sequential execution of database scripts 001â€“004.
The SQL scripts are canonical; EF must not create or migrate this schema.

The API uses normal ASP.NET configuration. Set
`ConnectionStrings:DefaultConnection` privately through the API project's
user-secrets editor. The local server address is `tcp:127.0.0.1,1433`, database
`TaskBoard`. No connection-string value belongs in the frontend.

## Start in two terminals

From the repository root, terminal one:

```text
dotnet restore TaskBoard.sln
dotnet build TaskBoard.sln
dotnet run --project backend/TaskBoard.Api --urls http://localhost:5000
```

Terminal two:

```text
npm --prefix frontend ci
npm --prefix frontend start
```

Open http://localhost:4200/. Angular proxies relative /api calls to
http://127.0.0.1:5000. The explicit API command overrides the generated launch
profile's port 5237. Production hosting must configure its own /api routing;
the development proxy is not part of the production build.

## Verification

```text
dotnet test TaskBoard.sln
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
```

SQL integration tests consume configuration normally, create isolated GUID test
databases from the canonical script, and drop only those test databases.
The configured login requires create/drop database permissions for these tests.

See [users verification](docs/users-slice.md),
[tasks verification](docs/tasks-slice.md), and
[frontend verification and manual browser steps](docs/frontend-slice.md).
Current automated results are 91 backend and 23 frontend tests passing.
The frontend production build passes; full browser interaction is unexecuted
because browser automation access is unavailable.

## Decisions and remaining scope

See [API contract](docs/api-contract.md) and [decisions](docs/decisions.md).
Status updates compare the original persisted status before saving; this is not
general row versioning and does not detect a change away and back.
Authentication, deployment, pagination and stronger concurrency versioning remain
outside this assessment implementation. Nothing has been published or pushed.
A fresh-clone walkthrough and the manual browser checklist remain to be done.
