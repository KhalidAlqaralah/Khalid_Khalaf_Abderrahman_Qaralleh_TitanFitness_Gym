import { Routes } from '@angular/router';

export const CLASSES_ROUTES: Routes = [
  {
    path: '',
    title: 'Class Schedule · Titan Fitness',
    loadComponent: () => import('./class-schedule').then(m => m.ClassScheduleComponent)
  }
];
