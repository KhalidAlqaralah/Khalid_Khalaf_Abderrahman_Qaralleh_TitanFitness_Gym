import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';
import { ApiProblem } from '../models/api.models';

export function problemOf(error: unknown): ApiProblem {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    return error.error as ApiProblem;
  }
  return {};
}

/** Field errors keyed by API field name ("fullName", "startDate"...). The empty key holds non-field errors. */
export function fieldErrors(error: unknown): Record<string, string[]> {
  return problemOf(error).errors ?? {};
}

export function hasFieldErrors(error: unknown): boolean {
  return Object.keys(fieldErrors(error)).some(key => key !== '');
}

/** The message to show for an error that is not about one field. */
export function messageOf(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  const problem = problemOf(error);
  return problem.detail ?? problem.title ?? fallback;
}

/**
 * Puts server field errors under the matching form controls (control names match API field names).
 * Returns true when at least one control received an error.
 */
export function applyServerErrors(form: FormGroup, error: unknown): boolean {
  let applied = false;
  for (const [field, messages] of Object.entries(fieldErrors(error))) {
    const control: AbstractControl | null = form.get(field);
    if (control && messages.length > 0) {
      control.setErrors({ ...(control.errors ?? {}), server: messages[0] });
      control.markAsTouched();
      applied = true;
    }
  }
  return applied;
}
