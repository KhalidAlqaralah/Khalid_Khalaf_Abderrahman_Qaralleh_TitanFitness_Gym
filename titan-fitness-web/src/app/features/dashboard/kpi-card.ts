import { Component, input } from '@angular/core';

/** One KPI tile: label + icon, the big number, and a line underneath. */
@Component({
  selector: 'app-kpi-card',
  template: `
    <div class="tf-card kpi h-100">
      <div class="kpi-head"><span>{{ label() }}</span><i class="bi" [class]="'bi ' + icon()"></i></div>
      @if (loading()) {
        <div class="placeholder-glow"><span class="placeholder col-4 kpi-ph"></span></div>
      } @else {
        <div class="kpi-value" [attr.data-testid]="testId()">{{ value() }}</div>
      }
      <div class="kpi-foot"><ng-content /></div>
    </div>
  `,
  styles: `
    .kpi { padding: 1.1rem 1.25rem; }
    .kpi-head { display: flex; justify-content: space-between; color: var(--tf-muted); font-size: .8rem; letter-spacing: .08em; text-transform: uppercase; font-weight: 700; padding-bottom: .75rem; border-bottom: 1px solid var(--tf-line); }
    .kpi-head .bi { color: var(--tf-navy); font-size: 1rem; }
    .kpi-value { font-size: 3rem; font-weight: 700; line-height: 1.2; margin: .75rem 0 .25rem; }
    .kpi-ph { height: 3rem; margin: .75rem 0 .25rem; display: inline-block; }
    .kpi-foot { color: var(--tf-muted); font-size: .9rem; }
  `
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly icon = input('bi-graph-up');
  readonly value = input<string | number | null>(null);
  readonly loading = input(false);
  readonly testId = input<string | null>(null);
}
