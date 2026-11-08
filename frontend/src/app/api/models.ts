export const statuses = ['Pending', 'InProgress', 'Done'] as const;
export type TaskStatus = (typeof statuses)[number];
export const priorities = ['Low', 'Medium', 'High'] as const;
export type Priority = (typeof priorities)[number];
export interface User {
  id: number;
  name: string;
  email: string;
}
export interface AdditionalData {
  priority?: Priority;
  dueDate?: string;
  tags?: string[];
  metadata?: Record<string, unknown>;
  [key: string]: unknown;
}
export interface TaskItem {
  id: number;
  title: string;
  status: TaskStatus;
  assignedUserId: number;
  assignedUserName: string;
  createdAt: string;
  additionalData: AdditionalData;
}
export interface CreateTask {
  title: string;
  assignedUserId: number;
  additionalData: AdditionalData;
}
export interface CreateUser {
  name: string;
  email: string;
}
export interface TaskFilters {
  userId?: number;
  status?: TaskStatus;
  priority?: Priority;
}
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
  errors?: Record<string, string[]>;
}
