import { formatDate } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { MemberListItem } from '../../core/models/api.models';
import { addDays, roundDownTo5, toDateOnly, toTimeOnly, today } from '../../core/models/dates';
import { applyServerErrors } from '../../core/services/api-errors';
import { BranchContextService } from '../../core/services/branch-context.service';
import { CheckInService } from '../../core/services/check-in.service';
import { ToastService } from '../../core/services/toast.service';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import { MemberPickerComponent } from '../shared/member-picker';

export interface NewCheckInData {
  /** When opened from a member row the member is pre-selected and locked. */
  member?: MemberListItem;
}

/** Date + time not in the future, and the date not older than 7 days. */
const checkInWindow: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const date = group.get('date')?.value as Date | null;
  const time = group.get('time')?.value as Date | null;
  if (!date || !time) return null;
  const at = new Date(date.getFullYear(), date.getMonth(), date.getDate(), time.getHours(), time.getMinutes());
  return at.getTime() > Date.now() ? { future: true } : null;
};

@Component({
  selector: 'app-new-check-in-dialog',
  imports: [ReactiveFormsModule, MatDatepickerModule, MatNativeDateModule, MatTimepickerModule, MatInputModule, MemberPickerComponent, AutofocusDirective],
  templateUrl: './new-check-in-dialog.html'
})
export class NewCheckInDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly checkIns = inject(CheckInService);
  private readonly toast = inject(ToastService);
  private readonly ref = inject(MatDialogRef<NewCheckInDialogComponent>);
  readonly data = inject<NewCheckInData>(MAT_DIALOG_DATA);
  readonly branches = inject(BranchContextService);

  readonly minDate = addDays(today(), -7);
  readonly maxDate = today();
  readonly locked = !!this.data?.member;

  readonly member = signal<MemberListItem | null>(this.data?.member ?? null);
  readonly memberTouched = signal(false);
  readonly memberError = computed(() => {
    const m = this.member();
    if (!m) return this.memberTouched() ? 'Member is required.' : null;
    return m.status === 'Active' ? null : `${m.fullName}'s membership is ${m.status === 'None' ? 'missing' : m.status.toLowerCase()} — they can't check in.`;
  });

  readonly form = this.fb.group(
    {
      date: this.fb.control<Date | null>(today(), Validators.required),
      time: this.fb.control<Date | null>(roundDownTo5(new Date()), Validators.required),
      notes: this.fb.control<string>('', Validators.maxLength(250))
    },
    { validators: checkInWindow }
  );

  readonly saving = signal(false);
  readonly serverError = signal<string | null>(null);

  save(): void {
    this.memberTouched.set(true);
    this.form.markAllAsTouched();
    const member = this.member();
    const branch = this.branches.currentBranch();
    if (!member || this.memberError() || this.form.invalid || !branch) return;

    const { date, time, notes } = this.form.getRawValue();
    this.saving.set(true);
    this.serverError.set(null);

    this.checkIns
      .record({ memberId: member.id, branchId: branch.id, date: toDateOnly(date!), time: toTimeOnly(time!), notes: notes || null })
      .subscribe({
        next: saved => {
          this.toast.success(`${saved.memberName} checked in at ${formatDate(saved.occurredAt, 'hh:mm a', 'en-US')}`);
          this.ref.close(saved);
        },
        error: err => {
          this.saving.set(false);
          const mapped = applyServerErrors(this.form, err);
          if (!mapped && err.status === 409) this.serverError.set(err.error?.detail ?? 'This member cannot check in.');
        }
      });
  }

  close(): void {
    this.ref.close();
  }
}
