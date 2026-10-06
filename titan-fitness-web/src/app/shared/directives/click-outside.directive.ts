import { Directive, ElementRef, HostListener, output } from '@angular/core';

/** Emits when the user clicks anywhere outside the host element (closes the ⋮ row menus). */
@Directive({ selector: '[appClickOutside]' })
export class ClickOutsideDirective {
  readonly appClickOutside = output<MouseEvent>();

  constructor(private el: ElementRef<HTMLElement>) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.appClickOutside.emit(event);
    }
  }
}
