import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./tasks/task-page').then((m) => m.TaskPage) },
  { path: '**', redirectTo: '' },
];
