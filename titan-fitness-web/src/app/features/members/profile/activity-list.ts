import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { MemberActivity } from '../../../core/models/api.models';
import { RelativeDayPipe } from '../../../shared/pipes/relative-day.pipe';

/** Recent Activity: the last seven check-ins and attended classes, newest first. */
@Component({
  selector: 'app-activity-list',
  imports: [DatePipe],
  template: `
    <div class="tf-card h-100">
      <div class="tf-card-header"><h3>Recent Activity</h3><span class="small text-muted-tf">Last {{ items().length }}</span></div>
      @if (loading()) {
        <div class="state-box"><span class="spinner-border spinner-border-sm me-2"></span>Loading activity…</div>
      } @else {
        <ul class="list-unstyled m-0">
          @for (a of items(); track $index) {
            <li class="activity" data-testid="activity-row">
              <span class="icon" [class.class]="a.kind === 'ClassAttendance'">
                <i class="bi" [class]="a.kind === 'CheckIn' ? 'bi bi-box-arrow-in-right' : 'bi bi-arrows-angle-expand'"></i>
              </span>
              <div class="flex-grow-1">
                <div>{{ a.title }}</div>
                <div class="small text-muted-tf">{{ a.detail }}</div>
              </div>
              <div class="text-end small">
                <div>{{ dayOf(a.occurredAt) }}</div>
                <div class="text-muted-tf">{{ a.occurredAt | date: 'hh:mm a' }}</div>
              </div>
            </li>
          } @empty {
            <li class="state-box"><i class="bi bi-clock-history"></i>No activity yet.</li>
          }
        </ul>
      }
    </div>
  `,
  styles: `
    .activity { display: flex; gap: 1rem; align-items: center; padding: .9rem 1.25rem; border-bottom: 1px solid var(--tf-line); }
    .icon { width: 40px; height: 40px; border-radius: 4px; background: var(--tf-grey-bg); display: inline-flex; align-items: center; justify-content: center; }
    .icon.class { background: var(--tf-navy-100); color: var(--tf-navy); }
  `
})
export class ActivityListComponent {
  readonly items = input.required<MemberActivity[]>();
  readonly loading = input(false);

  private readonly relative = new RelativeDayPipe();

  dayOf(value: string): string {
    const text = this.relative.transform(value);
    return text.startsWith('Today') ? 'Today' : text;
  }
}
