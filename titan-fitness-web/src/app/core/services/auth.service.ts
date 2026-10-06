import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Role, Session } from '../models/api.models';

const STORAGE_KEY = 'tf.session';

/** The signed-in user. The session survives a page refresh (localStorage) until it expires or the API rejects it. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly session = signal<Session | null>(this.restore());
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly role = computed<Role | null>(() => this.session()?.role ?? null);
  readonly isManager = computed(() => this.role() === 'Manager');
  readonly isStaff = computed(() => this.role() === 'Manager' || this.role() === 'FrontDesk');

  constructor(private http: HttpClient) {}

  login(userName: string, password: string): Observable<Session> {
    return this.http
      .post<Session>(`${environment.apiUrl}/auth/login`, { userName, password })
      .pipe(tap(session => this.store(session)));
  }

  logout(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* storage unavailable: nothing to clear */
    }
  }

  token(): string | null {
    return this.session()?.token ?? null;
  }

  /** Where a role lands after signing in. */
  homeUrl(): string {
    return this.role() === 'Member' ? '/member/classes' : '/dashboard';
  }

  private store(session: Session): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      /* storage unavailable: the session lives until the tab closes */
    }
  }

  private restore(): Session | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const session = JSON.parse(raw) as Session;
      return new Date(session.expiresAtUtc).getTime() > Date.now() ? session : null;
    } catch {
      return null;
    }
  }
}
