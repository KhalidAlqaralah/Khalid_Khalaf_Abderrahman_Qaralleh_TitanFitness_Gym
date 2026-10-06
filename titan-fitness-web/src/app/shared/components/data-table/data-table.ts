import { NgTemplateOutlet } from '@angular/common';
import { Component, TemplateRef, contentChild, input, output } from '@angular/core';
import { SortDirection } from '../../../core/models/api.models';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  align?: 'start' | 'end';
}

export interface SortState {
  sortBy: string;
  sortDirection: SortDirection;
}

/**
 * A sortable table shell. The parent passes the columns and a row template (`<ng-template #row let-item>`);
 * the table emits sortChange when a sortable header is clicked. Loading, error and empty states are built in.
 */
@Component({
  selector: 'app-data-table',
  imports: [NgTemplateOutlet],
  template: `
    <div class="table-responsive">
      <table class="tf-table">
        <thead>
          <tr>
            @for (col of columns(); track col.key) {
              <th [class.sortable]="col.sortable" [class.text-end]="col.align === 'end'"
                  [attr.aria-sort]="sort().sortBy === col.key ? (sort().sortDirection === 'Asc' ? 'ascending' : 'descending') : null"
                  (click)="col.sortable && toggleSort(col.key)">
                {{ col.label }}
                @if (col.sortable) {
                  <i class="bi" [class]="sort().sortBy === col.key ? (sort().sortDirection === 'Asc' ? 'bi bi-caret-up-fill' : 'bi bi-caret-down-fill') : 'bi bi-chevron-expand'"></i>
                }
              </th>
            }
          </tr>
        </thead>
        <tbody>
          @if (loading()) {
            <tr><td [attr.colspan]="columns().length"><div class="state-box"><span class="spinner-border spinner-border-sm me-2"></span>Loading…</div></td></tr>
          } @else if (error()) {
            <tr><td [attr.colspan]="columns().length">
              <div class="state-box"><i class="bi bi-exclamation-triangle"></i>{{ error() }}
                <div class="mt-2"><button type="button" class="btn btn-outline-navy btn-sm" (click)="retry.emit()">Retry</button></div>
              </div>
            </td></tr>
          } @else {
            @for (item of rows(); track trackKey(item)) {
              <ng-container [ngTemplateOutlet]="rowTemplate()" [ngTemplateOutletContext]="{ $implicit: item }" />
            } @empty {
              <tr><td [attr.colspan]="columns().length"><div class="state-box"><i class="bi bi-inbox"></i>{{ emptyText() }}</div></td></tr>
            }
          }
        </tbody>
      </table>
    </div>
  `
})
export class DataTableComponent<T extends { id: string }> {
  readonly columns = input.required<TableColumn[]>();
  readonly rows = input.required<T[]>();
  readonly sort = input.required<SortState>();
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly emptyText = input('Nothing matches the current search and filters.');

  readonly sortChange = output<SortState>();
  readonly retry = output<void>();

  readonly rowTemplate = contentChild.required<TemplateRef<{ $implicit: T }>>('row');

  trackKey(item: T): string {
    return item.id;
  }

  toggleSort(key: string): void {
    const current = this.sort();
    const direction: SortDirection = current.sortBy === key && current.sortDirection === 'Asc' ? 'Desc' : 'Asc';
    this.sortChange.emit({ sortBy: key, sortDirection: direction });
  }
}
