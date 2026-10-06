import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { MatTooltip } from '@angular/material/tooltip';
import { Membership } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';

/** Current Plan card: plan, price, renewal date, status and the Freeze Membership button. */
@Component({
  selector: 'app-plan-card',
  imports: [CurrencyPipe, DatePipe, StatusBadgeComponent, MatTooltip],
  template: `
    <div class="tf-card">
      <div class="tf-card-header"><h3>Current Plan</h3><i class="bi bi-star text-muted-tf"></i></div>
      <div class="tf-card-body">
        @if (membership(); as m) {
          <div class="d-flex justify-content-between align-items-start gap-3">
            <div>
              <div class="plan-name" data-testid="plan-name">{{ m.planName }}</div>
              <div class="text-muted-tf small">
                @switch (m.status) {
                  @case ('Expired') { Ended: {{ m.endDate | date: 'MMM d, y' }} }
                  @case ('Pending') { Starts: {{ m.startDate | date: 'MMM d, y' }} }
                  @default { Renews: {{ m.endDate | date: 'MMM d, y' }} }
                }
              </div>
              <app-status-badge class="d-inline-block mt-2" [status]="m.status" />
            </div>
            <div class="text-end">
              <div class="price">{{ m.price | currency: 'USD' : 'symbol' : '1.0-2' }}</div>
              <div class="text-muted-tf small">/{{ m.durationInMonths === 12 ? 'year' : m.durationInMonths + (m.durationInMonths === 1 ? ' month' : ' months') }}</div>
            </div>
          </div>
          <div class="mt-3 d-grid gap-2">
            <span [matTooltip]="m.canFreeze ? '' : (m.cannotFreezeReason ?? '')">
              <button type="button" class="btn btn-outline-navy w-100" [disabled]="!m.canFreeze" (click)="freeze.emit()" data-testid="profile-freeze">
                <i class="bi bi-snow me-1"></i> Freeze Membership
              </button>
            </span>
            @if (m.status === 'Expired') {
              <button type="button" class="btn btn-navy" (click)="renew.emit()" data-testid="profile-renew">Renew {{ m.planName }}</button>
            }
          </div>
        } @else {
          <div class="text-center text-muted-tf py-2" data-testid="no-plan">
            <i class="bi bi-card-checklist fs-3 d-block mb-2"></i>No current membership. Registering a member grants no access on its own.
          </div>
          <button type="button" class="btn btn-navy w-100 mt-2" (click)="sell.emit()" data-testid="profile-sell">Sell a Plan</button>
        }
      </div>
    </div>
  `,
  styles: `.plan-name { color: var(--tf-navy); font-size: 1.45rem; font-weight: 700; } .price { font-size: 1.4rem; font-weight: 700; }`
})
export class PlanCardComponent {
  readonly membership = input<Membership | null>(null);
  readonly freeze = output<void>();
  readonly sell = output<void>();
  readonly renew = output<void>();
}
