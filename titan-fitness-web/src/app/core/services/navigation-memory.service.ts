import { Injectable, signal } from '@angular/core';

/** Remembers the last list URL of each area so "Back" returns to the same page, sort and filters. */
@Injectable({ providedIn: 'root' })
export class NavigationMemoryService {
  private readonly lists = signal<Record<string, string>>({});

  remember(area: 'members' | 'trainers' | 'plans' | 'classes', url: string): void {
    this.lists.update(all => ({ ...all, [area]: url }));
  }

  listUrl(area: 'members' | 'trainers' | 'plans' | 'classes'): string {
    return this.lists()[area] ?? `/${area}`;
  }
}
