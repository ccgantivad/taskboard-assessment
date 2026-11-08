import { AbstractControl, ValidatorFn } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../api/models';
export function trimmedText(max: number): ValidatorFn {
  return (control: AbstractControl<string>) => {
    const value = control.value.trim();
    return !value ? { required: true } : value.length > max ? { maxlength: true } : null;
  };
}
// Match DataAnnotations EmailAddress after trimming; backend remains authoritative.
export const emailAddress: ValidatorFn = (control) => {
  const value = String(control.value).trim();
  const at = value.indexOf('@');
  return value.length <= 254 &&
    at > 0 &&
    at === value.lastIndexOf('@') &&
    at < value.length - 1 &&
    !/[\r\n]/.test(value)
    ? null
    : { email: true };
};
export const dateOnly: ValidatorFn = (control) => {
  const value = String(control.value);
  if (!value) return null;
  const parsed = new Date(`${value}T00:00:00Z`);
  return /^\d{4}-\d{2}-\d{2}$/.test(value) &&
    value.slice(0, 4) !== '0000' &&
    !Number.isNaN(parsed.getTime()) &&
    parsed.toISOString().slice(0, 10) === value
    ? null
    : { date: true };
};
export function problemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Unable to reach the API. Check your connection and try again.';
    const problem = error.error as ProblemDetails | null;
    const fields = problem?.errors && Object.values(problem.errors).flat().join(' ');
    return (
      fields ||
      problem?.detail ||
      problem?.title ||
      `Request failed (${error.status}). Please try again.`
    );
  }
  return 'Something went wrong. Please try again.';
}
