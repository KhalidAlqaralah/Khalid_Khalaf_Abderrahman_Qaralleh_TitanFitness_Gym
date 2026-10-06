import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Branch } from '../models/api.models';

const STORAGE_KEY = 'tf.branch';

/**
 * The header branch, shared by every screen: the dashboard is scoped to it, new members and classes
 * default to it, and check-ins are recorded at it.
 */
@Injectable({ providedIn: 'root' })
export class BranchContextService {
  readonly branches = signal<Branch[]>([]);
  readonly currentBranchId = signal<string | null>(this.restore());
  readonly currentBranch = computed(
    () => this.branches().find(b => b.id === this.currentBranchId()) ?? this.branches()[0] ?? null
  );
  readonly loaded = signal(false);

  constructor(private http: HttpClient) {
    effect(() => {
      const id = this.currentBranch()?.id;
      if (!id) return;
      try {
        localStorage.setItem(STORAGE_KEY, id);
      } catch {
        /* per-viewer convenience only */
      }
    });
  }

  load(): void {
    if (this.loaded()) return;
    this.http.get<Branch[]>(`${environment.apiUrl}/branches`).subscribe({
      next: branches => {
        this.branches.set(branches);
        this.loaded.set(true);
        if (!branches.some(b => b.id === this.currentBranchId())) {
          this.currentBranchId.set(branches.find(b => b.name === 'Downtown')?.id ?? branches[0]?.id ?? null);
        }
      }
    });
  }

  select(branchId: string): void {
    this.currentBranchId.set(branchId);
  }

  branchName(id: string | null | undefined): string {
    return this.branches().find(b => b.id === id)?.name ?? '';
  }

  reset(): void {
    this.loaded.set(false);
  }

  private restore(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
