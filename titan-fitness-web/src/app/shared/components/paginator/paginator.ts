import { Component, computed, input, output } from '@angular/core';

/** "Showing 1 to 10 of 240 entries" with page buttons. Takes total and page; emits pageChange. */
@Component({
  selector: 'app-paginator',
  template: `
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 px-3 py-3">
      <span class="text-muted-tf small" data-testid="paging-summary">
        @if (total() === 0) { No entries } @else { Showing {{ from() }} to {{ to() }} of {{ total() }} entries }
      </span>
      @if (pages() > 1) {
        <nav aria-label="Pages">
          <ul class="pagination pagination-sm m-0">
            <li class="page-item" [class.disabled]="page() === 1">
              <button class="page-link" type="button" (click)="go(page() - 1)" aria-label="Previous"><i class="bi bi-chevron-left"></i></button>
            </li>
            @for (p of pageNumbers(); track p) {
              <li class="page-item" [class.active]="p === page()">
                <button class="page-link" type="button" (click)="go(p)">{{ p }}</button>
              </li>
            }
            <li class="page-item" [class.disabled]="page() === pages()">
              <button class="page-link" type="button" (click)="go(page() + 1)" aria-label="Next"><i class="bi bi-chevron-right"></i></button>
            </li>
          </ul>
        </nav>
      }
    </div>
  `,
  styles: `.page-item.active .page-link { background: var(--tf-navy); border-color: var(--tf-navy); color: #fff; } .page-link { color: var(--tf-ink); min-width: 34px; text-align: center; }`
})
export class PaginatorComponent {
  readonly total = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input(10);
  readonly pageChange = output<number>();

  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly from = computed(() => (this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  readonly to = computed(() => Math.min(this.total(), this.page() * this.pageSize()));

  /** At most 5 page buttons around the current page. */
  readonly pageNumbers = computed(() => {
    const last = this.pages();
    const start = Math.max(1, Math.min(this.page() - 2, last - 4));
    const end = Math.min(last, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  go(page: number): void {
    if (page < 1 || page > this.pages() || page === this.page()) return;
    this.pageChange.emit(page);
  }
}
