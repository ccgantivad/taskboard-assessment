import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { UsersApi } from '../api/api';
import { User } from '../api/models';
import { emailAddress, problemMessage, trimmedText } from '../shared/forms';
@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule],
  template: ` <form [formGroup]="form" (ngSubmit)="submit()" aria-label="Create user">
    <fieldset [disabled]="submitting()">
      <label for="user-name">Name</label
      ><input id="user-name" formControlName="name" autocomplete="name" />
      @if (form.controls.name.touched && form.controls.name.invalid) {
        <small class="field-error">Enter a name up to 120 characters after trimming.</small>
      }
      <label for="user-email">Email</label
      ><input id="user-email" type="email" formControlName="email" autocomplete="email" />
      @if (form.controls.email.touched && form.controls.email.invalid) {
        <small class="field-error">Enter a valid email up to 254 characters.</small>
      }
      <button type="submit" class="secondary full" [disabled]="submitting()">
        {{ submitting() ? 'Adding user…' : 'Add user' }}
      </button>
    </fieldset>
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }
  </form>`,
})
export class UserForm {
  readonly created = output<User>();
  readonly submitting = signal(false);
  readonly error = signal('');
  private readonly api = inject(UsersApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);
  readonly form = this.fb.group({
    name: ['', trimmedText(120)],
    email: ['', [trimmedText(254), emailAddress]],
  });
  submit() {
    if (this.submitting()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.error.set('');
    this.api
      .create({ name: value.name.trim(), email: value.email.trim() })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: (user) => {
          this.form.reset();
          this.created.emit(user);
        },
        error: (error) => this.error.set(problemMessage(error)),
      });
  }
}
