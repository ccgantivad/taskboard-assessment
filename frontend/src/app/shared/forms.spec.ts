import { FormControl } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { dateOnly, emailAddress, problemMessage, trimmedText } from './forms';
describe('Backend-aligned validation', () => {
  it('rejects whitespace and validates trimmed length', () => {
    const control = new FormControl('', { nonNullable: true, validators: trimmedText(200) });
    control.setValue(' \t\n');
    expect(control.invalid).toBe(true);
    control.setValue(' ' + 'x'.repeat(200) + ' ');
    expect(control.valid).toBe(true);
    control.setValue('x'.repeat(201));
    expect(control.invalid).toBe(true);
  });
  it('validates names and emails at their API boundaries', () => {
    const name = new FormControl('n'.repeat(120), {
      nonNullable: true,
      validators: trimmedText(120),
    });
    expect(name.valid).toBe(true);
    name.setValue('n'.repeat(121));
    expect(name.invalid).toBe(true);
    const email = new FormControl(' synthetic@example.test ', emailAddress);
    expect(email.valid).toBe(true);
    email.setValue('a@@example.test');
    expect(email.invalid).toBe(true);
    email.setValue('x'.repeat(242) + '@example.test');
    expect(email.invalid).toBe(true);
  });
  it('accepts omitted dates and real leap days but rejects invalid dates', () => {
    const control = new FormControl('', dateOnly);
    expect(control.valid).toBe(true);
    control.setValue('2028-02-29');
    expect(control.valid).toBe(true);
    for (const value of ['2026-02-29', '2026-02-30', '0000-01-01', '2026-1-1', 'invalid']) {
      control.setValue(value);
      expect(control.invalid).toBe(true);
    }
  });
  it('shows validation details, conflict details, generic and network failures', () => {
    expect(
      problemMessage(
        new HttpErrorResponse({ status: 400, error: { errors: { Title: ['Title required.'] } } }),
      ),
    ).toBe('Title required.');
    expect(
      problemMessage(
        new HttpErrorResponse({ status: 409, error: { detail: 'Refresh and try again.' } }),
      ),
    ).toBe('Refresh and try again.');
    expect(
      problemMessage(
        new HttpErrorResponse({ status: 500, error: { title: 'Server unavailable' } }),
      ),
    ).toBe('Server unavailable');
    expect(problemMessage(new HttpErrorResponse({ status: 0 }))).toContain('Unable to reach');
    expect(problemMessage(null)).toContain('Something went wrong');
  });
});
