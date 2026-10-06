import { Injectable, signal } from '@angular/core';

export type ToastKind = 'info' | 'success' | 'error' | 'warning';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
  action?: { label: string; run: () => void };
}

/** App-wide toast messages, held in a signal the toast container renders. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  show(message: string, kind: ToastKind = 'info', action?: Toast['action'], durationMs = 5000): void {
    const toast: Toast = { id: this.nextId++, kind, message, action };
    this.toasts.update(list => [...list.filter(t => t.message !== message), toast]);
    setTimeout(() => this.dismiss(toast.id), action ? durationMs * 2 : durationMs);
  }

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string, action?: Toast['action']): void {
    this.show(message, 'error', action);
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
