import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { Member } from '../../../core/models/api.models';
import { initials } from '../../../core/models/dates';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';

/** Identity panel: photo, status chip, membership number, contact details and joined date. */
@Component({
  selector: 'app-identity-card',
  imports: [StatusBadgeComponent, DatePipe],
  template: `
    <div class="tf-card p-4 position-relative text-center">
      <app-status-badge class="status" [status]="member().status" />
      @if (member().photoUrl) {
        <img class="avatar avatar-lg" [src]="member().photoUrl" [alt]="member().fullName">
      } @else {
        <span class="avatar avatar-lg">{{ initialsOf(member().fullName) }}</span>
      }
      <h2 class="h3 fw-bold mt-3 mb-1" data-testid="profile-name">{{ member().fullName }}</h2>
      <div class="text-muted-tf" data-testid="profile-number">ID: #{{ member().membershipNumber }}</div>
      <ul class="list-unstyled text-start mt-4 mb-0 d-grid gap-2">
        <li><i class="bi bi-envelope me-2 text-muted-tf"></i>{{ member().email ?? 'No email on file' }}</li>
        <li><i class="bi bi-telephone me-2 text-muted-tf"></i>{{ member().phone ?? 'No phone on file' }}</li>
        <li><i class="bi bi-geo-alt me-2 text-muted-tf"></i>{{ member().address ?? 'No address on file' }}</li>
        <li><i class="bi bi-building me-2 text-muted-tf"></i>Home branch: {{ member().homeBranchName }}</li>
        <li><i class="bi bi-calendar me-2 text-muted-tf"></i>Joined: {{ member().joinedOn | date: 'MMM d, y' }}</li>
      </ul>
    </div>
  `,
  styles: `.status { position: absolute; top: 1rem; right: 1rem; }`
})
export class IdentityCardComponent {
  readonly member = input.required<Member>();

  initialsOf(name: string): string {
    return initials(name);
  }
}
