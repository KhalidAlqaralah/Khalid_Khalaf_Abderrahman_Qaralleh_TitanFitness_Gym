import { AfterViewInit, Directive, ElementRef, input } from '@angular/core';

/**
 * Focuses the first field when a dialog or form opens.
 * Put it on a field (`<input appAutofocus>`) or on a container: the first enabled field inside is focused.
 * It never moves the focus away from a field the user has already clicked or started typing in.
 */
@Directive({ selector: '[appAutofocus]' })
export class AutofocusDirective implements AfterViewInit {
  readonly appAutofocus = input<boolean | ''>(true);

  constructor(private el: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    if (this.appAutofocus() === false) return;

    // Wait for the dialog's open animation and focus trap to settle.
    setTimeout(() => {
      const active = document.activeElement as HTMLElement | null;
      if (active && active.matches('input, select, textarea, [contenteditable=true]')) return;

      const host = this.el.nativeElement;
      const target = host.matches('input, select, textarea, button')
        ? host
        : host.querySelector<HTMLElement>('input:not([disabled]):not([type=hidden]), select:not([disabled]), textarea:not([disabled])');
      target?.focus();
    }, 120);
  }
}
