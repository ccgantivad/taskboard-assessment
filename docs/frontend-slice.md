# Angular frontend verification

Angular CLI 22.2.6 generated frontend/ with standalone components, routing, SCSS,
no SSR, and its Vitest unit-test builder. Installed Node is 24.16.0, npm 11.13.0.
The lockfile resolves Angular 22.2.5, TypeScript 6.0.3 and Vitest 5.1.0.
Strict TypeScript and strict template checking are explicitly enabled.

## Contract reconciliation

Before writing the client, the API document was aligned with actual backend
routes and DTOs: the implementation validates object-valued JsonElement metadata,
uses valid yyyy-MM-dd dates and string-array tags, and returns UTC timestamp
strings. The generated API launch profile defaults to 5237; the documented
explicit run command selects the verified port 5000. No backend code or SQL
scripts were changed for frontend integration.

Typed HttpClient services use relative /api URLs. proxy.conf.json targets
http://127.0.0.1:5000, and angular.json applies it to ng serve. No database
credentials are present in frontend files.

## Behavior and structure

- api/: typed DTOs and users/tasks HttpClient services.
- shared/forms: trimmed required/length validators, email/date validation and
  readable ProblemDetails handling.
- tasks/: task page, creation form and confirmed-response status control.
- users/: compact creation form; success reloads the assignee list.
- Task filters use backend query parameters. switchMap cancels stale responses.
  Mutation guards prevent duplicate writes; failed updates preserve the displayed
  status and show the backend message. Current filters remain after creation.
- Optional priority, due date and comma-separated tags map to object metadata.
- Loading, submitting, errors with retry, empty lists and no-user states are
  explicit. Every input has a label; focus indicators and a skip link support
  keyboard use. CSS adapts to single-column mobile layouts.

## Commands and actual results: 7 November 2026

Executed from the repository root:

```text
npx --yes --package @angular/cli@22 ng new taskboard --directory frontend --standalone --routing --strict --style=scss --ssr=false --skip-git --package-manager=npm --test-runner=vitest --defaults
npm --prefix frontend ci
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
npm --prefix frontend ls --depth=0
npm --prefix frontend audit --omit=dev
dotnet run --project backend/TaskBoard.Api --no-build --urls http://localhost:5000
npm --prefix frontend start -- --host 127.0.0.1
```

All 23 frontend tests pass across six files: API query parameters/routes,
metadata creation, normalization and validation, duplicate submission guards,
successful writes, failed status updates, unchanged status before confirmation,
loading/error/retry/no-user/empty states, assignee refresh and stale request
cancellation. Production build succeeds without warnings after moving shared
header styles out of the component budget. Runtime dependency audit reports
zero vulnerabilities. Backend remains unchanged from checkpoint 9320285; its
91-test result is recorded in tasks-slice.md.

Real HTTP checks through the Angular proxy returned 200 for the application,
users/tasks lists and combined filters; 201 for synthetic user/task creation;
200 for Pending -> InProgress -> Done; 409 for direct Pending -> Done; and 400
for whitespace-title creation. Direct SQL verified persistence, then removed
only the generated synthetic task and user. At the end of those HTTP checks,
the database contained three users
(including the pre-existing users-smoke record) and one original Pending task.
Both seed users remain intact; no test databases remain.

## Browser verification limitation

Automated browser interaction was attempted, but not executed.
The browser extension host was unavailable. Native Firefox control then stopped
because browser URL-policy enforcement is not supported for this Windows browser.
No further browser-control path was attempted. HTTP checks are not browser
rendering, keyboard, responsiveness or visual evidence.

## Manual browser results reported by the user: 7 November 2026

The user confirmed that these checks passed in a real browser:

- Task listing.
- Creation of a task with an assigned user.
- Status filtering.
- Pending -> InProgress -> Done.
- Persistence after reloading the page.
- Manual access to the public GitHub repository.

These are user-reported manual results. Automated browser interaction remained
unavailable; no automated browser walkthrough is claimed. The user did not
report the additional checks below (such as user creation, priority/user filters,
failure-state behavior, keyboard use or responsive layouts), so those remain
unverified. Manual demo record identifiers and cleanup were not reported.

## Extended manual browser checklist - remaining items unverified

Start API and frontend in separate terminals using the root README commands.

1. Open http://localhost:4200/. Verify task rows show assignee, status, local
   creation date and priority. Tab through labeled filters and controls.
2. Expand Add a person. Create a clearly synthetic example.test user. Confirm it
   appears in both assignee lists. Try blank/long names and invalid emails.
3. Create a task with a trimmed title, that assignee, optional priority/date/tags.
   Confirm creation succeeds, the form resets and the list refreshes under the
   current filters. Whitespace titles and titles over 200 trimmed characters
   must fail locally without a request.
4. Combine user/status/priority filters. Change filters quickly and confirm old
   results never replace the latest selection. Choose an empty result set.
5. Confirm Pending -> Done is disabled. Change Pending -> InProgress -> Done.
   Refresh and verify persistence. Confirm Done -> Pending remains available.
6. Stop the API, attempt refresh/status change and check useful error messages,
   retained status and retry behavior. Restart the API and retry.
7. Inspect at 1440px and 390px widths, including long titles and error messages.
   Verify no horizontal page overflow and visible keyboard focus.
8. Record and clean up only the synthetic manual demo records, preserving seeds.

The core checks listed above passed according to the user. This extended list
also includes checks beyond the reported scope; do not treat it as fully passed.
Any manual demo records should be identified before cleanup; no additional
cleanup was performed or claimed as part of this documentation-only update.
