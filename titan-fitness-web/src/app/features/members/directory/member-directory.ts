import { Component, OnDestroy, computed, effect, inject, signal, untracked } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { MemberListItem, MemberStatus, SortDirection } from '../../../core/models/api.models';
import { initials } from '../../../core/models/dates';
import { BranchContextService } from '../../../core/services/branch-context.service';
import { CheckInService } from '../../../core/services/check-in.service';
import { HeaderSearchService } from '../../../core/services/header-search.service';
import { MemberService } from '../../../core/services/member.service';
import { NavigationMemoryService } from '../../../core/services/navigation-memory.service';
import { DataTableComponent, SortState, TableColumn } from '../../../shared/components/data-table/data-table';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';
import { RowMenuComponent, RowMenuItem } from '../../../shared/components/row-menu/row-menu';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { HighlightPipe } from '../../../shared/pipes/highlight.pipe';
import { RelativeDayPipe } from '../../../shared/pipes/relative-day.pipe';
import { NewCheckInDialogComponent } from '../../check-in/new-check-in-dialog';
import { AddMemberDialogComponent } from '../dialogs/add-member-dialog';
import { MemberFilterDialogComponent, MemberFilters } from '../dialogs/member-filter-dialog';

const PAGE_SIZE = 10;

/**
 * Member Directory. Page, sort, search and filters are signals kept in the query string,
 * so Back from a profile returns to the same view and the URL can be shared.
 */
@Component({
  selector: 'app-member-directory',
  imports: [DataTableComponent, PaginatorComponent, RowMenuComponent, StatusBadgeComponent, HighlightPipe, RelativeDayPipe],
  templateUrl: './member-directory.html'
})
export class MemberDirectoryComponent implements OnDestroy {
  private readonly members = inject(MemberService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly memory = inject(NavigationMemoryService);
  private readonly checkIns = inject(CheckInService);
  readonly branches = inject(BranchContextService);
  readonly search = inject(HeaderSearchService);
  private searchTicket = 0;

  readonly columns: TableColumn[] = [
    { key: 'name', label: 'Member Name', sortable: true },
    { key: 'number', label: 'ID', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'branch', label: 'Branch', sortable: true },
    { key: 'lastVisit', label: 'Last Visit', sortable: true },
    { key: 'actions', label: 'Actions', align: 'end' }
  ];

  // ---------- list state (from the URL) ----------
  private readonly params = this.route.snapshot.queryParamMap;
  readonly page = signal(Number(this.params.get('page')) || 1);
  readonly sort = signal<SortState>({
    sortBy: this.params.get('sortBy') ?? 'name',
    sortDirection: (this.params.get('dir') as SortDirection) ?? 'Asc'
  });
  readonly filters = signal<MemberFilters>({
    branchId: this.params.get('branch'),
    statuses: this.params.getAll('status') as MemberStatus[]
  });

  readonly activeFilterCount = computed(() => (this.filters().branchId ? 1 : 0) + this.filters().statuses.length);

  readonly rows = signal<MemberListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.searchTicket = this.search.enable('Search members...', this.params.get('q') ?? '');

    // A new search starts from page 1.
    let firstTerm = true;
    effect(() => {
      this.search.term();
      if (firstTerm) firstTerm = false;
      else untracked(() => this.page.set(1));
    });

    // Every state change: write it to the URL and reload. A saved check-in refreshes Last Visit.
    effect(() => {
      const state = { page: this.page(), sort: this.sort(), filters: this.filters(), term: this.search.term() };
      this.checkIns.lastCheckIn();
      untracked(() => {
        this.router.navigate([], {
          relativeTo: this.route,
          replaceUrl: true,
          queryParams: {
            page: state.page > 1 ? state.page : null,
            sortBy: state.sort.sortBy !== 'name' ? state.sort.sortBy : null,
            dir: state.sort.sortDirection !== 'Asc' ? state.sort.sortDirection : null,
            q: state.term || null,
            branch: state.filters.branchId,
            status: state.filters.statuses.length ? state.filters.statuses : null
          }
        }).then(() => this.memory.remember('members', this.router.url));
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
    this.members
      .list(
        {
          page: this.page(),
          pageSize: PAGE_SIZE,
          search: this.search.term(),
          sortBy: this.sort().sortBy,
          sortDirection: this.sort().sortDirection,
          branchId: this.filters().branchId,
          statuses: this.filters().statuses
        },
        () => this.load()
      )
      .subscribe({
        next: result => {
          this.rows.set(result.items);
          this.total.set(result.totalCount);
          this.loading.set(false);
          if (result.items.length === 0 && result.totalCount > 0 && this.page() > 1) this.page.set(result.totalPages);
        },
        error: () => {
          this.loading.set(false);
          this.error.set("Couldn't load members.");
        }
      });
  }

  onSort(sort: SortState): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  openFilters(): void {
    const ref = this.dialog.open(MemberFilterDialogComponent, { width: '520px', data: this.filters() });
    ref.componentInstance.filtersApplied.subscribe(filters => {
      this.filters.set(filters);
      this.page.set(1);
    });
  }

  addMember(): void {
    this.dialog.open(AddMemberDialogComponent, { width: '520px' }).afterClosed().subscribe(created => {
      if (created) this.load();
    });
  }

  menuFor(m: MemberListItem): RowMenuItem[] {
    const active = m.status === 'Active';
    const notActive = `Not available: membership is ${m.status === 'None' ? 'missing' : m.status.toLowerCase()}.`;
    return [
      { id: 'view', label: 'View Profile' },
      { id: 'checkin', label: 'Check-In', disabled: !active, reason: notActive },
      { id: 'book', label: 'Book Class', disabled: !active, reason: notActive },
      {
        id: 'freeze',
        label: 'Freeze Membership',
        disabled: !active || m.remainingFreezes <= 0,
        reason: !active ? notActive : 'No freezes remaining on this plan.'
      }
    ];
  }

  onAction(action: string, m: MemberListItem): void {
    switch (action) {
      case 'view':
        this.router.navigate(['/members', m.id]);
        break;
      case 'checkin':
        this.dialog.open(NewCheckInDialogComponent, { width: '560px', data: { member: m } });
        break;
      case 'book':
        this.router.navigate(['/classes'], { queryParams: { bookFor: m.id } });
        break;
      case 'freeze':
        this.router.navigate(['/members', m.id, 'freeze']);
        break;
    }
  }

  initialsOf(name: string): string {
    return initials(name);
  }
}
