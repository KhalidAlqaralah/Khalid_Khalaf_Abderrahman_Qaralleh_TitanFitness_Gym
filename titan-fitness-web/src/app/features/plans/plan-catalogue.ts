import { CurrencyPipe } from '@angular/common';
import { Component, OnDestroy, computed, effect, inject, signal, untracked } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AccessScope, Plan, PlanFilters, SortDirection } from '../../core/models/api.models';
import { HeaderSearchService } from '../../core/services/header-search.service';
import { NavigationMemoryService } from '../../core/services/navigation-memory.service';
import { PlanService } from '../../core/services/plan.service';
import { DataTableComponent, SortState, TableColumn } from '../../shared/components/data-table/data-table';
import { PaginatorComponent } from '../../shared/components/paginator/paginator';
import { RowMenuComponent, RowMenuItem } from '../../shared/components/row-menu/row-menu';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { FreezeAllowancePipe } from '../../shared/pipes/freeze-allowance.pipe';
import { HighlightPipe } from '../../shared/pipes/highlight.pipe';
import { LabelPipe } from '../../shared/pipes/labels.pipe';
import { PlanFilterDialogComponent } from './plan-filter-dialog';

const PAGE_SIZE = 10;

/** Plan Catalogue: paged, sortable, filterable list of membership plans. */
@Component({
  selector: 'app-plan-catalogue',
  imports: [DataTableComponent, PaginatorComponent, RowMenuComponent, StatusBadgeComponent, HighlightPipe, FreezeAllowancePipe, LabelPipe, CurrencyPipe, RouterLink],
  template: `
    <div class="d-flex flex-wrap align-items-start justify-content-between gap-3 mb-4">
      <div>
        <h1>Plan Catalogue</h1>
        <p class="page-subtitle">Manage and view all membership plans.</p>
      </div>
      <div class="d-flex gap-2">
        <button type="button" class="btn btn-outline-navy" (click)="openFilters()" data-testid="plans-filter">
          <i class="bi bi-funnel me-1"></i> Filter
          @if (activeFilterCount() > 0) { <span class="badge text-bg-primary ms-1">{{ activeFilterCount() }}</span> }
        </button>
        <a routerLink="/plans/new" class="btn btn-navy" data-testid="add-plan"><i class="bi bi-plus-lg me-1"></i> Add Plan</a>
      </div>
    </div>

    <div class="tf-card">
      <app-data-table [columns]="columns" [rows]="rows()" [sort]="sort()" [loading]="loading()" [error]="error()"
                      (sortChange)="onSort($event)" (retry)="load()" emptyText="No plan matches the current search and filters.">
        <ng-template #row let-p>
          <tr data-testid="plan-row">
            <td><a class="text-reset text-decoration-none fw-medium" [routerLink]="['/plans', p.id]" [innerHTML]="p.name | highlight: search.term()"></a></td>
            <td>{{ p.price | currency: 'USD' }}</td>
            <td>{{ p.durationInMonths }} {{ p.durationInMonths === 1 ? 'month' : 'months' }}</td>
            <td>{{ p.maxFreezeDays | freezeAllowance: p.maxFreezes }}</td>
            <td>{{ p.guestPassQuota }}</td>
            <td>{{ p.accessScope | label }}</td>
            <td><app-status-badge [status]="p.isPublished ? 'Published' : 'Retired'" /></td>
            <td class="text-end"><app-row-menu [items]="menu" [rowLabel]="p.name" (view)="view(p)" (edit)="edit(p)" /></td>
          </tr>
        </ng-template>
      </app-data-table>
      <app-paginator [total]="total()" [page]="page()" [pageSize]="10" (pageChange)="page.set($event)" />
    </div>
  `
})
export class PlanCatalogueComponent implements OnDestroy {
  private readonly plans = inject(PlanService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly memory = inject(NavigationMemoryService);
  readonly search = inject(HeaderSearchService);
  private searchTicket = 0;

  readonly columns: TableColumn[] = [
    { key: 'name', label: 'Plan Name', sortable: true },
    { key: 'price', label: 'Price', sortable: true },
    { key: 'duration', label: 'Duration', sortable: true },
    { key: 'freeze', label: 'Freeze Allowance' },
    { key: 'guestPasses', label: 'Guest Passes', sortable: true },
    { key: 'access', label: 'Access', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'actions', label: 'Actions', align: 'end' }
  ];
  readonly menu: RowMenuItem[] = [{ id: 'view', label: 'View Plan' }, { id: 'edit', label: 'Update Plan' }];

  private readonly params = this.route.snapshot.queryParamMap;
  readonly page = signal(Number(this.params.get('page')) || 1);
  readonly sort = signal<SortState>({ sortBy: this.params.get('sortBy') ?? 'name', sortDirection: (this.params.get('dir') as SortDirection) ?? 'Asc' });
  readonly filters = signal<PlanFilters>({
    durations: this.params.getAll('duration').map(Number),
    access: (this.params.get('access') as AccessScope | null) ?? null,
    minPrice: this.params.get('min') ? Number(this.params.get('min')) : null,
    maxPrice: this.params.get('max') ? Number(this.params.get('max')) : null,
    statuses: this.params.getAll('status') as ('Published' | 'Retired')[]
  });
  readonly activeFilterCount = computed(() => {
    const f = this.filters();
    return f.durations.length + (f.access ? 1 : 0) + (f.minPrice !== null || f.maxPrice !== null ? 1 : 0) + f.statuses.length;
  });

  readonly rows = signal<Plan[]>([]);
  readonly total = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.searchTicket = this.search.enable('Search plans...', this.params.get('q') ?? '');

    let firstTerm = true;
    effect(() => {
      this.search.term();
      if (firstTerm) firstTerm = false;
      else untracked(() => this.page.set(1));
    });

    effect(() => {
      const state = { page: this.page(), sort: this.sort(), f: this.filters(), term: this.search.term() };
      untracked(() => {
        this.router.navigate([], {
          relativeTo: this.route,
          replaceUrl: true,
          queryParams: {
            page: state.page > 1 ? state.page : null,
            sortBy: state.sort.sortBy !== 'name' ? state.sort.sortBy : null,
            dir: state.sort.sortDirection !== 'Asc' ? state.sort.sortDirection : null,
            q: state.term || null,
            duration: state.f.durations.length ? state.f.durations : null,
            access: state.f.access,
            min: state.f.minPrice,
            max: state.f.maxPrice,
            status: state.f.statuses.length ? state.f.statuses : null
          }
        }).then(() => this.memory.remember('plans', this.router.url));
        this.load();
      });
    });
  }

  ngOnDestroy(): void {
    this.search.disable(this.searchTicket);
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.plans
      .list({ page: this.page(), pageSize: PAGE_SIZE, search: this.search.term(), sortBy: this.sort().sortBy, sortDirection: this.sort().sortDirection, ...this.filters() }, () => this.load())
      .subscribe({
        next: result => {
          this.rows.set(result.items);
          this.total.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.error.set("Couldn't load plans.");
        }
      });
  }

  onSort(sort: SortState): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  openFilters(): void {
    const ref = this.dialog.open(PlanFilterDialogComponent, { width: '540px', data: this.filters() });
    ref.componentInstance.filtersApplied.subscribe(filters => {
      this.filters.set(filters);
      this.page.set(1);
    });
  }

  view(p: Plan): void {
    this.router.navigate(['/plans', p.id]);
  }

  edit(p: Plan): void {
    this.router.navigate(['/plans', p.id, 'edit']);
  }
}
