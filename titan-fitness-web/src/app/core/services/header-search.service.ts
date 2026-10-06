import { Injectable, signal } from '@angular/core';

/**
 * The search box in the top bar. Each list screen sets the placeholder when it opens and reads the term;
 * the box is hidden on screens that have nothing to search.
 */
@Injectable({ providedIn: 'root' })
export class HeaderSearchService {
  readonly placeholder = signal<string | null>(null);
  readonly term = signal('');

  private timer: ReturnType<typeof setTimeout> | undefined;
  private owner = 0;

  /** Called by a screen when it opens. Returns a ticket to pass to disable(). */
  enable(placeholder: string, initialTerm = ''): number {
    clearTimeout(this.timer);
    this.placeholder.set(placeholder);
    this.term.set(initialTerm);
    return ++this.owner;
  }

  /** Called by the same screen when it closes; ignored if another screen has taken the box since. */
  disable(ticket: number): void {
    if (ticket !== this.owner) return;
    clearTimeout(this.timer);
    this.placeholder.set(null);
    this.term.set('');
  }

  /** Typing is debounced so a list reloads once the user pauses. */
  type(value: string): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.term.set(value.trim()), 300);
  }
}
