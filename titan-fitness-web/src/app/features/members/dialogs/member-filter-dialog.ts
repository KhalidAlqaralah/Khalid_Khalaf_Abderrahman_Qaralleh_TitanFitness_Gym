import { Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MemberStatus } from '../../../core/models/api.models';
import { BranchContextService } from '../../../core/services/branch-context.service';

export interface MemberFilters {
  branchId: string | null;
  statuses: MemberStatus[];
}

/** Filter Members: one branch (default All Branches) and any of the statuses. Emits filtersApplied. */
@Component({
  selector: 'app-member-filter-dialog',
  imports: [FormsModule],
  template: `
    <div class="dialog-header">
      <h2>Filter Members</h2>
      <button type="button" class="btn-icon" aria-label="Close" (click)="ref.close()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="dialog-body">
      <label class="form-label" for="mf-branch">Branch</label>
      <select id="mf-branch" class="form-select mb-3" [ngModel]="branchId()" (ngModelChange)="branchId.set($event || null)">
        <option value="">All Branches</option>
        @for (b of branches.branches(); track b.id) { <option [value]="b.id">{{ b.name }}</option> }
      </select>
      <span class="form-label d-block">Status</span>
      @for (s of allStatuses; track s.value) {
        <div class="form-check form-check-inline">
          <input class="form-check-input" type="checkbox" [id]="'mf-' + s.value" [checked]="statuses().includes(s.value)" (change)="toggle(s.value)">
          <label class="form-check-label" [for]="'mf-' + s.value">{{ s.label }}</label>
        </div>
      }
    </div>
    <div class="dialog-footer">
      <button type="button" class="btn btn-link text-muted-tf me-auto" (click)="clear()">Clear all</button>
      <button type="button" class="btn btn-outline-navy" (click)="ref.close()">Cancel</button>
      <button type="button" class="btn btn-navy" (click)="apply()" data-testid="filter-apply">Apply Filters</button>
    </div>
  `
})
export class MemberFilterDialogComponent {
  readonly branches = inject(BranchContextService);
  readonly ref = inject<MatDialogRef<MemberFilterDialogComponent, MemberFilters>>(MatDialogRef);
  private readonly initial = inject<MemberFilters>(MAT_DIALOG_DATA);

  readonly filtersApplied = output<MemberFilters>();

  readonly allStatuses: { value: MemberStatus; label: string }[] = [
    { value: 'Active', label: 'Active' },
    { value: 'Frozen', label: 'Frozen' },
    { value: 'Expired', label: 'Expired' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Cancelled', label: 'Cancelled' },
    { value: 'None', label: 'No plan' }
  ];

  readonly branchId = signal<string | null>(this.initial.branchId);
  readonly statuses = signal<MemberStatus[]>([...this.initial.statuses]);

  toggle(status: MemberStatus): void {
    this.statuses.update(list => (list.includes(status) ? list.filter(s => s !== status) : [...list, status]));
  }

  clear(): void {
    this.branchId.set(null);
    this.statuses.set([]);
  }

  apply(): void {
    const filters = { branchId: this.branchId(), statuses: this.statuses() };
    this.filtersApplied.emit(filters);
    this.ref.close(filters);
  }
}
