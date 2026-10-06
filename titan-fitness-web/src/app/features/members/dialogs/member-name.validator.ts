import { AbstractControl, ValidationErrors, Validators } from '@angular/forms';

/** Member name: required, 2–80 characters after trimming, letters, spaces, hyphens and apostrophes only. */
export function memberNameValidators() {
  return [
    Validators.required,
    (control: AbstractControl): ValidationErrors | null => {
      const value = ((control.value as string) ?? '').trim().replace(/\s+/g, ' ');
      if (!value) return { required: true };
      if (value.length < 2 || value.length > 80) return { length: true };
      return /^[\p{L}][\p{L}\s'-]*$/u.test(value) ? null : { pattern: true };
    }
  ];
}

export function memberNameError(control: AbstractControl): string | null {
  if (!control.touched || control.valid) return null;
  if (control.hasError('server')) return control.getError('server');
  if (control.hasError('required')) return 'Member name is required.';
  if (control.hasError('length')) return 'Member name must be 2–80 characters.';
  if (control.hasError('pattern')) return 'Use letters, spaces, hyphens and apostrophes only.';
  return null;
}
