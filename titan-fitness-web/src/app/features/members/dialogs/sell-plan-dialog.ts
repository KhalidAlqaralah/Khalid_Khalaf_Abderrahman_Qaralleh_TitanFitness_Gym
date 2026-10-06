import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { Plan } from '../../../core/models/api.models';
import { toDateOnly, today } from '../../../core/models/dates';
import { applyServerErrors } from '../../../core/services/api-errors';
import { MembershipService } from '../../../core/services/membership.service';
import { ToastService } from '../../../core/services/toast.service';
import { LabelPipe } from '../../../shared/pipes/labels.pipe';
import { FreezeAllowancePipe } from '../../../shared/pipes/freeze-allowance.pipe';

export interface SellPlanData {
  memberId: string;
  memberName: string;
}

/** Sells a published plan to a member from a start date (today or later). */
@Component({
  selector: 'app-sell-plan-dialog',
  imports: [ReactiveFormsModule, MatDatepickerModule, MatInputModule, CurrencyPipe, LabelPipe, FreezeAllowancePipe],
  template: `
    <div class="dialog-header">
      <h2>Sell a Plan</h2>
      <button type="button" class="btn-icon" aria-label="Close" (click)="ref.close()"><i class="bi bi-x-lg"></i></button>
    </div>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <div class="dialog-body">
        <p class="text-muted-tf">For <strong>{{ data.memberName }}</strong></p>
        <label class="form-label req" for="sell-plan">Plan</label>
        <select id="sell-plan" class="form-select mb-2" formControlName="planId" [class.is-invalid]="form.controls.planId.touched && form.controls.planId.invalid">
          <option value="" disabled>Select a plan</option>
          @for (p of plans(); track p.id) {
            <option [value]="p.id">{{ p.name }} — {{ p.price | currency: 'USD' }} / {{ p.durationInMonths }} mo</option>
          }
        </select>
        @if (selected(); as p) {
          <div class="small text-muted-tf mb-3">Freezes: {{ p.maxFreezeDays | freezeAllowance: p.maxFreezes }} · Guest passes: {{ p.guestPassQuota }} · {{ p.accessScope | label }}</div>
        }
        @if (form.controls.planId.touched && form.controls.planId.invalid) {
          <div class="invalid-feedback mb-2">{{ form.controls.planId.getError('server') ?? 'Plan is required.' }}</div>
        }
        <label class="form-label req" for="sell-start">Start date</label>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <input matInput id="sell-start" [matDatepicker]="dp" formControlName="startDate" [min]="minDate">
          <mat-datepicker-toggle matIconSuffix [for]="dp" />
          <mat-datepicker #dp />
          @if (form.controls.startDate.hasError('server')) { <mat-error>{{ form.controls.startDate.getError('server') }}</mat-error> }
          @if (form.controls.startDate.hasError('matDatepickerMin')) { <mat-error>The start date cannot be in the past.</mat-error> }
        </mat-form-field>
      </div>
      <div class="dialog-footer">
        <button type="button" class="btn btn-outline-navy" (click)="ref.close()">Cancel</button>
        <button type="submit" class="btn btn-navy" [disabled]="saving()" data-testid="sell-save">Sell Plan</button>
      </div>
    </form>
  `
})
export class SellPlanDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly memberships = inject(MembershipService);
  private readonly toast = inject(ToastService);
  readonly data = inject<SellPlanData>(MAT_DIALOG_DATA);
  readonly ref = inject<MatDialogRef<SellPlanDialogComponent, boolean>>(MatDialogRef);

  readonly minDate = today();
  readonly plans = signal<Plan[]>([]);
  readonly saving = signal(false);

  readonly form = this.fb.group({
    planId: this.fb.nonNullable.control('', Validators.required),
    startDate: this.fb.control<Date | null>(today(), Validators.required)
  });

  selected(): Plan | undefined {
    return this.plans().find(p => p.id === this.form.controls.planId.value);
  }

  ngOnInit(): void {
    this.memberships.sellablePlans().subscribe({ next: plans => this.plans.set(plans) });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const { planId, startDate } = this.form.getRawValue();
    this.saving.set(true);
    this.memberships.purchase(this.data.memberId, planId, toDateOnly(startDate!)).subscribe({
      next: () => {
        this.toast.success(`${this.selected()?.name ?? 'Plan'} sold to ${this.data.memberName}`);
        this.ref.close(true);
      },
      error: err => {
        this.saving.set(false);
        applyServerErrors(this.form, err);
      }
    });
  }
}
