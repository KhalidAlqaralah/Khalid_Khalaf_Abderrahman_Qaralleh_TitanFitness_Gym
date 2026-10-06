import { Component, computed, input } from '@angular/core';

/** One coloured status chip for members, memberships, trainers, plans and classes. */
@Component({
  selector: 'app-status-badge',
  template: `
    <span class="tf-badge" [class]="tone()" [attr.data-status]="status()">
      @if (showDot()) { <span class="dot"></span> }
      @switch (status()) {
        @case ('InProgress') { In Progress }
        @case ('None') { No plan }
        @case ('Upcoming') { Upcoming }
        @default { {{ text() }} }
      }
    </span>
  `
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();
  /** Optional wording override, e.g. "Active (Booking Open)". */
  readonly label = input<string | null>(null);

  readonly text = computed(() => this.label() ?? this.status());

  readonly tone = computed(() => {
    switch (this.status()) {
      case 'Active':
      case 'Published':
      case 'Confirmed':
      case 'Attended':
        return 'tf-badge ok';
      case 'Frozen':
      case 'Inactive':
      case 'Pending':
      case 'None':
      case 'Waitlisted':
        return 'tf-badge grey';
      case 'Expired':
      case 'Retired':
      case 'NoShow':
        return 'tf-badge red';
      case 'Upcoming':
        return 'tf-badge outline';
      case 'InProgress':
        return 'tf-badge navy-outline';
      case 'Full':
        return 'tf-badge red-solid';
      case 'Completed':
        return 'tf-badge filled';
      case 'Cancelled':
        return 'tf-badge red-outline';
      default:
        return 'tf-badge grey';
    }
  });

  readonly showDot = computed(() => !['Upcoming', 'InProgress', 'Full', 'Completed', 'Cancelled'].includes(this.status()));
}
