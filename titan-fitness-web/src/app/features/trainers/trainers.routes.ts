import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

/** Trainers feature (branch managers only), lazy loaded. One details component serves View, Add and Update. */
export const TRAINERS_ROUTES: Routes = [
  {
    path: '',
    title: 'Trainers · Titan Fitness',
    loadComponent: () => import('./trainer-directory').then(m => m.TrainerDirectoryComponent)
  },
  {
    path: 'new',
    title: 'New Trainer · Titan Fitness',
    data: { mode: 'add' },
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./trainer-details').then(m => m.TrainerDetailsComponent)
  },
  {
    path: ':id',
    title: 'Trainer Details · Titan Fitness',
    data: { mode: 'view' },
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./trainer-details').then(m => m.TrainerDetailsComponent)
  },
  {
    path: ':id/edit',
    title: 'Update Trainer · Titan Fitness',
    data: { mode: 'edit' },
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./trainer-details').then(m => m.TrainerDetailsComponent)
  }
];
