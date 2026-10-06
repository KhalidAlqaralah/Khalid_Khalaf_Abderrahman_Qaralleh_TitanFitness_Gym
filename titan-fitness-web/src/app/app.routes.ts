import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards/auth.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Sign in · Titan Fitness',
    loadComponent: () => import('./features/auth/login').then(m => m.LoginComponent)
  },
  {
    path: 'access-denied',
    title: 'Access denied · Titan Fitness',
    loadComponent: () => import('./features/errors/access-denied').then(m => m.AccessDeniedComponent)
  },
  {
    path: 'member',
    canActivate: [roleGuard],
    data: { roles: ['Member'] },
    loadChildren: () => import('./features/member-portal/member-portal.routes').then(m => m.MEMBER_PORTAL_ROUTES)
  },
  {
    path: '',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['FrontDesk', 'Manager'] },
    loadComponent: () => import('./layout/shell').then(m => m.ShellComponent),
    children: [
      {
        path: 'dashboard',
        title: 'Dashboard · Titan Fitness',
        loadComponent: () => import('./features/dashboard/dashboard').then(m => m.DashboardComponent)
      },
      {
        path: 'members',
        loadChildren: () => import('./features/members/members.routes').then(m => m.MEMBERS_ROUTES)
      },
      {
        path: 'classes',
        loadChildren: () => import('./features/classes/classes.routes').then(m => m.CLASSES_ROUTES)
      },
      {
        path: 'trainers',
        canActivate: [roleGuard],
        data: { roles: ['Manager'] },
        loadChildren: () => import('./features/trainers/trainers.routes').then(m => m.TRAINERS_ROUTES)
      },
      {
        path: 'plans',
        canActivate: [roleGuard],
        data: { roles: ['Manager'] },
        loadChildren: () => import('./features/plans/plans.routes').then(m => m.PLANS_ROUTES)
      }
    ]
  },
  {
    path: '**',
    title: 'Page not found · Titan Fitness',
    loadComponent: () => import('./features/errors/not-found').then(m => m.NotFoundComponent)
  }
];
