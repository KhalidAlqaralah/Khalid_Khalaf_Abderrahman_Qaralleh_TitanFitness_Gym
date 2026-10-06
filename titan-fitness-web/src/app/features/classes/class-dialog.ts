import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { Observable } from 'rxjs';
import { ClassSession, ClassSessionRequest, TrainerLookup } from '../../core/models/api.models';
import { parseDateOnly, timeOnDate, toDateOnly, toTimeOnly, today } from '../../core/models/dates';
import { applyServerErrors } from '../../core/services/api-errors';
import { BranchContextService } from '../../core/services/branch-context.service';
import { ClassService } from '../../core/services/class.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmService } from '../../shared/components/confirm-dialog/confirm.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';

const REQUIRED_MESSAGES: Record<string, string> = {
  className: 'Class name is required.',
  branchId: 'Branch is required.',
  date: 'Date is required.',
  startTime: 'Start time is required.'
};

export type ClassDialogMode = 'add' | 'view' | 'edit';

export interface ClassDialogData {
  mode: ClassDialogMode;
  session?: ClassSession;
  /** Pre-selected branch and date for Add. */
  branchId?: string | null;
  date?: Date | null;
}

/** Add New Class / Class Details / Edit Class: one dialog, three modes (Figure 6). */
@Component({
  selector: 'app-class-dialog',
  imports: [ReactiveFormsModule, MatDatepickerModule, MatTimepickerModule, MatInputModule, StatusBadgeComponent, AutofocusDirective],
  templateUrl: './class-dialog.html',
  styleUrl: './class-dialog.css'
})
export class ClassDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly classes = inject(ClassService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  readonly branches = inject(BranchContextService);
  readonly data = inject<ClassDialogData>(MAT_DIALOG_DATA);
  readonly ref = inject<MatDialogRef<ClassDialogComponent, boolean>>(MatDialogRef);

  readonly mode = signal<ClassDialogMode>(this.data.mode);
  readonly session = signal<ClassSession | null>(this.data.session ?? null);
  readonly title = computed(() => ({ add: 'Add New Class', view: 'Class Details', edit: 'Edit Class' })[this.mode()]);
  readonly durations = [30, 45, 60];
  readonly minDate = today();
  readonly trainers = signal<TrainerLookup[]>([]);
  readonly saving = signal(false);

  readonly form = this.fb.group(
    {
      className: this.fb.nonNullable.control('', [Validators.required, c => this.nameRule(c)]),
      branchId: this.fb.nonNullable.control(this.data.branchId ?? this.branches.currentBranch()?.id ?? '', Validators.required),
      trainerId: this.fb.control<string | null>(null),
      studioId: this.fb.control<string | null>(null),
      date: this.fb.control<Date | null>(this.data.date ?? null, Validators.required),
      startTime: this.fb.control<Date | null>(null, Validators.required),
      capacityLimit: this.fb.control<number | null>(null, [Validators.min(1), Validators.max(100), Validators.pattern(/^\d+$/)]),
      durationInMinutes: this.fb.nonNullable.control(45, Validators.required),
      description: this.fb.nonNullable.control('', Validators.maxLength(500))
    },
    { validators: g => this.crossRules(g) }
  );

  private readonly branchValue = toSignal(this.form.controls.branchId.valueChanges, { initialValue: this.form.controls.branchId.value });
  private readonly studioValue = toSignal(this.form.controls.studioId.valueChanges, { initialValue: this.form.controls.studioId.value });

  readonly studios = computed(() => this.branches.branches().find(b => b.id === this.branchValue())?.studios ?? []);
  readonly roomCapacity = computed(() => this.studios().find(s => s.id === this.studioValue())?.capacity ?? null);

  ngOnInit(): void {
    const s = this.session();
    if (s) this.fill(s);
    this.applyMode();
    this.loadTrainers(this.form.controls.branchId.value);

    this.form.controls.branchId.valueChanges.subscribe(branchId => {
      this.form.controls.trainerId.setValue(null);
      this.form.controls.studioId.setValue(null);
      this.loadTrainers(branchId);
    });
  }

  private fill(s: ClassSession): void {
    this.form.reset({
      className: s.className,
      branchId: s.branchId,
      trainerId: s.trainerId,
      studioId: s.studioId,
      date: parseDateOnly(s.date),
      startTime: timeOnDate(s.startTime, parseDateOnly(s.date)),
      capacityLimit: s.capacityLimit,
      durationInMinutes: s.durationInMinutes,
      description: s.description ?? ''
    }, { emitEvent: false });
  }

  private applyMode(): void {
    if (this.mode() === 'view') {
      this.form.disable({ emitEvent: false });
      return;
    }
    this.form.enable({ emitEvent: false });
    // The branch is editable only while the class has no bookings.
    if (this.mode() === 'edit' && (this.session()?.activeBookings ?? 0) > 0) {
      this.form.controls.branchId.disable({ emitEvent: false });
    }
  }

  private loadTrainers(branchId: string): void {
    if (!branchId) {
      this.trainers.set([]);
      return;
    }
    this.classes.trainers(branchId).subscribe({
      next: list => {
        // Keep the saved trainer visible in View / Edit even if they were later deactivated.
        const s = this.session();
        const extra = s?.trainerId && !list.some(t => t.id === s.trainerId) && s.branchId === branchId
          ? [{ id: s.trainerId, name: `${s.trainerName} (inactive)`, branchId, isActive: false }]
          : [];
        this.trainers.set([...list, ...extra]);
      }
    });
  }

  switchToEdit(): void {
    this.mode.set('edit');
    this.applyMode();
  }

  cancel(): void {
    if (this.mode() === 'edit' && this.data.mode === 'view') {
      const back = () => {
        this.fill(this.session()!);
        this.mode.set('view');
        this.applyMode();
      };
      if (this.form.dirty) this.confirm.discardChanges().subscribe(ok => ok && back());
      else back();
      return;
    }
    if (this.mode() !== 'view' && this.form.dirty) {
      this.confirm.discardChanges().subscribe(ok => ok && this.ref.close(false));
      return;
    }
    this.ref.close(false);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const v = this.form.getRawValue();
    const request: ClassSessionRequest = {
      className: v.className.trim(),
      branchId: v.branchId,
      trainerId: v.trainerId || null,
      studioId: v.studioId || null,
      date: toDateOnly(v.date!),
      startTime: toTimeOnly(v.startTime!),
      durationInMinutes: v.durationInMinutes,
      capacityLimit: v.capacityLimit,
      description: v.description.trim() || null
    };

    this.saving.set(true);
    const s = this.session();
    const call: Observable<unknown> = this.mode() === 'add' || !s ? this.classes.create(request) : this.classes.update(s.id, request);
    call.subscribe({
      next: () => {
        this.toast.success(this.mode() === 'add' ? `${request.className} scheduled` : 'Class updated');
        this.ref.close(true);
      },
      error: err => {
        this.saving.set(false);
        applyServerErrors(this.form, err);
      }
    });
  }

  error(name: string): string | null {
    const c = this.form.get(name);
    if (!c || !c.touched || c.valid) return null;
    if (c.hasError('server')) return c.getError('server');
    if (c.hasError('required')) return REQUIRED_MESSAGES[name] ?? 'This field is required.';
    if (c.hasError('nameLength')) return 'Class name must be 3–80 characters.';
    if (c.hasError('min') || c.hasError('max') || c.hasError('pattern')) return 'Capacity must be a whole number from 1 to 100.';
    if (c.hasError('matDatepickerMin')) return 'The date must be today or later.';
    if (c.hasError('maxlength')) return 'Description can be up to 500 characters.';
    return 'This value is not valid.';
  }

  private nameRule(c: AbstractControl): ValidationErrors | null {
    const length = ((c.value as string) ?? '').trim().length;
    return length === 0 || (length >= 3 && length <= 80) ? null : { nameLength: true };
  }

  /** Start later than now when the date is today; capacity within the room and not below enrolment. */
  private crossRules(group: AbstractControl): ValidationErrors | null {
    const errors: ValidationErrors = {};
    const date = group.get('date')?.value as Date | null;
    const time = group.get('startTime')?.value as Date | null;
    const capacity = group.get('capacityLimit')?.value as number | null;

    if (date && time && this.mode?.() !== 'view') {
      const start = new Date(date.getFullYear(), date.getMonth(), date.getDate(), time.getHours(), time.getMinutes());
      const unchanged = this.session?.() && toDateOnly(date) === this.session()!.date && toTimeOnly(time) === this.session()!.startTime;
      if (!unchanged && start.getTime() <= Date.now()) errors['startInPast'] = true;
    }
    const room = this.roomCapacity?.();
    if (room && (capacity ?? 20) > room) errors['aboveRoom'] = room;
    const enrolled = this.session?.()?.enrolled ?? 0;
    if (this.mode?.() === 'edit' && capacity !== null && capacity < enrolled) errors['belowEnrolment'] = enrolled;
    return Object.keys(errors).length ? errors : null;
  }
}
