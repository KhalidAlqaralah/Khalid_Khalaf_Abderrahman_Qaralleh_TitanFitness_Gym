import { Component } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toast-stack" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="tf-toast" [class]="'tf-toast ' + toast.kind" role="status" data-testid="toast">
          <span class="flex-grow-1">{{ toast.message }}</span>
          @if (toast.action; as action) {
            <button type="button" (click)="action.run(); toasts.dismiss(toast.id)">{{ action.label }}</button>
          }
          <button type="button" class="close" aria-label="Dismiss" (click)="toasts.dismiss(toast.id)"><i class="bi bi-x-lg"></i></button>
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  constructor(public toasts: ToastService) {}
}
