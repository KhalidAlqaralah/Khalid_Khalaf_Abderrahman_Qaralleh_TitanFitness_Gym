import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { Trainer, TrainerRequest } from '../../core/models/api.models';
import { HasUnsavedChanges } from '../../core/guards/unsaved-changes.guard';
import { applyServerErrors } from '../../core/services/api-errors';
import { BranchContextService } from '../../core/services/branch-context.service';
import { NavigationMemoryService } from '../../core/services/navigation-memory.service';
import { ToastService } from '../../core/services/toast.service';
import { TrainerService } from '../../core/services/trainer.service';
import { ConfirmService } from '../../shared/components/confirm-dialog/confirm.service';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';

type Mode = 'add' | 'view' | 'edit';

/**
 * One page for View (/trainers/:id), Add (/trainers/new) and Update (/trainers/:id/edit).
 * The mode comes from the route; Edit Trainer switches to Update in place and only changes the URL.
 */
@Component({
  selector: 'app-trainer-details',
  imports: [ReactiveFormsModule, AutofocusDirective],
  templateUrl: './trainer-details.html'
})
export class TrainerDetailsComponent implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly trainers = inject(TrainerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly memory = inject(NavigationMemoryService);
  readonly branches = inject(BranchContextService);

  /** Route parameter :id (absent for /trainers/new). */
  readonly id = input<string>();

  readonly mode = signal<Mode>(this.route.snapshot.data['mode'] as Mode);
  /** Where Update mode was opened from: in place from View, or straight from the directory row. */
  private openedFromView = false;
  private leaving = false;

  readonly trainer = signal<Trainer | null>(null);
  readonly loading = signal(this.mode() !== 'add');
  readonly notFound = signal(false);
  readonly saving = signal(false);

  readonly badge = computed(() => ({ add: 'Add mode', view: 'View mode', edit: 'Update mode' })[this.mode()]);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80), Validators.pattern(/.*\S.*\S.*/)]],
    specialty: ['', Validators.maxLength(100)],
    branchId: ['', Validators.required],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
    phone: ['', Validators.pattern(/^\+?[0-9\s\-()]{6,20}$/)],
    isActive: [true]
  });

  ngOnInit(): void {
    if (this.mode() === 'add') return;

    this.trainers.getById(this.id()!).subscribe({
      next: trainer => {
        this.trainer.set(trainer);
        this.fill(trainer);
        this.loading.set(false);
        this.applyMode();
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.notFound.set(err.status === 404);
      }
    });
  }

  private fill(t: Trainer): void {
    this.form.reset({ name: t.name, specialty: t.specialty ?? '', branchId: t.branchId, email: t.email, phone: t.phone ?? '', isActive: t.isActive });
  }

  private applyMode(): void {
    if (this.mode() === 'view') this.form.disable();
    else this.form.enable();
  }

  editInPlace(): void {
    this.openedFromView = true;
    this.mode.set('edit');
    this.applyMode();
    this.location.go(`/trainers/${this.id()}/edit`);
  }

  backToList(): void {
    this.router.navigateByUrl(this.memory.listUrl('trainers'));
  }

  cancel(): void {
    const proceed = () => {
      if (this.mode() === 'edit' && this.openedFromView) {
        this.fill(this.trainer()!);
        this.mode.set('view');
        this.applyMode();
        this.location.go(`/trainers/${this.id()}`);
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
    const request: TrainerRequest = {
      name: v.name.trim(),
      specialty: v.specialty.trim() || null,
      branchId: v.branchId,
      email: v.email.trim(),
      phone: v.phone.trim() || null,
      isActive: v.isActive
    };

    this.saving.set(true);
    const call: Observable<unknown> = this.mode() === 'add' ? this.trainers.create(request) : this.trainers.update(this.id()!, request);

    call.subscribe({
      next: created => {
        this.saving.set(false);
        if (this.mode() === 'add') {
          const id = (created as { id: string }).id;
          this.toast.success(`${request.name} added to the roster`);
          this.form.markAsPristine();
          this.router.navigate(['/trainers', id]);
          return;
        }
        this.toast.success('Trainer updated');
        this.trainers.getById(this.id()!).subscribe({ next: t => { this.trainer.set(t); this.fill(t); this.applyMode(); } });
        this.mode.set('view');
        this.form.markAsPristine();
        this.applyMode();
        this.openedFromView = false;
        this.location.go(`/trainers/${this.id()}`);
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
        return c.hasError('required') ? 'Trainer name is required.' : 'Trainer name must be 2–80 characters.';
      case 'specialty':
        return 'Specialty can be up to 100 characters.';
      case 'branchId':
        return 'Branch is required.';
      case 'email':
        return c.hasError('required') ? 'Email is required.' : 'Enter a valid email address.';
      case 'phone':
        return 'Enter a valid phone number, e.g. +1 (555) 000-0000.';
      default:
        return 'This value is not valid.';
    }
  }
}
