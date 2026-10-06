import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { hasFieldErrors, messageOf } from '../services/api-errors';
import { PAGE_LOAD, RETRY, SILENT } from '../services/http-options';
import { ToastService } from '../services/toast.service';

/**
 * Handles HTTP errors for the whole app in one place, then re-throws so each screen can show
 * its own inline message (field errors, "not found" state...).
 *
 *  0        → "Can't reach the server" toast, stay on the page
 *  400      → field errors go to the form; toast only when none is field-specific
 *  401      → clear the session, go to sign-in and come back to the same URL afterwards
 *  403      → "You don't have permission" toast; page loads show the Access denied page
 *  404      → page loads show the screen's not-found state; otherwise "The item no longer exists."
 *  409      → the server message (duplicate / changed by someone else)
 *  422      → field errors go to the form; toast the message when it is not about one field
 *  5xx      → "Something went wrong" toast, with Retry for lists
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) return throwError(() => error);

      const isPageLoad = req.context.get(PAGE_LOAD);
      const retry = req.context.get(RETRY);
      const silent = req.context.get(SILENT);
      const isLogin = req.url.endsWith('/auth/login');

      switch (error.status) {
        case 0:
          toast.error("Can't reach the server. Check your connection.", retry ? { label: 'Retry', run: retry } : undefined);
          break;
        case 400:
          if (!hasFieldErrors(error) && !silent) toast.error(messageOf(error, 'The request is not valid.'));
          break;
        case 401:
          // Several requests can fail at once; only the first one signs out and redirects.
          if (!isLogin && auth.isLoggedIn()) {
            auth.logout();
            // During a page load the router has not reached the new URL yet, so use the URL being opened.
            const navigation = router.currentNavigation();
            const target = navigation ? router.serializeUrl(navigation.finalUrl ?? navigation.extractedUrl) : router.url;
            const returnUrl = target.startsWith('/login') ? undefined : target;
            router.navigate(['/login'], { queryParams: { returnUrl, expired: 1 } });
            toast.show('Your session has ended. Please sign in again.', 'warning');
          }
          break;
        case 403:
          toast.error("You don't have permission to do this.");
          if (isPageLoad) router.navigate(['/access-denied'], { skipLocationChange: true });
          break;
        case 404:
          if (!isPageLoad && !silent) toast.error('The item no longer exists.');
          break;
        case 409:
          if (!silent) toast.error(messageOf(error, 'This change conflicts with existing data.'));
          break;
        case 422:
          if (!hasFieldErrors(error) && !silent) toast.error(messageOf(error, 'Validation failed.'));
          break;
        default:
          if (error.status >= 500) {
            toast.error('Something went wrong. Please try again.', retry ? { label: 'Retry', run: retry } : undefined);
          }
      }

      return throwError(() => error);
    })
  );
};
