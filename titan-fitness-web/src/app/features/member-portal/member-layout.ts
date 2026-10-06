import { TitleCasePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { Eligibility } from '../../core/models/api.models';
import { AuthService } from '../../core/services/auth.service';
import { SelfServiceService } from '../../core/services/self-service.service';

@Component({
  selector: 'app-member-layout',
  imports: [RouterOutlet, TitleCasePipe],
  template: `
    <header class="member-top">
      <div class="d-flex align-items-center gap-2">
        <span class="mark"><i class="bi bi-arrows-angle-expand"></i></span>
        <strong>Titan Fitness</strong>
        <span class="text-muted-tf ms-2">Hi, {{ auth.session()?.displayName | titlecase }}</span>
      </div>
      <div class="d-flex align-items-center gap-3">
        @if (eligibility(); as e) {
          <span class="tf-badge" [class.ok]="e.eligible" [class.red]="!e.eligible" data-testid="member-status"><span class="dot"></span>{{ e.eligible ? 'Active Member' : e.status }}</span>
        }
        <button type="button" class="btn btn-outline-navy btn-sm" (click)="signOut()" data-testid="sign-out">Sign out</button>
      </div>
    </header>
    <main class="member-main"><router-outlet /></main>
  `,
  styles: `
    :host { display: block; min-height: 100vh; background: var(--tf-bg); }
    .member-top { display: flex; justify-content: space-between; align-items: center; padding: 1rem 2rem; background: #fff; border-bottom: 1px solid var(--tf-line); }
    .mark { width: 32px; height: 32px; border-radius: 8px; background: var(--tf-navy); color: #fff; display: inline-flex; align-items: center; justify-content: center; }
    .member-main { max-width: 760px; margin: 0 auto; padding: 1.5rem 1rem 3rem; }
  `
})
export class MemberLayoutComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly self = inject(SelfServiceService);
  private readonly router = inject(Router);

  readonly eligibility = signal<Eligibility | null>(null);

  ngOnInit(): void {
    this.self.eligibility().subscribe({ next: e => this.eligibility.set(e) });
  }

  signOut(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
