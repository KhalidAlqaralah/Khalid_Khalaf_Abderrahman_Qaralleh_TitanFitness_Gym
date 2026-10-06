import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Member } from '../../../core/models/api.models';
import { applyServerErrors } from '../../../core/services/api-errors';
import { BranchContextService } from '../../../core/services/branch-context.service';
import { MemberService } from '../../../core/services/member.service';
import { ToastService } from '../../../core/services/toast.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { AutofocusDirective } from '../../../shared/directives/autofocus.directive';
import { memberNameError, memberNameValidators } from './member-name.validator';

/** Edit Member: name and branch, pre-filled; member ID and status shown read-only. */
@Component({
  selector: 'app-edit-member-dialog',
  imports: [ReactiveFormsModule, StatusBadgeComponent, AutofocusDirective],
  template: `
    <div class="dialog-header">
      <h2>Edit Member</h2>
      <button type="button" class="btn-icon" aria-label="Close" (click)="ref.close()"><i class="bi bi-x-lg"></i></button>
    </div>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <div class="dialog-body">
        <div class="d-flex align-items-center gap-3 mb-3 p-2 rounded bg-light">
          <span class="small text-muted-tf">Member ID</span><strong data-testid="edit-member-id">#{{ data.membershipNumber }}</strong>
          <app-status-badge class="ms-auto" [status]="data.status" />
        </div>
        <div class="mb-3">
          <label class="form-label req" for="edit-name">Member name</label>
          <input id="edit-name" class="form-control" formControlName="fullName" appAutofocus maxlength="80" [class.is-invalid]="nameError()">
          @if (nameError(); as message) { <div class="invalid-feedback">{{ message }}</div> }
        </div>
        <div>
          <label class="form-label req" for="edit-branch">Branch</label>
          <select id="edit-branch" class="form-select" formControlName="homeBranchId">
            @for (b of branches.branches(); track b.id) { <option [value]="b.id">{{ b.name }} Branch</option> }
          </select>
        </div>
      </div>
      <div class="dialog-footer">
        <button type="button" class="btn btn-outline-navy" (click)="ref.close()">Cancel</button>
        <button type="submit" class="btn btn-navy" [disabled]="saving() || form.pristine" data-testid="edit-member-save">Save Changes</button>
      </div>
    </form>
  `
})
export class EditMemberDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly members = inject(MemberService);
  private readonly toast = inject(ToastService);
  readonly branches = inject(BranchContextService);
  readonly data = inject<Member>(MAT_DIALOG_DATA);
  readonly ref = inject<MatDialogRef<EditMemberDialogComponent, boolean>>(MatDialogRef);

  readonly form = this.fb.nonNullable.group({
    fullName: [this.data.fullName, memberNameValidators()],
    homeBranchId: [this.data.homeBranchId, Validators.required]
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
    this.members.update(this.data.id, fullName.trim(), homeBranchId).subscribe({
      next: () => {
        this.toast.success('Member updated');
        this.ref.close(true);
      },
      error: err => {
        this.saving.set(false);
        applyServerErrors(this.form, err);
      }
    });
  }
}
