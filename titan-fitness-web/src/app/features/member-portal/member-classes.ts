import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClassSession } from '../../core/models/api.models';
import { parseDateOnly, timeOnDate, toDateOnly, today } from '../../core/models/dates';
import { SelfServiceService } from '../../core/services/self-service.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';

/** The member's class list for the coming week, grouped by day. */
@Component({
  selector: 'app-member-classes',
  imports: [RouterLink, DatePipe, StatusBadgeComponent],
  template: `
    <h1 class="mb-1">Book a Class</h1>
    <p class="page-subtitle mb-4">Classes at every branch for the next 7 days.</p>
    @if (loading()) {
      <div class="state-box"><span class="spinner-border spinner-border-sm me-2"></span>Loading classes…</div>
    } @else {
      @for (g of groups(); track g.date) {
        <h2 class="h6 text-muted-tf text-uppercase mt-4 mb-2">{{ g.label }}</h2>
        <div class="tf-card">
          @for (s of g.sessions; track s.id) {
            <div class="d-flex align-items-center gap-3 p-3 border-bottom" data-testid="member-class">
              <div class="text-center" style="width:64px"><strong>{{ start(s) | date: 'hh:mm' }}</strong><div class="small text-muted-tf">{{ start(s) | date: 'a' }}</div></div>
              <div class="flex-grow-1">
                <div class="fw-bold">{{ s.className }}</div>
                <div class="small text-muted-tf">{{ s.trainerName ?? 'Trainer TBA' }} • {{ s.studioName ?? 'Room TBA' }} • {{ s.branchName }}</div>
              </div>
              <span class="small text-muted-tf text-nowrap">{{ s.remainingPlaces }} left</span>
              <app-status-badge [status]="s.state" />
              <a class="btn btn-sm" [class.btn-navy]="s.state === 'Upcoming' || s.state === 'Full'"
                 [class.btn-outline-navy]="s.state !== 'Upcoming' && s.state !== 'Full'"
                 [class.disabled]="s.state !== 'Upcoming' && s.state !== 'Full'" [routerLink]="['/member/book', s.id]">Book</a>
            </div>
          }
        </div>
      } @empty {
        <div class="tf-card state-box"><i class="bi bi-calendar-x"></i>No classes in the next 7 days.</div>
      }
    }
  `
})
export class MemberClassesComponent implements OnInit {
  private readonly self = inject(SelfServiceService);
  readonly sessions = signal<ClassSession[]>([]);
  readonly loading = signal(true);

  readonly groups = computed(() => {
    const map = new Map<string, ClassSession[]>();
    for (const s of this.sessions()) map.set(s.date, [...(map.get(s.date) ?? []), s]);
    return [...map.entries()].map(([date, sessions]) => ({
      date,
      label: parseDateOnly(date).toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' }),
      sessions
    }));
  });

  ngOnInit(): void {
    this.self.classes(toDateOnly(today()), 7).subscribe({
      next: list => {
        this.sessions.set(list.filter(s => s.state !== 'Completed'));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  start(s: ClassSession): Date {
    return timeOnDate(s.startTime, parseDateOnly(s.date));
  }
}
