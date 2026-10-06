import { Component, computed, input } from '@angular/core';

/** Freezes Used / Guest Passes: consumption against the terms the membership was sold under. */
@Component({
  selector: 'app-usage-card',
  template: `
    <div class="tf-card p-3 h-100">
      <div class="d-flex justify-content-between align-items-center">
        <strong><i class="bi me-2" [class]="'bi me-2 ' + icon()"></i>{{ label() }}</strong>
        <span class="text-muted-tf" [attr.data-testid]="testId()">{{ used() }} / {{ max() }}</span>
      </div>
      <div class="fill-bar my-3"><span [style.width.%]="percent()"></span></div>
      <div class="text-end small text-muted-tf">{{ remainingText() }}</div>
    </div>
  `
})
export class UsageCardComponent {
  readonly label = input.required<string>();
  readonly icon = input('bi-snow');
  readonly used = input.required<number>();
  readonly max = input.required<number>();
  readonly unit = input('freeze');
  readonly units = input('freezes');
  readonly testId = input<string | null>(null);

  readonly percent = computed(() => (this.max() === 0 ? 0 : Math.min(100, (this.used() / this.max()) * 100)));
  readonly remainingText = computed(() => {
    const left = Math.max(0, this.max() - this.used());
    if (this.max() === 0) return `No ${this.units()} on this plan`;
    return `${left} ${left === 1 ? this.unit() : this.units()} remaining`;
  });
}
