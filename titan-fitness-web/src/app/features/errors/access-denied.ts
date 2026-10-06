import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-access-denied',
  imports: [RouterLink],
  template: `
    <div class="error-page">
      <div class="tf-card error-card text-center" data-testid="access-denied">
        <i class="bi bi-shield-lock display-5 text-danger"></i>
        <h1 class="mt-3">Access denied</h1>
        <p class="page-subtitle">You don't have permission to open this screen. Trainers and Plans are for branch managers.</p>
        <a class="btn btn-navy mt-3" [routerLink]="auth.isLoggedIn() ? auth.homeUrl() : '/login'">Go to {{ auth.isLoggedIn() ? 'my home page' : 'sign in' }}</a>
      </div>
    </div>
  `,
  styles: `.error-page { min-height: 80vh; display: flex; align-items: center; justify-content: center; padding: 1rem; } .error-card { padding: 2.5rem; max-width: 480px; }`
})
export class AccessDeniedComponent {
  constructor(public auth: AuthService) {}
}
