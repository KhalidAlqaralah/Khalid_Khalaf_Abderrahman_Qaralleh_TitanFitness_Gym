import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AccessScope, Plan, PlanRequest } from '../../core/models/api.models';
import { HasUnsavedChanges } from '../../core/guards/unsaved-changes.guard';
import { applyServerErrors } from '../../core/services/api-errors';
import { NavigationMemoryService } from '../../core/services/navigation-memory.service';
import { PlanService } from '../../core/services/plan.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmService } from '../../shared/components/confirm-dialog/confirm.service';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import { LabelPipe } from '../../shared/pipes/labels.pipe';

type Mode = 'add' | 'view' | 'edit';

const wholeNumber = Validators.pattern(/^\d+$/);

/** Max freezes must be 0 when max freeze days is 0. */
function freezesNeedDays(group: AbstractControl): ValidationErrors | null {
  const days = Number(group.get('maxFreezeDays')?.value || 0);
  const freezes = Number(group.get('maxFreezes')?.value || 0);
  return days === 0 && freezes > 0 ? { freezesWithoutDays: true } : null;
}

/** One page for View (/plans/:id), Add (/plans/new) and Update (/plans/:id/edit). */
@Component({
  selector: 'app-plan-details',
  imports: [ReactiveFormsModule, AutofocusDirective, LabelPipe],
  templateUrl: './plan-details.html'
})
export class PlanDetailsComponent implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly plans = inject(PlanService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly memory = inject(NavigationMemoryService);

  readonly id = input<string>();

  readonly mode = signal<Mode>(this.route.snapshot.data['mode'] as Mode);
  private openedFromView = false;
  private leaving = false;

  readonly plan = signal<Plan | null>(null);
  readonly loading = signal(this.mode() !== 'add');
  readonly notFound = signal(false);
  readonly saving = signal(false);
  readonly badge = computed(() => ({ add: 'Add mode', view: 'View mode', edit: 'Update mode' })[this.mode()]);
  readonly accessOptions: { value: AccessScope; label: string }[] = [
    { value: 'HomeBranchOnly', label: 'Home branch only' },
    { value: 'AllBranches', label: 'All branches' }
  ];

  readonly form = this.fb.group(
    {
      name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(60), c => ((c.value ?? '').trim().length === 1 ? { minlength: true } : null)]),
      price: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), c => (c.value !== null && Math.round(c.value * 100) !== c.value * 100 ? { decimals: true } : null)]),
      durationInMonths: this.fb.control<number | null>(null, [Validators.required, Validators.min(1), Validators.max(36), wholeNumber]),
      isPublished: this.fb.nonNullable.control(false),
      maxFreezeDays: this.fb.control<number | null>(null, [Validators.min(0), wholeNumber]),
      maxFreezes: this.fb.control<number | null>(null, [Validators.min(0), wholeNumber]),
      guestPassQuota: this.fb.control<number | null>(null, [Validators.min(0), wholeNumber]),
      accessScope: this.fb.control<AccessScope | null>(null)
    },
    { validators: freezesNeedDays }
  );

  ngOnInit(): void {
    if (this.mode() === 'add') return;
    this.plans.getById(this.id()!).subscribe({
      next: plan => {
        this.plan.set(plan);
        this.fill(plan);
        this.loading.set(false);
        this.applyMode();
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.notFound.set(err.status === 404);
      }
    });
  }

  private fill(p: Plan): void {
    this.form.reset({
      name: p.name,
      price: p.price,
      durationInMonths: p.durationInMonths,
      isPublished: p.isPublished,
      maxFreezeDays: p.maxFreezeDays,
      maxFreezes: p.maxFreezes,
      guestPassQuota: p.guestPassQuota,
      accessScope: p.accessScope
    });
  }

  private applyMode(): void {
    if (this.mode() === 'view') this.form.disable();
    else this.form.enable();
  }

  chooseAccess(value: AccessScope): void {
    if (this.mode() === 'view') return;
    this.form.controls.accessScope.setValue(value);
    this.form.controls.accessScope.markAsDirty();
  }

  editInPlace(): void {
    this.openedFromView = true;
    this.mode.set('edit');
    this.applyMode();
    this.location.go(`/plans/${this.id()}/edit`);
  }

  backToList(): void {
    this.router.navigateByUrl(this.memory.listUrl('plans'));
  }

  cancel(): void {
    const proceed = () => {
      if (this.mode() === 'edit' && this.openedFromView) {
        this.fill(this.plan()!);
        this.mode.set('view');
        this.applyMode();
        this.location.go(`/plans/${this.id()}`);
      } else {
        this.leaving = true;
        this.backToList();
      }
    };
    if (this.form.dirty) this.confirm.discardChanges().subscribe(ok => ok && proceed());
    else proceed();
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const v = this.form.getRawValue();
    const request: PlanRequest = {
      name: v.name.trim(),
      price: v.price,
      durationInMonths: v.durationInMonths,
      isPublished: v.isPublished,
      maxFreezeDays: v.maxFreezeDays ?? 0,
      maxFreezes: v.maxFreezes ?? 0,
      guestPassQuota: v.guestPassQuota ?? 0,
      accessScope: v.accessScope ?? 'HomeBranchOnly'
    };

    this.saving.set(true);
    const call: Observable<unknown> = this.mode() === 'add' ? this.plans.create(request) : this.plans.update(this.id()!, request);
    call.subscribe({
      next: created => {
        this.saving.set(false);
        this.form.markAsPristine();
        if (this.mode() === 'add') {
          this.toast.success(`${request.name} created`);
          this.router.navigate(['/plans', (created as { id: string }).id]);
          return;
        }
        this.toast.success('Plan updated');
        this.plans.getById(this.id()!).subscribe({ next: p => { this.plan.set(p); this.fill(p); this.applyMode(); } });
        this.mode.set('view');
        this.applyMode();
        this.openedFromView = false;
        this.location.go(`/plans/${this.id()}`);
      },
      error: err => {
        this.saving.set(false);
        applyServerErrors(this.form, err);
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return !this.leaving && this.mode() !== 'view' && this.form.dirty;
  }

  error(name: string): string | null {
    const c = this.form.get(name);
    if (!c || !c.touched || c.valid) return null;
    if (c.hasError('server')) return c.getError('server');
    switch (name) {
      case 'name':
        return c.hasError('required') ? 'Plan name is required.' : 'Plan name must be 2–60 characters.';
      case 'price':
        return c.hasError('required') ? 'Price is required.' : c.hasError('decimals') ? 'Use at most 2 decimals.' : 'Price cannot be negative.';
      case 'durationInMonths':
        return c.hasError('required') ? 'Duration is required.' : 'Duration must be a whole number from 1 to 36.';
      default:
        return 'Enter a whole number of 0 or more.';
    }
  }
}
