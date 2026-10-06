import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ConfirmData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <div class="dialog-header"><h2>{{ data.title }}</h2></div>
    <div class="dialog-body"><p class="m-0">{{ data.message }}</p></div>
    <div class="dialog-footer">
      <button type="button" class="btn btn-outline-navy" (click)="ref.close(false)">{{ data.cancelText ?? 'Cancel' }}</button>
      <button type="button" class="btn" [class.btn-danger]="data.danger" [class.btn-navy]="!data.danger" (click)="ref.close(true)" cdkFocusInitial>
        {{ data.confirmText ?? 'OK' }}
      </button>
    </div>
  `
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmData>(MAT_DIALOG_DATA);
  readonly ref = inject<MatDialogRef<ConfirmDialogComponent, boolean>>(MatDialogRef);
}
