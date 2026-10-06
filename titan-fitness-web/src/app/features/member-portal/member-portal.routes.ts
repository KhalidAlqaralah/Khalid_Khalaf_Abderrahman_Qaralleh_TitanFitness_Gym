import { Routes } from '@angular/router';

/** Member self-service (role Member), lazy loaded. */
export const MEMBER_PORTAL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./member-layout').then(m => m.MemberLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'classes' },
      { path: 'classes', title: 'Classes · Titan Fitness', loadComponent: () => import('./member-classes').then(m => m.MemberClassesComponent) },
      { path: 'book/:id', title: 'Class Booking · Titan Fitness', loadComponent: () => import('./member-booking').then(m => m.MemberBookingComponent) }
    ]
  }
];
