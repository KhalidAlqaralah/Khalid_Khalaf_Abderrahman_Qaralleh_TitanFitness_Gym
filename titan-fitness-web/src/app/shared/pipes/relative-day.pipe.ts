import { formatDate } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { addDays, sameDay, today } from '../../core/models/dates';

/** Last Visit style: "Today, 08:30 AM", "Yesterday", "Oct 12, 2023"; empty values show "—". */
@Pipe({ name: 'relativeDay' })
export class RelativeDayPipe implements PipeTransform {
  transform(value: string | null | undefined, withTimeForPast = false): string {
    if (!value) return '—';
    const date = new Date(value);
    const time = formatDate(date, 'hh:mm a', 'en-US');

    if (sameDay(date, today())) return `Today, ${time}`;
    if (sameDay(date, addDays(today(), -1))) return withTimeForPast ? `Yesterday, ${time}` : 'Yesterday';
    return formatDate(date, 'MMM dd, yyyy', 'en-US');
  }
}
