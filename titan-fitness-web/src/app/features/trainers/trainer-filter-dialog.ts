import { Component, OnInit, computed, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { TrainerFilters } from '../../core/models/api.models';
import { BranchContextService } from '../../core/services/branch-context.service';
import { TrainerService } from '../../core/services/trainer.service';

/** Filter Trainers: branches (multi-select), specialties (multi-select with type-ahead), status checkboxes. */
@Component({
  selector: 'app-trainer-filter-dialog',
  imports: [FormsModule, MatSelectModule],
  template: `
    <div class="dialog-header">
      <h2>Filter Trainers</h2>
      <button type="button" class="btn-icon" aria-label="Close" (click)="ref.close()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="dialog-body">
      <label class="form-label" for="tf-branch">Branch</label>
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="mb-3">
        <mat-select id="tf-branch" multiple placeholder="All branches" [ngModel]="branchIds()" (ngModelChange)="branchIds.set($event)" data-testid="filter-branches">
          @for (b of branches.branches(); track b.id) { <mat-option [value]="b.id">{{ b.name }}</mat-option> }
        </mat-select>
      </mat-form-field>

      <label class="form-label" for="tf-specialty">Specialty</label>
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="mb-3">
        <mat-select id="tf-specialty" multiple placeholder="All specialties" [ngModel]="specialties()" (ngModelChange)="specialties.set($event)"
                    (openedChange)="typeahead.set('')" data-testid="filter-specialties">
          <div class="px-3 pt-2 pb-1 bg-white sticky-top">
            <input class="form-control form-control-sm" placeholder="Type to filter…" [ngModel]="typeahead()" (ngModelChange)="typeahead.set($event)"
                   (keydown)="$event.stopPropagation()" aria-label="Filter specialties">
          </div>
          @for (s of visibleSpecialties(); track s) { <mat-option [value]="s">{{ s }}</mat-option> }
          @for (s of hiddenSelected(); track s) { <mat-option [value]="s" class="d-none">{{ s }}</mat-option> }
        </mat-select>
      </mat-form-field>

      <span class="form-label d-block">Status</span>
      @for (s of allStatuses; track s) {
        <div class="form-check form-check-inline">
          <input class="form-check-input" type="checkbox" [id]="'tfs-' + s" [checked]="statuses().includes(s)" (change)="toggle(s)">
          <label class="form-check-label" [for]="'tfs-' + s">{{ s }}</label>
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
export class TrainerFilterDialogComponent implements OnInit {
  private readonly trainers = inject(TrainerService);
  readonly branches = inject(BranchContextService);
  readonly ref = inject<MatDialogRef<TrainerFilterDialogComponent, TrainerFilters>>(MatDialogRef);
  private readonly initial = inject<TrainerFilters>(MAT_DIALOG_DATA);

  readonly filtersApplied = output<TrainerFilters>();

  readonly allStatuses: ('Active' | 'Inactive')[] = ['Active', 'Inactive'];
  readonly allSpecialties = signal<string[]>([]);
  readonly branchIds = signal<string[]>([...this.initial.branchIds]);
  readonly specialties = signal<string[]>([...this.initial.specialties]);
  readonly statuses = signal<('Active' | 'Inactive')[]>([...this.initial.statuses]);
  readonly typeahead = signal('');

  readonly visibleSpecialties = computed(() => {
    const term = this.typeahead().toLowerCase();
    return this.allSpecialties().filter(s => s.toLowerCase().includes(term));
  });

  /** Selected options hidden by the type-ahead stay registered so the selection is kept. */
  readonly hiddenSelected = computed(() => this.specialties().filter(s => !this.visibleSpecialties().includes(s)));

  ngOnInit(): void {
    this.trainers.specialties().subscribe({ next: list => this.allSpecialties.set(list) });
  }

  toggle(status: 'Active' | 'Inactive'): void {
    this.statuses.update(list => (list.includes(status) ? list.filter(s => s !== status) : [...list, status]));
  }

  clear(): void {
    this.branchIds.set([]);
    this.specialties.set([]);
    this.statuses.set([]);
  }

  apply(): void {
    const filters: TrainerFilters = { branchIds: this.branchIds(), specialties: this.specialties(), statuses: this.statuses() };
    this.filtersApplied.emit(filters);
    this.ref.close(filters);
  }
}
