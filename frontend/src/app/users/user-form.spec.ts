import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { UserForm } from './user-form';
describe('User creation form', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [UserForm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('rejects invalid fields', () => {
    const fixture = TestBed.createComponent(UserForm);
    const form = fixture.componentInstance;
    form.form.setValue({ name: ' ', email: 'invalid' });
    form.submit();
    http.expectNone('/api/users');
    expect(form.form.invalid).toBe(true);
  });
  it('trims, prevents duplicates, and emits the saved user', () => {
    const fixture = TestBed.createComponent(UserForm);
    const form = fixture.componentInstance;
    let id = 0;
    form.created.subscribe((user) => (id = user.id));
    form.form.setValue({ name: ' Synthetic Person ', email: ' synthetic@example.test ' });
    form.submit();
    form.submit();
    const req = http.expectOne('/api/users');
    expect(req.request.body).toEqual({ name: 'Synthetic Person', email: 'synthetic@example.test' });
    expect(id).toBe(0);
    req.flush({ id: 2, name: 'Synthetic Person', email: 'synthetic@example.test' });
    expect(id).toBe(2);
    expect(form.submitting()).toBe(false);
  });
  it('shows API errors and keeps values to retry', () => {
    const form = TestBed.createComponent(UserForm).componentInstance;
    form.form.setValue({ name: 'Synthetic', email: 'synthetic@example.test' });
    form.submit();
    http
      .expectOne('/api/users')
      .flush({ detail: 'Please try again later.' }, { status: 500, statusText: 'Error' });
    expect(form.error()).toContain('try again');
    expect(form.form.controls.name.value).toBe('Synthetic');
    expect(form.submitting()).toBe(false);
  });
});
