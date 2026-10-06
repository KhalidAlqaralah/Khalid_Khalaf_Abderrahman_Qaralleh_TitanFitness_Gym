import { Component, DestroyRef, OnInit, inject, input, model, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { debounceTime, distinctUntilChanged, filter, switchMap } from 'rxjs';
import { MemberListItem } from '../../core/models/api.models';
import { initials } from '../../core/models/dates';
import { MemberService } from '../../core/services/member.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';

/**
 * Member autocomplete: search by name or member ID (#TF-1001); options show avatar, name, ID and status.
 * The chosen member is a two-way model(): [(member)].
 */
@Component({
  selector: 'app-member-picker',
  imports: [ReactiveFormsModule, MatAutocompleteModule, StatusBadgeComponent],
  template: `
    @if (member(); as chosen) {
      <div class="picked" data-testid="picked-member">
        <span class="avatar">{{ initialsOf(chosen.fullName) }}</span>
        <div class="flex-grow-1">
          <div class="fw-semibold">{{ chosen.fullName }}</div>
          <div class="small text-muted-tf">ID: #{{ chosen.membershipNumber }}</div>
        </div>
        <app-status-badge [status]="chosen.status" />
        @if (!locked()) {
          <button type="button" class="btn-icon" aria-label="Change member" (click)="clear()"><i class="bi bi-x-lg"></i></button>
        }
      </div>
    } @else {
      <div class="input-group">
        <span class="input-group-text bg-white"><i class="bi bi-search"></i></span>
        <input type="text" class="form-control" [class.is-invalid]="invalid()" [formControl]="search" [matAutocomplete]="auto"
               placeholder="Search by name or ID..." [attr.id]="inputId()" data-testid="member-search">
      </div>
      <mat-autocomplete #auto="matAutocomplete" (optionSelected)="pick($event)" [displayWith]="display">
        @for (option of options(); track option.id) {
          <mat-option [value]="option">
            <div class="d-flex align-items-center gap-2 w-100">
              <span class="avatar">{{ initialsOf(option.fullName) }}</span>
              <div class="flex-grow-1">
                <div class="fw-semibold">{{ option.fullName }}</div>
                <div class="small text-muted-tf">#{{ option.membershipNumber }} · {{ option.branchName }}</div>
              </div>
              <app-status-badge [status]="option.status" />
            </div>
          </mat-option>
        }
        @if (searched() && options().length === 0) {
          <mat-option disabled>No member matches "{{ search.value }}"</mat-option>
        }
      </mat-autocomplete>
    }
  `,
  styles: `.picked { display: flex; align-items: center; gap: .75rem; padding: .6rem .75rem; border: 1px solid var(--tf-line); background: #f3f5f9; border-radius: 6px; }`
})
export class MemberPickerComponent implements OnInit {
  private readonly members = inject(MemberService);
  private readonly destroyRef = inject(DestroyRef);

  readonly member = model<MemberListItem | null>(null);
  readonly locked = input(false);
  readonly invalid = input(false);
  readonly inputId = input('member-search');

  readonly search = new FormControl<string | MemberListItem>('', { nonNullable: true });
  readonly options = signal<MemberListItem[]>([]);
  readonly searched = signal(false);

  readonly display = (value: MemberListItem | string | null) => (typeof value === 'string' ? value : value?.fullName ?? '');

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(
        filter((value): value is string => typeof value === 'string'),
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(term => this.members.search(term)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(result => {
        this.options.set(result.items);
        this.searched.set(true);
      });
  }

  pick(event: MatAutocompleteSelectedEvent): void {
    this.member.set(event.option.value as MemberListItem);
  }

  clear(): void {
    this.member.set(null);
    this.search.setValue('');
    this.options.set([]);
    this.searched.set(false);
  }

  initialsOf(name: string): string {
    return initials(name);
  }
}
