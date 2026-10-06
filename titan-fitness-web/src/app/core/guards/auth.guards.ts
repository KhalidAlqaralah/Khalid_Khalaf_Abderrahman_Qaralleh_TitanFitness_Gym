import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Role } from '../models/api.models';
import { AuthService } from '../services/auth.service';

/** Only signed-in users; otherwise sign in and come back to the same URL. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Only the roles listed in the route's data.roles; others see the Access denied page. */
export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

  const roles = (route.data['roles'] as Role[] | undefined) ?? [];
  if (roles.length === 0 || roles.includes(auth.role()!)) return true;

  return auth.role() === 'Member' ? router.createUrlTree(['/member/classes']) : router.createUrlTree(['/access-denied']);
};

/** The sign-in page is for signed-out users. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isLoggedIn() ? inject(Router).createUrlTree([auth.homeUrl()]) : true;
};
