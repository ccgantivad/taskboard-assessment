import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TaskForm } from './task-form';
describe('Task creation form', () => {
  let fixture: ComponentFixture<TaskForm>;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TaskForm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(TaskForm);
    fixture.componentRef.setInput('users', [
      { id: 1, name: 'Synthetic', email: 'synthetic@example.test' },
    ]);
    fixture.detectChanges();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('rejects whitespace titles and missing or unavailable assignees', () => {
    const component = fixture.componentInstance;
    component.form.patchValue({ title: ' ', assignedUserId: 1 });
    component.submit();
    component.form.patchValue({ title: 'Valid', assignedUserId: null });
    component.submit();
    component.form.patchValue({ assignedUserId: 99 });
    component.submit();
    expect(component.form.controls.assignedUserId.invalid).toBe(true);
    http.expectNone('/api/tasks');
  });
  it('normalizes metadata, prevents duplicates and emits only after success', () => {
    const component = fixture.componentInstance;
    const emitted: unknown[] = [];
    component.created.subscribe((value) => emitted.push(value));
    component.form.setValue({
      title: '  Synthetic task  ',
      assignedUserId: 1,
      priority: 'High',
      dueDate: '2026-11-10',
      tags: ' sql, review, , ',
    });
    component.submit();
    component.submit();
    expect(component.submitting()).toBe(true);
    expect(emitted).toEqual([]);
    const req = http.expectOne('/api/tasks');
    expect(req.request.body).toEqual({
      title: 'Synthetic task',
      assignedUserId: 1,
      additionalData: { priority: 'High', dueDate: '2026-11-10', tags: ['sql', 'review'] },
    });
    req.flush({ id: 8, title: 'Synthetic task' });
    expect(emitted).toHaveLength(1);
    expect(component.submitting()).toBe(false);
    expect(component.form.controls.title.value).toBe('');
  });
  it('omits optional metadata and preserves input after a server validation error', () => {
    const component = fixture.componentInstance;
    component.form.patchValue({ title: 'Synthetic', assignedUserId: 1 });
    component.submit();
    const req = http.expectOne('/api/tasks');
    expect(req.request.body.additionalData).toEqual({});
    req.flush(
      { errors: { AssignedUserId: ['The assigned user does not exist.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(component.error()).toContain('assigned user');
    expect(component.form.controls.title.value).toBe('Synthetic');
    expect(component.submitting()).toBe(false);
  });
  it('disables creation when there are no users or assignees are loading', () => {
    fixture.componentRef.setInput('users', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Create a user');
    expect(fixture.nativeElement.querySelector('fieldset').disabled).toBe(true);
    fixture.componentInstance.submit();
    http.expectNone('/api/tasks');
    fixture.componentRef.setInput('usersLoading', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('fieldset').disabled).toBe(true);
  });
});
