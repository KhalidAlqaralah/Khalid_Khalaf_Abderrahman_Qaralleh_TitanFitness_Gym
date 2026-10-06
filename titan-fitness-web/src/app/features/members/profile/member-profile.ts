import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, effect, inject, input, signal, untracked } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { Member, MemberActivity, Membership } from '../../../core/models/api.models';
import { CheckInService } from '../../../core/services/check-in.service';
import { HeaderSearchService } from '../../../core/services/header-search.service';
import { MemberService } from '../../../core/services/member.service';
import { MembershipService } from '../../../core/services/membership.service';
import { NavigationMemoryService } from '../../../core/services/navigation-memory.service';
import { ToastService } from '../../../core/services/toast.service';
import { EditMemberDialogComponent } from '../dialogs/edit-member-dialog';
import { SellPlanDialogComponent } from '../dialogs/sell-plan-dialog';
import { ActivityListComponent } from './activity-list';
import { IdentityCardComponent } from './identity-card';
import { PlanCardComponent } from './plan-card';
import { UsageCardComponent } from './usage-card';

/**
 * Member Profile (child route /members/:id). Parent page composing identity card, plan card,
 * usage cards and activity list. The URL is bookmarkable: :id is bound to the `id` input.
 */
@Component({
  selector: 'app-member-profile',
  imports: [IdentityCardComponent, PlanCardComponent, UsageCardComponent, ActivityListComponent],
  templateUrl: './member-profile.html'
})
export class MemberProfileComponent implements OnDestroy {
  private readonly members = inject(MemberService);
  private readonly memberships = inject(MembershipService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly memory = inject(NavigationMemoryService);
  private readonly toast = inject(ToastService);
  private readonly checkIns = inject(CheckInService);
  private readonly search = inject(HeaderSearchService);
  private searchTicket = 0;

  /** Route parameter :id. */
  readonly id = input.required<string>();

  readonly member = signal<Member | null>(null);
  readonly membership = signal<Membership | null>(null);
  readonly activity = signal<MemberActivity[]>([]);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly activityLoading = signal(true);
  /** False until the current membership request has answered (found or 404). */
  readonly membershipLoaded = signal(false);

  constructor() {
    // The header keeps "Search members…": typing searches the directory.
    this.searchTicket = this.search.enable('Search members...');
    effect(() => {
      const term = this.search.term();
      if (term) untracked(() => this.router.navigate(['/members'], { queryParams: { q: term } }));
    });

    effect(() => {
      const id = this.id();
      this.checkIns.lastCheckIn();
      untracked(() => this.load(id));
    });
  }

  ngOnDestroy(): void {
    this.search.disable(this.searchTicket);
  }

  load(id: string): void {
    this.loading.set(true);
    this.notFound.set(false);

    // The member first; plan and activity only when the member exists (no stray 404 toasts).
    this.members.getById(id).subscribe({
      next: member => {
        this.member.set(member);
        this.loading.set(false);
        this.loadDetails(id);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.notFound.set(err.status === 404);
      }
    });
  }

  private loadDetails(id: string): void {
    this.membershipLoaded.set(false);
    this.members.currentMembership(id).subscribe({
      next: m => {
        this.membership.set(m);
        this.membershipLoaded.set(true);
      },
      error: () => {
        this.membership.set(null);
        this.membershipLoaded.set(true);
      }
    });

    this.activityLoading.set(true);
    this.members.activity(id, 7).subscribe({
      next: list => {
        this.activity.set(list);
        this.activityLoading.set(false);
      },
      error: () => this.activityLoading.set(false)
    });
  }

  back(): void {
    this.router.navigateByUrl(this.memory.listUrl('members'));
  }

  edit(): void {
    const member = this.member();
    if (!member) return;
    this.dialog.open(EditMemberDialogComponent, { width: '520px', data: member }).afterClosed().subscribe(saved => {
      if (saved) this.load(member.id);
    });
  }

  freeze(): void {
    this.router.navigate(['/members', this.id(), 'freeze']);
  }

  sell(): void {
    const member = this.member();
    if (!member) return;
    this.dialog
      .open(SellPlanDialogComponent, { width: '520px', data: { memberId: member.id, memberName: member.fullName } })
      .afterClosed()
      .subscribe(sold => {
        if (sold) this.load(member.id);
      });
  }

  renew(): void {
    const m = this.membership();
    if (!m) return;
    this.memberships.renew(m.id, null).subscribe({
      next: () => {
        this.toast.success(`${m.planName} renewed`);
        this.load(this.id());
      }
    });
  }
}
