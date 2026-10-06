import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, computed, effect, inject, signal, untracked } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router, RouterLink } from '@angular/router';
import { ActiveMembers, CheckInsToday, ClassSession, ClassState } from '../../core/models/api.models';
import { timeOnDate } from '../../core/models/dates';
import { BranchContextService } from '../../core/services/branch-context.service';
import { CheckInService } from '../../core/services/check-in.service';
import { ClassService } from '../../core/services/class.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { HeaderSearchService } from '../../core/services/header-search.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { HighlightPipe } from '../../shared/pipes/highlight.pipe';
import { NewCheckInDialogComponent } from '../check-in/new-check-in-dialog';
import { AddMemberDialogComponent } from '../members/dialogs/add-member-dialog';
import { KpiCardComponent } from './kpi-card';

type StateFilter = 'All' | ClassState;

/** Landing page: live floor KPIs for the header branch, today's upcoming classes and quick actions. */
@Component({
  selector: 'app-dashboard',
  imports: [KpiCardComponent, StatusBadgeComponent, RouterLink, DatePipe, DecimalPipe, HighlightPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnDestroy {
  private readonly dashboard = inject(DashboardService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  readonly branches = inject(BranchContextService);
  readonly search = inject(HeaderSearchService);
  private searchTicket = 0;
  private readonly checkIns = inject(CheckInService);
  private readonly classes = inject(ClassService);

  readonly checkInsToday = signal<CheckInsToday | null>(null);
  readonly activeMembers = signal<ActiveMembers | null>(null);
  readonly upcoming = signal<ClassSession[]>([]);
  readonly loadingClasses = signal(true);
  readonly classesError = signal<string | null>(null);
  readonly stateFilter = signal<StateFilter>('All');
  readonly stateFilters: StateFilter[] = ['All', 'InProgress', 'Upcoming', 'Full', 'Cancelled'];

  /** Upcoming Classes after the header search and the state chips — derived, never stored. */
  readonly filteredClasses = computed(() => {
    const term = this.search.term().toLowerCase();
    const state = this.stateFilter();
    return this.upcoming().filter(
      c =>
        (state === 'All' || c.state === state) &&
        (!term || [c.className, c.trainerName, c.studioName].some(v => v?.toLowerCase().includes(term)))
    );
  });

  readonly changeText = computed(() => {
    const kpi = this.checkInsToday();
    if (!kpi) return '';
    if (kpi.changePercent === null) return `${kpi.sameDayLastWeek} on the same day last week`;
    return `${kpi.changePercent >= 0 ? '+' : ''}${kpi.changePercent}% vs last week`;
  });

  private readonly refresh = setInterval(() => this.loadKpis(), 60_000);

  constructor() {
    this.searchTicket = this.search.enable('Search classes...');

    // Reload when the header branch changes, after a check-in is saved, or after a class changes.
    effect(() => {
      this.branches.currentBranch();
      this.checkIns.lastCheckIn();
      this.classes.changes();
      untracked(() => {
        this.loadKpis();
        this.loadClasses();
      });
    });
  }

  ngOnDestroy(): void {
    clearInterval(this.refresh);
    this.search.disable(this.searchTicket);
  }

  loadKpis(): void {
    const branchId = this.branches.currentBranch()?.id ?? null;
    if (!branchId) return;
    this.dashboard.checkInsToday(branchId).subscribe({ next: kpi => this.checkInsToday.set(kpi) });
    this.dashboard.activeMembers(branchId).subscribe({ next: kpi => this.activeMembers.set(kpi) });
  }

  loadClasses(): void {
    const branchId = this.branches.currentBranch()?.id ?? null;
    if (!branchId) return;
    this.loadingClasses.set(true);
    this.classesError.set(null);
    this.dashboard.upcomingClasses(branchId, 10, () => this.loadClasses()).subscribe({
      next: list => {
        this.upcoming.set(list);
        this.loadingClasses.set(false);
      },
      error: () => {
        this.loadingClasses.set(false);
        this.classesError.set("Couldn't load today's classes.");
      }
    });
  }

  startTime(c: ClassSession): Date {
    return timeOnDate(c.startTime);
  }

  shortName(name: string | null): string {
    if (!name) return 'No trainer';
    const [first, last] = name.split(' ');
    return last ? `${first} ${last[0]}.` : first;
  }

  newMember(): void {
    this.dialog.open(AddMemberDialogComponent, { width: '520px' });
  }

  manualCheckIn(): void {
    this.dialog.open(NewCheckInDialogComponent, { width: '560px', data: {} });
  }

  registerClass(): void {
    this.router.navigate(['/classes']);
  }
}
