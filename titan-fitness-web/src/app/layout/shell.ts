import { TitleCasePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { BranchContextService } from '../core/services/branch-context.service';
import { HeaderSearchService } from '../core/services/header-search.service';
import { NewCheckInDialogComponent } from '../features/check-in/new-check-in-dialog';
import { ClickOutsideDirective } from '../shared/directives/click-outside.directive';
import { initials } from '../core/models/dates';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  managerOnly?: boolean;
}

/** Staff layout: side menu, top bar (branch, search, calendar, user) and the content area. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ClickOutsideDirective, TitleCasePipe],
  templateUrl: './shell.html',
  styleUrl: './shell.css'
})
export class ShellComponent implements OnInit {
  readonly nav: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: 'bi-grid-1x2' },
    { path: '/members', label: 'Members', icon: 'bi-people' },
    { path: '/classes', label: 'Classes', icon: 'bi-arrows-angle-expand' },
    { path: '/trainers', label: 'Trainers', icon: 'bi-person-badge', managerOnly: true },
    { path: '/plans', label: 'Plans', icon: 'bi-tags', managerOnly: true }
  ];

  readonly userMenuOpen = signal(false);
  readonly branchMenuOpen = signal(false);

  constructor(
    public auth: AuthService,
    public branches: BranchContextService,
    public search: HeaderSearchService,
    private dialog: MatDialog,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.branches.load();
  }

  userInitials(): string {
    return initials(this.auth.session()?.displayName ?? '?');
  }

  openCheckIn(): void {
    this.dialog.open(NewCheckInDialogComponent, { width: '560px', data: {} });
  }

  chooseBranch(id: string): void {
    this.branches.select(id);
    this.branchMenuOpen.set(false);
  }

  signOut(): void {
    this.auth.logout();
    this.branches.reset();
    this.router.navigate(['/login']);
  }
}
