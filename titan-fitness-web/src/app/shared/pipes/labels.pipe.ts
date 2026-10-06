import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Record<string, string> = {
  HomeBranchOnly: 'Home branch only',
  AllBranches: 'All branches',
  ExtendedTravel: 'Extended Travel',
  InProgress: 'In Progress',
  NoShow: 'No show',
  None: 'No plan'
};

/** Turns API enum values into the words shown on screen. */
@Pipe({ name: 'label' })
export class LabelPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';
    return LABELS[value] ?? value.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}
