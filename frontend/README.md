# TaskBoard frontend

Angular 22 standalone components with strict TypeScript/templates, SCSS and
conventional typed reactive forms. The CLI-generated Vitest setup is retained.

See the [root README](../README.md) for prerequisites, private API configuration,
and commands to start the API and frontend in separate terminals. Relative /api
requests use [proxy.conf.json](proxy.conf.json) to reach http://127.0.0.1:5000;
production hosting requires separate API routing. Frontend files contain no
database credentials.

Run from the repository root:

```text
npm --prefix frontend ci
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
```

Recorded results: 23 frontend tests passed; the production build passed without
warnings. Output is frontend/dist/taskboard/browser/. See
[frontend verification](../docs/verification/frontend.md) for HTTP checks,
candidate-performed manual results and remaining checks. Automated browser
interaction was unavailable; no e2e runner is configured.
