import { CanDeactivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ConfirmService } from '../../shared/components/confirm-dialog/confirm.service';

/** A page with a form it may lose implements this. */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/** Asks "Discard changes?" before leaving a page with edited, unsaved fields. */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component): Observable<boolean> => {
  if (!component.hasUnsavedChanges()) return of(true);
  return inject(ConfirmService).discardChanges();
};
