import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TaskPage } from './task-page';
import { TaskItem } from '../api/models';
const task: TaskItem = {
  id: 1,
  title: 'Synthetic task',
  status: 'Pending',
  assignedUserId: 1,
  assignedUserName: 'Synthetic',
  createdAt: '2026-11-07T12:00:00Z',
  additionalData: { priority: 'High' },
};
describe('Task page', () => {
  let fixture: ComponentFixture<TaskPage>;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TaskPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(TaskPage);
    fixture.detectChanges();
  });
  afterEach(() => http.verify({ ignoreCancelled: true }));
  function initial(items: TaskItem[] = [task]) {
    http
      .expectOne('/api/users')
      .flush([{ id: 1, name: 'Synthetic', email: 'synthetic@example.test' }]);
    http.expectOne('/api/tasks').flush(items);
    fixture.detectChanges();
  }
  it('shows loading then tasks with assignee, priority and status', () => {
    expect(fixture.nativeElement.textContent).toContain('Loading tasks');
    initial();
    expect(fixture.componentInstance.loading()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Synthetic task');
    expect(fixture.nativeElement.textContent).toContain('High');
  });
  it('cancels stale filter requests and sends combined filters', () => {
    initial();
    const page = fixture.componentInstance;
    page.filters.controls.status.setValue('Pending');
    const stale = http.expectOne((r) => r.url === '/api/tasks');
    expect(page.loading()).toBe(true);
    page.filters.patchValue({ userId: '1', priority: 'High' });
    expect(stale.cancelled).toBe(true);
    const latest = http.expectOne((r) => r.url === '/api/tasks');
    expect(latest.request.params.get('status')).toBe('Pending');
    expect(latest.request.params.get('userId')).toBe('1');
    expect(latest.request.params.get('priority')).toBe('High');
    latest.flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No tasks here yet');
  });
  it('preserves the displayed status on failure and prevents duplicate writes', () => {
    initial();
    const page = fixture.componentInstance;
    page.updateStatus(task, 'InProgress');
    page.updateStatus(task, 'InProgress');
    const req = http.expectOne('/api/tasks/1/status');
    expect(page.tasks()[0].status).toBe('Pending');
    expect(page.updating().has(1)).toBe(true);
    req.flush(
      { detail: 'The task was changed concurrently. Refresh and try again.' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();
    expect(page.tasks()[0].status).toBe('Pending');
    expect(page.updating().has(1)).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('changed concurrently');
  });
  it('updates status only after success then reloads using current filters', () => {
    initial();
    const page = fixture.componentInstance;
    page.updateStatus(task, 'InProgress');
    expect(page.tasks()[0].status).toBe('Pending');
    http.expectOne('/api/tasks/1/status').flush({ ...task, status: 'InProgress' });
    expect(page.tasks()[0].status).toBe('InProgress');
    http.expectOne('/api/tasks').flush([{ ...task, status: 'InProgress' }]);
    expect(page.loading()).toBe(false);
  });
  it('refreshes assignees after user creation and tasks after creation', () => {
    initial();
    const page = fixture.componentInstance;
    page.userCreated({ id: 2, name: 'New Synthetic', email: 'new@example.test' });
    http
      .expectOne('/api/users')
      .flush([{ id: 2, name: 'New Synthetic', email: 'new@example.test' }]);
    expect(page.users()[0].id).toBe(2);
    page.taskCreated();
    http.expectOne('/api/tasks').flush([]);
    expect(page.tasks()).toEqual([]);
  });
  it('shows task errors with retry and a no-user state', () => {
    http.expectOne('/api/users').flush([]);
    http
      .expectOne('/api/tasks')
      .flush({ detail: 'Temporarily unavailable.' }, { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Temporarily unavailable');
    expect(fixture.nativeElement.textContent).toContain('Create a user');
    fixture.componentInstance.reload();
    http.expectOne('/api/tasks').flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No tasks here yet');
  });
  it('shows assignee loading errors and supports retry', () => {
    http
      .expectOne('/api/users')
      .flush({ detail: 'Users unavailable.' }, { status: 500, statusText: 'Error' });
    http.expectOne('/api/tasks').flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Users unavailable');
    fixture.componentInstance.reloadUsers();
    http.expectOne('/api/users').flush([]);
    expect(fixture.componentInstance.usersError()).toBe('');
  });
});
