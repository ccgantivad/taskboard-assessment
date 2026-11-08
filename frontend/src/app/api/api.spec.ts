import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TasksApi, UsersApi } from './api';

describe('API contracts', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('sends all filters as query parameters', () => {
    TestBed.inject(TasksApi)
      .list({ userId: 7, status: 'InProgress', priority: 'High' })
      .subscribe();
    const req = http.expectOne((r) => r.url === '/api/tasks');
    expect(req.request.params.get('userId')).toBe('7');
    expect(req.request.params.get('status')).toBe('InProgress');
    expect(req.request.params.get('priority')).toBe('High');
    req.flush([]);
  });
  it('omits unset filters', () => {
    TestBed.inject(TasksApi).list().subscribe();
    const req = http.expectOne('/api/tasks');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([]);
  });
  it('uses exact creation and update routes with object-valued metadata', () => {
    TestBed.inject(TasksApi)
      .create({ title: 'Synthetic', assignedUserId: 1, additionalData: { priority: 'Medium' } })
      .subscribe();
    const create = http.expectOne('/api/tasks');
    expect(create.request.method).toBe('POST');
    expect(create.request.body.additionalData).toEqual({ priority: 'Medium' });
    create.flush({});
    TestBed.inject(TasksApi).updateStatus(8, 'Done').subscribe();
    const update = http.expectOne('/api/tasks/8/status');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({ status: 'Done' });
    update.flush({});
  });
  it('uses the users routes', () => {
    const api = TestBed.inject(UsersApi);
    api.list().subscribe();
    http.expectOne('/api/users').flush([]);
    api.create({ name: 'Synthetic', email: 'synthetic@example.test' }).subscribe();
    const req = http.expectOne('/api/users');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('synthetic@example.test');
    req.flush({});
  });
});
