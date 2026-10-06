import { Pipe, PipeTransform } from '@angular/core';

/** (60, 3) → "60 days / 3 freezes", (15, 1) → "15 days / 1 freeze", 0 → "None". */
@Pipe({ name: 'freezeAllowance' })
export class FreezeAllowancePipe implements PipeTransform {
  transform(days: number | null | undefined, freezes: number | null | undefined): string {
    if (!days || !freezes) return 'None';
    return `${days} ${days === 1 ? 'day' : 'days'} / ${freezes} ${freezes === 1 ? 'freeze' : 'freezes'}`;
  }
}
