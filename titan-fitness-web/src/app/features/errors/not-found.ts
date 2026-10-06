import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** The router's not-found fallback (path '**'). */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <div class="error-page">
      <div class="tf-card error-card text-center" data-testid="not-found-page">
        <div class="display-4 fw-bold text-muted-tf">404</div>
        <h1 class="mt-2">Page not found</h1>
        <p class="page-subtitle">The address does not match any screen of the staff portal.</p>
        <a class="btn btn-navy mt-3" routerLink="/dashboard">Back to the dashboard</a>
      </div>
    </div>
  `,
  styles: `.error-page { min-height: 80vh; display: flex; align-items: center; justify-content: center; padding: 1rem; } .error-card { padding: 2.5rem; max-width: 480px; }`
})
export class NotFoundComponent {}
