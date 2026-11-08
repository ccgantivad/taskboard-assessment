import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CreateTask, CreateUser, TaskFilters, TaskItem, TaskStatus, User } from './models';

@Injectable({ providedIn: 'root' })
export class TasksApi {
  private readonly http = inject(HttpClient);
  list(filters: TaskFilters = {}) {
    let params = new HttpParams();
    if (filters.userId !== undefined) params = params.set('userId', filters.userId);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.priority) params = params.set('priority', filters.priority);
    return this.http.get<TaskItem[]>('/api/tasks', { params });
  }
  create(request: CreateTask) {
    return this.http.post<TaskItem>('/api/tasks', request);
  }
  updateStatus(id: number, status: TaskStatus) {
    return this.http.put<TaskItem>(`/api/tasks/${id}/status`, { status });
  }
}
@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  list() {
    return this.http.get<User[]>('/api/users');
  }
  create(request: CreateUser) {
    return this.http.post<User>('/api/users', request);
  }
}
