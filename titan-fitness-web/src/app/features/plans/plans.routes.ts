import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

/** Plans feature (branch managers only), lazy loaded. One PlanDetailsComponent serves View, Add and Update. */
export const PLANS_ROUTES: Routes = [
  {
    path: '',
    title: 'Plans · Titan Fitness',
    loadComponent: () => import('./plan-catalogue').then(m => m.PlanCatalogueComponent)
  },
  {
    path: 'new',
    title: 'New Plan · Titan Fitness',
    data: { mode: 'add' },
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./plan-details').then(m => m.PlanDetailsComponent)
  },
  {
    path: ':id',
    title: 'Plan Details · Titan Fitness',
    data: { mode: 'view' },
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./plan-details').then(m => m.PlanDetailsComponent)
  },
  {
    path: ':id/edit',
    title: 'Update Plan · Titan Fitness',
    data: { mode: 'edit' },
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./plan-details').then(m => m.PlanDetailsComponent)
  }
];
