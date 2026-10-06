import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { MemberCreated } from '../../../core/models/api.models';
import { applyServerErrors } from '../../../core/services/api-errors';
import { BranchContextService } from '../../../core/services/branch-context.service';
import { MemberService } from '../../../core/services/member.service';
import { ToastService } from '../../../core/services/toast.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { AutofocusDirective } from '../../../shared/directives/autofocus.directive';
import { memberNameError, memberNameValidators } from './member-name.validator';

/** Add Member: just a name and a branch. The system sets the ID, joined date and who added the member. */
@Component({
  selector: 'app-add-member-dialog',
  imports: [ReactiveFormsModule, StatusBadgeComponent, AutofocusDirective],
  template: `
    <div class="dialog-header">
      <h2>Add Member</h2>
      <button type="button" class="btn-icon" aria-label="Close" (click)="ref.close()"><i class="bi bi-x-lg"></i></button>
    </div>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <div class="dialog-body">
        <div class="mb-3">
          <label class="form-label req" for="add-name">Member name</label>
          <input id="add-name" class="form-control" formControlName="fullName" appAutofocus placeholder="e.g., Jane Doe" maxlength="80"
                 [class.is-invalid]="nameError()" data-testid="member-name">
          @if (nameError(); as message) { <div class="invalid-feedback">{{ message }}</div> }
        </div>
        <div class="mb-3">
          <label class="form-label req" for="add-branch">Branch</label>
          <select id="add-branch" class="form-select" formControlName="homeBranchId"
                  [class.is-invalid]="form.controls.homeBranchId.touched && form.controls.homeBranchId.invalid">
            @for (b of branches.branches(); track b.id) { <option [value]="b.id">{{ b.name }} Branch</option> }
          </select>
          @if (form.controls.homeBranchId.touched && form.controls.homeBranchId.invalid) {
            <div class="invalid-feedback">{{ form.controls.homeBranchId.getError('server') ?? 'Branch is required.' }}</div>
          }
        </div>
        <div>
          <span class="form-label d-block">Status</span>
          <app-status-badge status="Active" />
          <div class="form-hint mt-1">The member ID (next #TF-NNNN) and joined date (today) are set automatically. Sell a plan from the profile to give access.</div>
        </div>
      </div>
      <div class="dialog-footer">
        <button type="button" class="btn btn-outline-navy" (click)="ref.close()">Cancel</button>
        <button type="submit" class="btn btn-navy" [disabled]="saving()" data-testid="member-save"><i class="bi bi-person-plus me-1"></i> Add Member</button>
      </div>
    </form>
  `
})
export class AddMemberDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly members = inject(MemberService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly branches = inject(BranchContextService);
  readonly ref = inject<MatDialogRef<AddMemberDialogComponent, MemberCreated>>(MatDialogRef);

  readonly form = this.fb.nonNullable.group({
    fullName: ['', memberNameValidators()],
    homeBranchId: [this.branches.currentBranch()?.id ?? '', Validators.required]
  });

  readonly saving = signal(false);

  nameError(): string | null {
    return memberNameError(this.form.controls.fullName);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const { fullName, homeBranchId } = this.form.getRawValue();
    this.saving.set(true);
    this.members.create(fullName.trim(), homeBranchId).subscribe({
      next: created => {
        this.toast.success(`${fullName.trim().replace(/\s+/g, ' ')} added as #${created.membershipNumber}`);
        this.ref.close(created);
        this.router.navigate(['/members', created.id]);
      },
      error: err => {
        this.saving.set(false);
        applyServerErrors(this.form, err);
      }
    });
  }
}
