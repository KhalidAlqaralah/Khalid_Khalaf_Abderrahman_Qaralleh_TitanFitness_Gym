import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatTooltip } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { FreezeReason, Member, Membership } from '../../../core/models/api.models';
import { addDays, addMonths, freezeDays, initials, parseDateOnly, toDateOnly, today } from '../../../core/models/dates';
import { applyServerErrors } from '../../../core/services/api-errors';
import { MemberService } from '../../../core/services/member.service';
import { MembershipService } from '../../../core/services/membership.service';
import { ToastService } from '../../../core/services/toast.service';
import { HasUnsavedChanges } from '../../../core/guards/unsaved-changes.guard';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';

/**
 * Freeze Membership (/members/:id/freeze). Pauses the current membership for 1, 2 or 3 months;
 * the Projected Impact panel recalculates the new end date with computed() as the form changes.
 */
@Component({
  selector: 'app-freeze-membership',
  imports: [ReactiveFormsModule, MatDatepickerModule, MatInputModule, MatTooltip, RouterLink, DatePipe, StatusBadgeComponent],
  templateUrl: './freeze-membership.html',
  styleUrl: './freeze-membership.css'
})
export class FreezeMembershipComponent implements HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly members = inject(MemberService);
  private readonly memberships = inject(MembershipService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Route parameter :id (the member). */
  readonly id = input.required<string>();

  readonly member = signal<Member | null>(null);
  readonly membership = signal<Membership | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly saving = signal(false);
  private saved = false;

  readonly reasons: { value: FreezeReason; label: string }[] = [
    { value: 'ExtendedTravel', label: 'Extended Travel' },
    { value: 'Medical', label: 'Medical' },
    { value: 'Injury', label: 'Injury' },
    { value: 'Financial', label: 'Financial' },
    { value: 'Other', label: 'Other' }
  ];
  readonly durations = [1, 2, 3];
  readonly minDate = today();

  readonly form = this.fb.group({
    startDate: this.fb.control<Date | null>(addDays(today(), 1), [Validators.required, c => this.startDateRule(c)]),
    durationInMonths: this.fb.control<number | null>(1, Validators.required),
    reason: this.fb.control<FreezeReason | null>(null, Validators.required),
    notes: this.fb.control<string>('', Validators.maxLength(500))
  });

  private readonly startValue = toSignal(this.form.controls.startDate.valueChanges, { initialValue: this.form.controls.startDate.value });
  private readonly durationValue = toSignal(this.form.controls.durationInMonths.valueChanges, { initialValue: 1 });
  private readonly formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

  readonly originalEnd = computed(() => {
    const m = this.membership();
    return m ? parseDateOnly(m.endDate) : null;
  });

  /** Start date, only when it passes validation (otherwise the panel shows "—"). */
  private readonly validStart = computed(() => {
    const start = this.startValue();
    this.formStatus();
    return start && this.form.controls.startDate.valid ? start : null;
  });

  /** Allowance days each option would use: 30 per month (fewer for a shorter calendar month), as the backend counts. */
  readonly optionDays = computed(() => {
    const start = this.validStart() ?? addDays(today(), 1);
    return this.durations.map(months => Math.min(freezeDays(start, months), months * 30));
  });

  readonly freezeDaysUsed = computed(() => {
    const start = this.validStart();
    const months = this.durationValue();
    return start && months ? freezeDays(start, months) : null;
  });

  /** New End Date = Original End Date + the frozen days (how the backend extends the membership). */
  readonly newEndDate = computed(() => {
    const end = this.originalEnd();
    const days = this.freezeDaysUsed();
    return end && days !== null ? addDays(end, days) : null;
  });

  readonly freezeEnd = computed(() => {
    const start = this.validStart();
    const months = this.durationValue();
    return start && months ? addDays(addMonths(start, months), -1) : null;
  });

  readonly canFreeze = computed(() => this.membership()?.canFreeze === true);

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => this.load(id));
    });

    // The overlap check depends on the duration too.
    this.form.controls.durationInMonths.valueChanges.pipe(takeUntilDestroyed()).subscribe(() =>
      this.form.controls.startDate.updateValueAndValidity({ emitEvent: false })
    );

    // An option longer than the remaining freeze days is disabled; move off it if needed.
    effect(() => {
      const m = this.membership();
      const days = this.optionDays();
      untracked(() => {
        if (!m) return;
        const current = this.form.controls.durationInMonths.value;
        if (current && days[current - 1] > m.remainingFreezeDays) {
          const fit = this.durations.filter((_, i) => days[i] <= m.remainingFreezeDays).pop() ?? null;
          this.form.controls.durationInMonths.setValue(fit);
        }
      });
    });
  }

  load(id: string): void {
    this.loading.set(true);
    this.members.getById(id).subscribe({
      next: member => {
        this.member.set(member);
        this.members.currentMembership(id).subscribe({
          next: m => {
            this.membership.set(m);
            this.loading.set(false);
            this.form.controls.startDate.updateValueAndValidity();
            if (!m.canFreeze) this.form.disable();
          },
          error: () => {
            this.membership.set(null);
            this.loading.set(false);
            this.form.disable();
          }
        });
      },
      error: (err: HttpErrorResponse) => {
        this.notFound.set(err.status === 404);
        this.loading.set(false);
      }
    });
  }

  optionDisabled(index: number): boolean {
    const m = this.membership();
    return !m || !this.canFreeze() || this.optionDays()[index] > m.remainingFreezeDays;
  }

  optionTooltip(index: number): string {
    const m = this.membership();
    if (!m || !this.optionDisabled(index) || !this.canFreeze()) return '';
    return `${this.durations[index]} month${index ? 's' : ''} uses ${this.optionDays()[index]} freeze days; only ${m.remainingFreezeDays} remain.`;
  }

  chooseDuration(months: number, index: number): void {
    if (this.optionDisabled(index)) return;
    this.form.controls.durationInMonths.setValue(months);
    this.form.controls.durationInMonths.markAsDirty();
  }

  initialsOf(name: string): string {
    return initials(name);
  }

  hasUnsavedChanges(): boolean {
    return !this.saved && this.form.dirty;
  }

  confirm(): void {
    this.form.markAllAsTouched();
    const m = this.membership();
    if (!m || this.form.invalid) return;

    const { startDate, durationInMonths, reason, notes } = this.form.getRawValue();
    this.saving.set(true);
    this.memberships
      .freeze(m.id, { startDate: toDateOnly(startDate!), durationInMonths, reason, notes: notes || null })
      .subscribe({
        next: done => {
          this.saved = true;
          this.toast.success(`Membership frozen from ${this.format(done.startDate)} to ${this.format(done.endDate)}. New end date: ${this.format(done.newEndDate)}.`);
          this.router.navigate(['/members', this.id()]);
        },
        error: err => {
          this.saving.set(false);
          applyServerErrors(this.form, err);
        }
      });
  }

  cancel(): void {
    this.router.navigate(['/members', this.id()]);
  }

  private format(date: string): string {
    return parseDateOnly(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  /** Today or later, before the current end date, and not overlapping a scheduled freeze. */
  private startDateRule(control: AbstractControl): ValidationErrors | null {
    const start = control.value as Date | null;
    const m = this.membership?.();
    if (!start) return null;
    if (start < today()) return { past: true };
    if (!m) return null;
    if (start >= parseDateOnly(m.endDate)) return { afterEnd: true };
    const months = this.form?.controls.durationInMonths.value ?? 1;
    const end = addDays(addMonths(start, months), -1);
    const clash = m.freezes.some(f => parseDateOnly(f.startDate) <= end && start <= parseDateOnly(f.endDate));
    return clash ? { overlap: true } : null;
  }
}
