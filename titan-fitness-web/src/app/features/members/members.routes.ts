import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

/** Members feature, lazy loaded: directory, profile sub-screen and freeze sub-screen. */
export const MEMBERS_ROUTES: Routes = [
  {
    path: '',
    title: 'Members · Titan Fitness',
    loadComponent: () => import('./directory/member-directory').then(m => m.MemberDirectoryComponent)
  },
  {
    path: ':id',
    title: 'Member Profile · Titan Fitness',
    loadComponent: () => import('./profile/member-profile').then(m => m.MemberProfileComponent)
  },
  {
    path: ':id/freeze',
    title: 'Freeze Membership · Titan Fitness',
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./freeze/freeze-membership').then(m => m.FreezeMembershipComponent)
  }
];
