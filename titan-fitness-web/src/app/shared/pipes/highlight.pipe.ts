import { Pipe, PipeTransform } from '@angular/core';

/** Wraps the parts of a text that match the search term in <mark>. Pure: recomputes only when text or term change. */
@Pipe({ name: 'highlight' })
export class HighlightPipe implements PipeTransform {
  transform(text: string | null | undefined, term: string | null | undefined): string {
    const safe = escapeHtml(text ?? '');
    const needle = (term ?? '').trim().replace(/^#/, '');
    if (!needle) return safe;

    const pattern = new RegExp(`(${escapeRegExp(escapeHtml(needle))})`, 'gi');
    return safe.replace(pattern, '<mark class="hl">$1</mark>');
  }
}

function escapeHtml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
