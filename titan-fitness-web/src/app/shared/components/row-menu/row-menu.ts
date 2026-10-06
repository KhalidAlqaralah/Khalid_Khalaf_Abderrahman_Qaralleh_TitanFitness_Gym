import { Component, input, output, signal } from '@angular/core';
import { MatTooltip } from '@angular/material/tooltip';
import { ClickOutsideDirective } from '../../directives/click-outside.directive';

export interface RowMenuItem {
  id: string;
  label: string;
  disabled?: boolean;
  /** Tooltip explaining why the action is disabled. */
  reason?: string | null;
}

/**
 * The ⋮ row menu. Closes on outside click (appClickOutside). Emits the chosen action id,
 * and the view / edit outputs for the two common actions.
 */
@Component({
  selector: 'app-row-menu',
  imports: [ClickOutsideDirective, MatTooltip],
  template: `
    <div class="row-menu" (appClickOutside)="open.set(false)">
      <button type="button" class="btn-icon" [attr.aria-label]="'Actions for ' + rowLabel()" aria-haspopup="menu"
              [attr.aria-expanded]="open()" (click)="toggle($event)">
        <i class="bi bi-three-dots-vertical"></i>
      </button>
      @if (open()) {
        <div class="row-menu-list" role="menu">
          @for (item of items(); track item.id) {
            <span [matTooltip]="item.disabled ? (item.reason ?? '') : ''" matTooltipPosition="left">
              <button type="button" role="menuitem" [disabled]="item.disabled" (click)="choose(item)">{{ item.label }}</button>
            </span>
          }
        </div>
      }
    </div>
  `
})
export class RowMenuComponent {
  readonly items = input.required<RowMenuItem[]>();
  readonly rowLabel = input('row');
  readonly selected = output<string>();
  readonly view = output<void>();
  readonly edit = output<void>();

  readonly open = signal(false);

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    this.open.update(o => !o);
  }

  choose(item: RowMenuItem): void {
    if (item.disabled) return;
    this.open.set(false);
    this.selected.emit(item.id);
    if (item.id === 'view') this.view.emit();
    if (item.id === 'edit') this.edit.emit();
  }
}
