import { HttpContext, HttpContextToken, HttpParams } from '@angular/common/http';

/** Marks a request as the main load of a page: 404 shows the page's "not found" state, 403 shows Access denied. */
export const PAGE_LOAD = new HttpContextToken<boolean>(() => false);

/** A list load that can be retried from the error toast. */
export const RETRY = new HttpContextToken<(() => void) | null>(() => null);

/** Skips the global error toast (the caller shows its own message). */
export const SILENT = new HttpContextToken<boolean>(() => false);

export function pageLoad(): HttpContext {
  return new HttpContext().set(PAGE_LOAD, true);
}

/** A list that is the main content of its page: page-load error handling plus a Retry action on server errors. */
export function listContext(retry?: () => void): HttpContext {
  return new HttpContext().set(PAGE_LOAD, true).set(RETRY, retry ?? null);
}

/** Builds query parameters, skipping empty values and repeating keys for arrays (?statuses=A&statuses=B). */
export function toParams(query: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined || value === '') continue;
    if (Array.isArray(value)) {
      for (const item of value) params = params.append(key, String(item));
    } else {
      params = params.set(key, String(value));
    }
  }
  return params;
}
