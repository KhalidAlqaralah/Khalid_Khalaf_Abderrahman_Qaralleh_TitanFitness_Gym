import { Component, OnDestroy, computed, effect, inject, signal, untracked } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SortDirection, TrainerFilters, TrainerListItem } from '../../core/models/api.models';
import { initials } from '../../core/models/dates';
import { HeaderSearchService } from '../../core/services/header-search.service';
import { NavigationMemoryService } from '../../core/services/navigation-memory.service';
import { TrainerService } from '../../core/services/trainer.service';
import { DataTableComponent, SortState, TableColumn } from '../../shared/components/data-table/data-table';
import { PaginatorComponent } from '../../shared/components/paginator/paginator';
import { RowMenuComponent, RowMenuItem } from '../../shared/components/row-menu/row-menu';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { HighlightPipe } from '../../shared/pipes/highlight.pipe';
import { TrainerFilterDialogComponent } from './trainer-filter-dialog';

const PAGE_SIZE = 10;

/** Trainer Directory (parent): data table, status badge, filter dialog and paginator are its children. */
@Component({
  selector: 'app-trainer-directory',
  imports: [DataTableComponent, PaginatorComponent, RowMenuComponent, StatusBadgeComponent, HighlightPipe, RouterLink],
  template: `
    <div class="d-flex flex-wrap align-items-start justify-content-between gap-3 mb-4">
      <div>
        <h1>Trainer Directory</h1>
        <p class="page-subtitle">Manage and view all trainers.</p>
      </div>
      <div class="d-flex gap-2">
        <button type="button" class="btn btn-outline-navy" (click)="openFilters()" data-testid="trainers-filter">
          <i class="bi bi-funnel me-1"></i> Filter
          @if (activeFilterCount() > 0) { <span class="badge text-bg-primary ms-1">{{ activeFilterCount() }}</span> }
        </button>
        <a routerLink="/trainers/new" class="btn btn-navy" data-testid="add-trainer"><i class="bi bi-plus-lg me-1"></i> Add Trainer</a>
      </div>
    </div>

    <div class="tf-card">
      <app-data-table [columns]="columns" [rows]="rows()" [sort]="sort()" [loading]="loading()" [error]="error()"
                      (sortChange)="onSort($event)" (retry)="load()" emptyText="No trainer matches the current search and filters.">
        <ng-template #row let-t>
          <tr data-testid="trainer-row">
            <td>
              <a class="d-flex align-items-center gap-3 text-reset text-decoration-none" [routerLink]="['/trainers', t.id]">
                <span class="avatar">{{ initialsOf(t.name) }}</span><span [innerHTML]="t.name | highlight: search.term()"></span>
              </a>
            </td>
            <td class="text-muted-tf" [innerHTML]="'#' + t.code | highlight: search.term()"></td>
            <td [innerHTML]="(t.specialty ?? '—') | highlight: search.term()"></td>
            <td [innerHTML]="t.branchName | highlight: search.term()"></td>
            <td><app-status-badge [status]="t.isActive ? 'Active' : 'Inactive'" /></td>
            <td class="text-end"><app-row-menu [items]="menu" [rowLabel]="t.name" (view)="view(t)" (edit)="edit(t)" /></td>
          </tr>
        </ng-template>
      </app-data-table>
      <app-paginator [total]="total()" [page]="page()" [pageSize]="10" (pageChange)="page.set($event)" />
    </div>
  `
})
export class TrainerDirectoryComponent implements OnDestroy {
  private readonly trainers = inject(TrainerService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly memory = inject(NavigationMemoryService);
  readonly search = inject(HeaderSearchService);
  private searchTicket = 0;

  readonly columns: TableColumn[] = [
    { key: 'name', label: 'Trainer Name', sortable: true },
    { key: 'code', label: 'ID', sortable: true },
    { key: 'specialty', label: 'Specialty', sortable: true },
    { key: 'branch', label: 'Branch', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'actions', label: 'Actions', align: 'end' }
  ];
  readonly menu: RowMenuItem[] = [{ id: 'view', label: 'View Trainer' }, { id: 'edit', label: 'Update Trainer' }];

  private readonly params = this.route.snapshot.queryParamMap;
  readonly page = signal(Number(this.params.get('page')) || 1);
  readonly sort = signal<SortState>({ sortBy: this.params.get('sortBy') ?? 'name', sortDirection: (this.params.get('dir') as SortDirection) ?? 'Asc' });
  readonly filters = signal<TrainerFilters>({
    branchIds: this.params.getAll('branch'),
    specialties: this.params.getAll('specialty'),
    statuses: this.params.getAll('status') as ('Active' | 'Inactive')[]
  });
  readonly activeFilterCount = computed(() => this.filters().branchIds.length + this.filters().specialties.length + this.filters().statuses.length);

  readonly rows = signal<TrainerListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.searchTicket = this.search.enable('Search trainers...', this.params.get('q') ?? '');

    let firstTerm = true;
    effect(() => {
      this.search.term();
      if (firstTerm) firstTerm = false;
      else untracked(() => this.page.set(1));
    });

    effect(() => {
      const state = { page: this.page(), sort: this.sort(), filters: this.filters(), term: this.search.term() };
      untracked(() => {
        this.router.navigate([], {
          relativeTo: this.route,
          replaceUrl: true,
          queryParams: {
            page: state.page > 1 ? state.page : null,
            sortBy: state.sort.sortBy !== 'name' ? state.sort.sortBy : null,
            dir: state.sort.sortDirection !== 'Asc' ? state.sort.sortDirection : null,
            q: state.term || null,
            branch: state.filters.branchIds.length ? state.filters.branchIds : null,
            specialty: state.filters.specialties.length ? state.filters.specialties : null,
            status: state.filters.statuses.length ? state.filters.statuses : null
          }
        }).then(() => this.memory.remember('trainers', this.router.url));
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
    this.trainers
      .list({ page: this.page(), pageSize: PAGE_SIZE, search: this.search.term(), sortBy: this.sort().sortBy, sortDirection: this.sort().sortDirection, ...this.filters() }, () => this.load())
      .subscribe({
        next: result => {
          this.rows.set(result.items);
          this.total.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.error.set("Couldn't load trainers.");
        }
      });
  }

  onSort(sort: SortState): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  openFilters(): void {
    const ref = this.dialog.open(TrainerFilterDialogComponent, { width: '520px', data: this.filters() });
    ref.componentInstance.filtersApplied.subscribe(filters => {
      this.filters.set(filters);
      this.page.set(1);
    });
  }

  view(t: TrainerListItem): void {
    this.router.navigate(['/trainers', t.id]);
  }

  edit(t: TrainerListItem): void {
    this.router.navigate(['/trainers', t.id, 'edit']);
  }

  initialsOf(name: string): string {
    return initials(name);
  }
}
