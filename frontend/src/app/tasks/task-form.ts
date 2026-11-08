import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import {
  FormControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TasksApi } from '../api/api';
import { AdditionalData, priorities, Priority, TaskItem, User } from '../api/models';
import { dateOnly, problemMessage, trimmedText } from '../shared/forms';
@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule],
  templateUrl: './task-form.html',
})
export class TaskForm {
  readonly users = input.required<readonly User[]>();
  readonly usersLoading = input(false);
  readonly created = output<TaskItem>();
  private readonly api = inject(TasksApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);
  readonly priorities = priorities;
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly form = this.fb.group({
    title: ['', trimmedText(200)],
    assignedUserId: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
    priority: this.fb.control<Priority | ''>(''),
    dueDate: ['', dateOnly],
    tags: [''],
  });
  submit() {
    if (this.submitting() || this.usersLoading() || !this.users().length) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const values = this.form.getRawValue();
    if (!this.users().some((user) => user.id === values.assignedUserId)) {
      this.form.controls.assignedUserId.setErrors({ unknown: true });
      return;
    }
    const additionalData: AdditionalData = {};
    if (values.priority) additionalData.priority = values.priority;
    if (values.dueDate) additionalData.dueDate = values.dueDate;
    const tags = values.tags
      .split(',')
      .map((tag) => tag.trim())
      .filter(Boolean);
    if (tags.length) additionalData.tags = tags;
    this.submitting.set(true);
    this.error.set('');
    this.api
      .create({
        title: values.title.trim(),
        assignedUserId: values.assignedUserId!,
        additionalData,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: (task) => {
          this.form.reset();
          this.created.emit(task);
        },
        error: (error) => this.error.set(problemMessage(error)),
      });
  }
}
