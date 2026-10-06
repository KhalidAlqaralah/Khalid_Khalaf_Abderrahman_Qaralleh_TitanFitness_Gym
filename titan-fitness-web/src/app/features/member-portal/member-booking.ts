import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { BookingResult, ClassSession, Eligibility } from '../../core/models/api.models';
import { parseDateOnly, sameDay, timeOnDate, today } from '../../core/models/dates';
import { messageOf } from '../../core/services/api-errors';
import { SelfServiceService } from '../../core/services/self-service.service';

/** Book Session — Member View (Figure 8): the member books themselves; eligibility comes from their membership. */
@Component({
  selector: 'app-member-booking',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './member-booking.html',
  styleUrl: './member-booking.css'
})
export class MemberBookingComponent implements OnInit {
  private readonly self = inject(SelfServiceService);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  readonly session = signal<ClassSession | null>(null);
  readonly eligibility = signal<Eligibility | null>(null);
  readonly notFound = signal(false);
  readonly saving = signal(false);
  readonly result = signal<BookingResult | null>(null);
  readonly error = signal<string | null>(null);
  note = '';

  readonly start = computed(() => { const s = this.session(); return s ? timeOnDate(s.startTime, parseDateOnly(s.date)) : null; });
  readonly end = computed(() => { const s = this.session(); return s ? timeOnDate(s.endTime, parseDateOnly(s.date)) : null; });
  readonly isToday = computed(() => { const s = this.session(); return !!s && sameDay(parseDateOnly(s.date), today()); });
  readonly fill = computed(() => { const s = this.session(); return s ? Math.min(100, (s.enrolled / s.capacityLimit) * 100) : 0; });
  readonly bookable = computed(() => {
    const s = this.session();
    const e = this.eligibility();
    return !!s && !!e && e.eligible && !e.existingBooking && (s.state === 'Upcoming' || s.state === 'Full') && !this.result();
  });

  ngOnInit(): void {
    this.self.classById(this.id()).subscribe({
      next: s => this.session.set(s),
      error: (err: HttpErrorResponse) => this.notFound.set(err.status === 404)
    });
    this.self.eligibility(this.id()).subscribe({ next: e => this.eligibility.set(e) });
  }

  confirm(): void {
    if (!this.bookable()) return;
    this.saving.set(true);
    this.error.set(null);
    this.self.book(this.id(), this.note.trim() || null).subscribe({
      next: r => {
        this.saving.set(false);
        this.result.set(r);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(messageOf(err));
      }
    });
  }

  back(): void {
    this.router.navigate(['/member/classes']);
  }
}
