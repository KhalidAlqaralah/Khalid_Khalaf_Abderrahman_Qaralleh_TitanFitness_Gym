import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSliderModule } from '@angular/material/slider';
import { AccessScope, PlanFilterOptions, PlanFilters } from '../../core/models/api.models';
import { PlanService } from '../../core/services/plan.service';

/** Filter Plans: duration chips, access radio, price range (inputs + slider), status checkboxes. */
@Component({
  selector: 'app-plan-filter-dialog',
  imports: [FormsModule, MatSliderModule, CurrencyPipe],
  template: `
    <div class="dialog-header">
      <h2>Filter Plans</h2>
      <button type="button" class="btn-icon" aria-label="Close" (click)="ref.close()"><i class="bi bi-x-lg"></i></button>
    </div>
    <div class="dialog-body">
      <span class="form-label d-block">Duration</span>
      <div class="d-flex flex-wrap gap-2 mb-3">
        @for (d of options()?.durations ?? []; track d) {
          <button type="button" class="btn btn-sm rounded-pill" [class.btn-navy]="durations().includes(d)" [class.btn-outline-navy]="!durations().includes(d)"
                  (click)="toggleDuration(d)" [attr.aria-pressed]="durations().includes(d)" [attr.data-testid]="'chip-' + d">
            {{ d }} {{ d === 1 ? 'month' : 'months' }}
          </button>
        }
      </div>

      <span class="form-label d-block">Access</span>
      <div class="mb-3">
        @for (a of accessOptions; track a.label) {
          <div class="form-check form-check-inline">
            <input class="form-check-input" type="radio" name="access" [id]="'pa-' + a.label" [checked]="access() === a.value" (change)="access.set(a.value)">
            <label class="form-check-label" [for]="'pa-' + a.label">{{ a.label }}</label>
          </div>
        }
      </div>

      <span class="form-label d-block">Price range (USD)</span>
      <div class="d-flex gap-2 align-items-center">
        <input type="number" class="form-control" placeholder="Min" min="0" [ngModel]="minPrice()" (ngModelChange)="minPrice.set($event === '' ? null : $event)" aria-label="Min price" data-testid="min-price">
        <span>–</span>
        <input type="number" class="form-control" placeholder="Max" min="0" [ngModel]="maxPrice()" (ngModelChange)="maxPrice.set($event === '' ? null : $event)" aria-label="Max price" data-testid="max-price">
      </div>
      @if (options(); as o) {
        <mat-slider class="w-100" [min]="o.minPrice" [max]="o.maxPrice" step="1" discrete>
          <input matSliderStartThumb [ngModel]="minPrice() ?? o.minPrice" (ngModelChange)="minPrice.set($event)" aria-label="Minimum price">
          <input matSliderEndThumb [ngModel]="maxPrice() ?? o.maxPrice" (ngModelChange)="maxPrice.set($event)" aria-label="Maximum price">
        </mat-slider>
        <div class="d-flex justify-content-between small text-muted-tf"><span>{{ o.minPrice | currency: 'USD' }}</span><span>{{ o.maxPrice | currency: 'USD' }}</span></div>
      }
      @if (rangeInvalid()) { <div class="invalid-feedback">Max price cannot be below min price.</div> }

      <span class="form-label d-block mt-3">Status</span>
      @for (s of allStatuses; track s) {
        <div class="form-check form-check-inline">
          <input class="form-check-input" type="checkbox" [id]="'ps-' + s" [checked]="statuses().includes(s)" (change)="toggleStatus(s)">
          <label class="form-check-label" [for]="'ps-' + s">{{ s }}</label>
        </div>
      }
    </div>
    <div class="dialog-footer">
      <button type="button" class="btn btn-link text-muted-tf me-auto" (click)="clear()">Clear all</button>
      <button type="button" class="btn btn-outline-navy" (click)="ref.close()">Cancel</button>
      <button type="button" class="btn btn-navy" [disabled]="rangeInvalid()" (click)="apply()" data-testid="filter-apply">Apply Filters</button>
    </div>
  `
})
export class PlanFilterDialogComponent implements OnInit {
  private readonly plans = inject(PlanService);
  readonly ref = inject<MatDialogRef<PlanFilterDialogComponent, PlanFilters>>(MatDialogRef);
  private readonly initial = inject<PlanFilters>(MAT_DIALOG_DATA);

  readonly filtersApplied = output<PlanFilters>();

  readonly accessOptions: { label: string; value: AccessScope | null }[] = [
    { label: 'Any', value: null },
    { label: 'All branches (full access)', value: 'AllBranches' },
    { label: 'Home branch only', value: 'HomeBranchOnly' }
  ];
  readonly allStatuses: ('Published' | 'Retired')[] = ['Published', 'Retired'];

  readonly options = signal<PlanFilterOptions | null>(null);
  readonly durations = signal<number[]>([...this.initial.durations]);
  readonly access = signal<AccessScope | null>(this.initial.access);
  readonly minPrice = signal<number | null>(this.initial.minPrice);
  readonly maxPrice = signal<number | null>(this.initial.maxPrice);
  readonly statuses = signal<('Published' | 'Retired')[]>([...this.initial.statuses]);

  ngOnInit(): void {
    this.plans.filterOptions().subscribe({ next: o => this.options.set(o) });
  }

  rangeInvalid(): boolean {
    const min = this.minPrice();
    const max = this.maxPrice();
    return min !== null && max !== null && Number(max) < Number(min);
  }

  toggleDuration(d: number): void {
    this.durations.update(list => (list.includes(d) ? list.filter(x => x !== d) : [...list, d].sort((a, b) => a - b)));
  }

  toggleStatus(s: 'Published' | 'Retired'): void {
    this.statuses.update(list => (list.includes(s) ? list.filter(x => x !== s) : [...list, s]));
  }

  clear(): void {
    this.durations.set([]);
    this.access.set(null);
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.statuses.set([]);
  }

  apply(): void {
    const o = this.options();
    // A slider left at its bounds means "no limit".
    const min = this.minPrice() !== null && Number(this.minPrice()) !== o?.minPrice ? Number(this.minPrice()) : null;
    const max = this.maxPrice() !== null && Number(this.maxPrice()) !== o?.maxPrice ? Number(this.maxPrice()) : null;
    const filters: PlanFilters = { durations: this.durations(), access: this.access(), minPrice: min, maxPrice: max, statuses: this.statuses() };
    this.filtersApplied.emit(filters);
    this.ref.close(filters);
  }
}
