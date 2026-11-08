import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  combineLatest,
  finalize,
  map,
  of,
  startWith,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { TasksApi, UsersApi } from '../api/api';
import {
  priorities,
  Priority,
  statuses,
  TaskFilters,
  TaskItem,
  TaskStatus,
  User,
} from '../api/models';
import { problemMessage } from '../shared/forms';
import { TaskForm } from './task-form';
import { UserForm } from '../users/user-form';
import { StatusControl } from './status-control';
@Component({
  selector: 'app-task-page',
  imports: [DatePipe, ReactiveFormsModule, TaskForm, UserForm, StatusControl],
  templateUrl: './task-page.html',
  styleUrl: './task-page.scss',
})
export class TaskPage {
  private readonly tasksApi = inject(TasksApi);
  private readonly usersApi = inject(UsersApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly refresh = new Subject<void>();
  private readonly usersRefresh = new Subject<void>();
  readonly tasks = signal<TaskItem[]>([]);
  readonly users = signal<User[]>([]);
  readonly loading = signal(true);
  readonly usersLoading = signal(true);
  readonly error = signal('');
  readonly usersError = signal('');
  readonly notice = signal('');
  readonly updating = signal(new Set<number>());
  readonly statusErrors = signal<Record<number, string>>({});
  readonly statuses = statuses;
  readonly priorities = priorities;
  readonly filters = this.fb.group({
    status: this.fb.control<TaskStatus | ''>(''),
    userId: [''],
    priority: this.fb.control<Priority | ''>(''),
  });
  constructor() {
    combineLatest([
      this.filters.valueChanges.pipe(startWith(this.filters.getRawValue())),
      this.refresh.pipe(startWith(undefined)),
    ])
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set('');
        }),
        switchMap(() => {
          const value = this.filters.getRawValue();
          const filters: TaskFilters = {};
          if (value.status) filters.status = value.status;
          if (value.priority) filters.priority = value.priority;
          if (value.userId) filters.userId = Number(value.userId);
          return this.tasksApi.list(filters).pipe(
            map((items) => ({ items, error: '' })),
            catchError((error) => of({ items: [] as TaskItem[], error: problemMessage(error) })),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.tasks.set(result.items);
        this.error.set(result.error);
        this.loading.set(false);
      });
    this.usersRefresh
      .pipe(
        startWith(undefined),
        tap(() => this.usersLoading.set(true)),
        switchMap(() =>
          this.usersApi.list().pipe(
            map((users) => ({ users, error: '' })),
            catchError((error) => of({ users: [] as User[], error: problemMessage(error) })),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.users.set(result.users);
        this.usersError.set(result.error);
        this.usersLoading.set(false);
      });
  }
  reload() {
    this.refresh.next();
  }
  reloadUsers() {
    this.usersRefresh.next();
  }
  taskCreated() {
    this.notice.set('Task created. Your current filters still apply.');
    this.reload();
  }
  userCreated(user: User) {
    this.notice.set(`${user.name} was added. Assignees refreshed.`);
    this.reloadUsers();
  }
  updateStatus(task: TaskItem, status: TaskStatus) {
    if (this.updating().has(task.id)) return;
    this.updating.update((ids) => new Set(ids).add(task.id));
    this.statusErrors.update((errors) => ({ ...errors, [task.id]: '' }));
    this.tasksApi
      .updateStatus(task.id, status)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.updating.update((ids) => {
            const next = new Set(ids);
            next.delete(task.id);
            return next;
          });
        }),
      )
      .subscribe({
        next: (updated) => {
          this.tasks.update((tasks) =>
            tasks.map((item) => (item.id === updated.id ? updated : item)),
          );
          this.notice.set('Task status updated.');
          this.reload();
        },
        error: (error) =>
          this.statusErrors.update((errors) => ({ ...errors, [task.id]: problemMessage(error) })),
      });
  }
}
