import { DatePipe, PercentPipe } from '@angular/common';
import { Component, OnDestroy, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { CapacityOverview, ClassSession, MemberListItem } from '../../core/models/api.models';
import { addDays, parseDateOnly, sameDay, timeOnDate, toDateOnly, today } from '../../core/models/dates';
import { BranchContextService } from '../../core/services/branch-context.service';
import { ClassService } from '../../core/services/class.service';
import { HeaderSearchService } from '../../core/services/header-search.service';
import { MemberService } from '../../core/services/member.service';
import { NavigationMemoryService } from '../../core/services/navigation-memory.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmService } from '../../shared/components/confirm-dialog/confirm.service';
import { RowMenuComponent, RowMenuItem } from '../../shared/components/row-menu/row-menu';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { HighlightPipe } from '../../shared/pipes/highlight.pipe';
import { BookSessionDialogComponent } from './book-session-dialog';
import { ClassDialogComponent, ClassDialogMode } from './class-dialog';

interface DayGroup {
  date: string;
  label: string;
  sessions: ClassSession[];
}

/**
 * Class Schedule: sessions for one branch (or all) on a day or a week, with live capacity and the
 * Capacity Overview. Branch, date, view and search are kept in the query string.
 */
@Component({
  selector: 'app-class-schedule',
  imports: [FormsModule, DatePipe, PercentPipe, RowMenuComponent, StatusBadgeComponent, HighlightPipe],
  templateUrl: './class-schedule.html',
  styleUrl: './class-schedule.css'
})
export class ClassScheduleComponent implements OnDestroy {
  private readonly classes = inject(ClassService);
  private readonly membersApi = inject(MemberService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly memory = inject(NavigationMemoryService);
  readonly branches = inject(BranchContextService);
  readonly search = inject(HeaderSearchService);
  private searchTicket = 0;

  private readonly params = this.route.snapshot.queryParamMap;

  /** '' means All Branches; null means "follow the header branch". */
  readonly branchFilter = signal<string | null>(this.params.has('branch') ? this.params.get('branch') : null);
  readonly date = signal<Date>(this.params.get('date') ? parseDateOnly(this.params.get('date')!) : today());
  readonly view = signal<'day' | 'week'>(this.params.get('view') === 'week' ? 'week' : 'day');
  readonly bookFor = signal<MemberListItem | null>(null);

  readonly effectiveBranch = computed(() => this.branchFilter() ?? this.branches.currentBranch()?.id ?? '');
  readonly allBranches = computed(() => this.effectiveBranch() === '');

  readonly sessions = signal<ClassSession[]>([]);
  readonly overview = signal<CapacityOverview | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly isToday = computed(() => sameDay(this.date(), today()));
  readonly subtitle = computed(() =>
    this.view() === 'week'
      ? `Sessions for ${this.fmt(this.date())} – ${this.fmt(addDays(this.date(), 6))}`
      : this.isToday() ? "Manage and monitor today's sessions." : `Sessions for ${this.fmt(this.date())}`
  );
  readonly listTitle = computed(() =>
    this.view() === 'week' ? 'This Week' : this.isToday() ? "Today's Sessions" : this.date().toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' })
  );

  readonly groups = computed<DayGroup[]>(() => {
    const map = new Map<string, ClassSession[]>();
    for (const s of this.sessions()) map.set(s.date, [...(map.get(s.date) ?? []), s]);
    return [...map.entries()].map(([date, sessions]) => ({
      date,
      label: parseDateOnly(date).toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' }),
      sessions
    }));
  });

  constructor() {
    this.searchTicket = this.search.enable('Search classes...', this.params.get('q') ?? '');

    const bookFor = this.params.get('bookFor');
    if (bookFor) {
      this.membersApi.getById(bookFor).subscribe({
        next: m => this.bookFor.set({ id: m.id, fullName: m.fullName, membershipNumber: m.membershipNumber, status: m.status, branchId: m.homeBranchId, branchName: m.homeBranchName, photoUrl: m.photoUrl, lastVisit: m.lastVisit, remainingFreezes: 0 })
      });
    }

    effect(() => {
      const query = {
        branchId: this.effectiveBranch() || null,
        date: toDateOnly(this.date()),
        days: this.view() === 'week' ? 7 : 1,
        search: this.search.term() || null
      };
      this.classes.changes();
      untracked(() => {
        this.router.navigate([], {
          relativeTo: this.route,
          replaceUrl: true,
          queryParams: {
            branch: this.branchFilter(),
            date: this.isToday() ? null : query.date,
            view: this.view() === 'week' ? 'week' : null,
            q: query.search,
            bookFor: this.bookFor()?.id ?? this.params.get('bookFor')
          }
        }).then(() => this.memory.remember('classes', this.router.url));
        this.load(query);
      });
    });
  }

  ngOnDestroy(): void {
    this.search.disable(this.searchTicket);
  }

  load(query = { branchId: this.effectiveBranch() || null, date: toDateOnly(this.date()), days: this.view() === 'week' ? 7 : 1, search: this.search.term() || null }): void {
    this.loading.set(true);
    this.error.set(null);
    this.classes.schedule(query, () => this.load()).subscribe({
      next: list => {
        this.sessions.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set("Couldn't load the schedule.");
      }
    });
    this.classes.capacityOverview(query).subscribe({ next: o => this.overview.set(o) });
  }

  setBranch(value: string): void {
    this.branchFilter.set(value);
  }

  shiftDate(days: number): void {
    this.date.update(d => addDays(d, days * (this.view() === 'week' ? 7 : 1)));
  }

  pickDate(value: string): void {
    if (value) this.date.set(parseDateOnly(value));
  }

  dateInput(): string {
    return toDateOnly(this.date());
  }

  startOf(s: ClassSession): Date {
    return timeOnDate(s.startTime, parseDateOnly(s.date));
  }

  endOf(s: ClassSession): Date {
    return timeOnDate(s.endTime, parseDateOnly(s.date));
  }

  fillPercent(s: ClassSession): number {
    return s.capacityLimit === 0 ? 0 : Math.min(100, Math.round((s.enrolled / s.capacityLimit) * 100));
  }

  nearlyFull(s: ClassSession): boolean {
    return !s.isCancelled && s.enrolled / s.capacityLimit >= 0.9;
  }

  menuFor(s: ClassSession): RowMenuItem[] {
    const closed = s.isCancelled || s.state === 'Completed';
    const bookable = s.state === 'Upcoming' || s.state === 'Full';
    return [
      { id: 'view', label: 'View Class' },
      { id: 'edit', label: 'Edit Class', disabled: closed, reason: s.isCancelled ? 'This class was cancelled.' : 'This class has finished.' },
      { id: 'book', label: 'Book Session', disabled: !bookable, reason: s.state === 'InProgress' ? 'The class has already started.' : 'This class no longer takes bookings.' },
      { id: 'cancel', label: 'Cancel Class', disabled: closed, reason: s.isCancelled ? 'Already cancelled.' : 'This class has finished.' }
    ];
  }

  onAction(action: string, s: ClassSession): void {
    switch (action) {
      case 'view':
      case 'edit':
        this.openDialog(action as ClassDialogMode, s);
        break;
      case 'book':
        this.dialog.open(BookSessionDialogComponent, { width: '720px', data: { session: s, member: this.bookFor() } }).afterClosed().subscribe(done => {
          if (done && this.bookFor()) this.clearBookFor();
        });
        break;
      case 'cancel':
        this.confirm
          .ask({ title: 'Cancel class?', message: `Cancel ${s.className}? Every booking on it (${s.activeBookings}) is cancelled too.`, confirmText: 'Cancel Class', cancelText: 'Keep', danger: true })
          .subscribe(ok => {
            if (!ok) return;
            this.classes.cancel(s.id).subscribe({ next: () => this.toast.success(`${s.className} cancelled`) });
          });
        break;
    }
  }

  addClass(): void {
    this.openDialog('add');
  }

  clearBookFor(): void {
    this.bookFor.set(null);
    this.router.navigate([], { relativeTo: this.route, replaceUrl: true, queryParams: { bookFor: null }, queryParamsHandling: 'merge' });
  }

  private openDialog(mode: ClassDialogMode, session?: ClassSession): void {
    const open = (s?: ClassSession) =>
      this.dialog.open(ClassDialogComponent, {
        width: '860px',
        // Add opens with an empty date, as in Figure 6.
        data: { mode, session: s, branchId: this.effectiveBranch() || this.branches.currentBranch()?.id, date: null }
      });

    if (session) this.classes.getById(session.id).subscribe({ next: fresh => open(fresh) });
    else open();
  }

  private fmt(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
