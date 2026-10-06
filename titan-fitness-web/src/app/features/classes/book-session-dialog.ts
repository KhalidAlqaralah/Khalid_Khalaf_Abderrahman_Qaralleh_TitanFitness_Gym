import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ClassSession, MemberListItem } from '../../core/models/api.models';
import { parseDateOnly, sameDay, timeOnDate, today } from '../../core/models/dates';
import { messageOf } from '../../core/services/api-errors';
import { ClassService } from '../../core/services/class.service';
import { ToastService } from '../../core/services/toast.service';
import { MemberPickerComponent } from '../shared/member-picker';

export interface BookSessionData {
  session: ClassSession;
  member?: MemberListItem | null;
}

/** Book Session (Figure 7): pick a member, add a note, confirm. Full classes put the member on the waitlist. */
@Component({
  selector: 'app-book-session-dialog',
  imports: [FormsModule, DatePipe, MemberPickerComponent],
  template: `
    <div class="dialog-header align-items-start">
      <div>
        <h2 class="book-title">{{ s.className }}</h2>
        <div class="d-flex flex-wrap gap-4 mt-2 text-muted-tf">
          <span><i class="bi bi-clock me-1"></i>{{ dayLabel() }}, {{ start() | date: 'HH:mm' }} - {{ end() | date: 'HH:mm' }}</span>
          <span><i class="bi bi-person me-1"></i>Trainer: {{ s.trainerName ?? 'TBA' }}</span>
          <span><i class="bi bi-geo-alt me-1"></i>{{ s.studioName ?? 'No room' }}</span>
        </div>
      </div>
      <span class="tf-badge grey spots" data-testid="spots"><i class="bi bi-people"></i>{{ s.remainingPlaces }} / {{ s.capacityLimit }} spots remaining</span>
    </div>
    <div class="dialog-body">
      @if (s.remainingPlaces === 0) {
        <div class="alert alert-warning py-2 small">This class is full. Confirming adds the member to the waitlist ({{ s.waitlist }} waiting).</div>
      }
      <label class="form-label" for="book-member">Select Member</label>
      <app-member-picker [(member)]="member" inputId="book-member" [invalid]="!!memberError()" />
      @if (memberError(); as e) { <div class="invalid-feedback" data-testid="book-member-error">{{ e }}</div> }

      <label class="form-label mt-4" for="book-notes">Special Requirements / Notes (Optional)</label>
      <textarea id="book-notes" class="form-control" rows="3" maxlength="500" [(ngModel)]="note" placeholder="e.g., Recovering from mild shoulder injury"></textarea>
      <div class="char-count">{{ note.length }} / 500</div>

      @if (serverError(); as e) { <div class="alert alert-danger py-2 small mt-2 mb-0" role="alert" data-testid="book-error">{{ e }}</div> }
    </div>
    <div class="dialog-footer">
      <button type="button" class="btn btn-outline-navy" (click)="ref.close()">Cancel</button>
      <button type="button" class="btn btn-navy" [disabled]="saving()" (click)="confirm()" data-testid="confirm-booking">Confirm Booking</button>
    </div>
  `,
  styles: `.book-title { color: var(--tf-navy); font-size: 1.9rem !important; } .spots { border-radius: 4px; }`
})
export class BookSessionDialogComponent {
  private readonly classes = inject(ClassService);
  private readonly toast = inject(ToastService);
  readonly data = inject<BookSessionData>(MAT_DIALOG_DATA);
  readonly ref = inject<MatDialogRef<BookSessionDialogComponent, boolean>>(MatDialogRef);

  readonly s = this.data.session;
  readonly member = signal<MemberListItem | null>(this.data.member ?? null);
  readonly touched = signal(false);
  readonly saving = signal(false);
  readonly serverError = signal<string | null>(null);
  note = '';

  readonly start = computed(() => timeOnDate(this.s.startTime, parseDateOnly(this.s.date)));
  readonly end = computed(() => timeOnDate(this.s.endTime, parseDateOnly(this.s.date)));
  readonly dayLabel = computed(() => (sameDay(parseDateOnly(this.s.date), today()) ? 'Today' : parseDateOnly(this.s.date).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })));

  readonly memberError = computed(() => {
    const m = this.member();
    if (!m) return this.touched() ? 'Select a member.' : null;
    return m.status === 'Active' ? null : `${m.fullName} can't book: membership is ${m.status === 'None' ? 'missing' : m.status.toLowerCase()}.`;
  });

  confirm(): void {
    this.touched.set(true);
    const m = this.member();
    if (!m || this.memberError()) return;

    this.saving.set(true);
    this.serverError.set(null);
    this.classes.book(this.s.id, m.id, this.note.trim() || null).subscribe({
      next: result => {
        this.toast.success(
          result.status === 'Waitlisted'
            ? `${m.fullName} added to the waitlist for ${this.s.className} (#${result.waitlistPosition})`
            : `${m.fullName} booked on ${this.s.className}`
        );
        this.ref.close(true);
      },
      error: err => {
        this.saving.set(false);
        this.serverError.set(messageOf(err));
      }
    });
  }
}

