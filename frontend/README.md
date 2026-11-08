# TaskBoard frontend

Angular 22 standalone, strict TypeScript/templates, SCSS and conventional typed
reactive forms. Vitest uses the Angular CLI-generated unit-test builder.

From the repository root, start the API in terminal one:

```text
dotnet run --project backend/TaskBoard.Api --urls http://localhost:5000
```

In terminal two:

```text
npm --prefix frontend ci
npm --prefix frontend start
```

Open http://localhost:4200/. Relative /api calls use proxy.conf.json to reach
http://127.0.0.1:5000. Backend database setup and private configuration are
required first; see ../README.md. Do not add credentials to frontend files.
Production hosting needs its own /api routing.

Verification from the repository root:

```text
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
```

23 unit/component tests cover validation, requests, successful creation,
failed status changes, duplicate prevention, loading/errors and stale filters.
Production output is frontend/dist/taskboard/browser/.
See ../docs/frontend-slice.md for actual results, HTTP proxy checks, cleanup,
and the unexecuted manual browser checklist. No e2e runner is configured.
