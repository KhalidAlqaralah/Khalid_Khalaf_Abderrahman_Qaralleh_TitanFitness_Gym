import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BookingResult, CapacityOverview, ClassScheduleQuery, ClassSession, ClassSessionRequest, CreatedId, SessionBooking, TrainerLookup
} from '../models/api.models';
import { listContext, toParams } from './http-options';

@Injectable({ providedIn: 'root' })
export class ClassService {
  private readonly api = `${environment.apiUrl}/class-sessions`;

  /** Bumped after every change so open schedules and the dashboard reload. */
  readonly changes = signal(0);

  constructor(private http: HttpClient) {}

  schedule(query: ClassScheduleQuery, retry?: () => void): Observable<ClassSession[]> {
    return this.http.get<ClassSession[]>(this.api, { params: toParams(query), context: listContext(retry) });
  }

  capacityOverview(query: ClassScheduleQuery): Observable<CapacityOverview> {
    return this.http.get<CapacityOverview>(`${this.api}/capacity-overview`, { params: toParams({ ...query, search: null }) });
  }

  getById(id: string): Observable<ClassSession> {
    return this.http.get<ClassSession>(`${this.api}/${id}`);
  }

  bookings(id: string): Observable<SessionBooking[]> {
    return this.http.get<SessionBooking[]>(`${this.api}/${id}/bookings`);
  }

  create(request: ClassSessionRequest): Observable<CreatedId> {
    return this.http.post<CreatedId>(this.api, request).pipe(tap(() => this.changed()));
  }

  update(id: string, request: ClassSessionRequest): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}`, request).pipe(tap(() => this.changed()));
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.api}/${id}/cancel`, {}).pipe(tap(() => this.changed()));
  }

  book(id: string, memberId: string | null, note: string | null): Observable<BookingResult> {
    return this.http.post<BookingResult>(`${this.api}/${id}/bookings`, { memberId, note }).pipe(tap(() => this.changed()));
  }

  /** Active trainers at a branch, for the instructor dropdown. */
  trainers(branchId: string | null): Observable<TrainerLookup[]> {
    return this.http.get<TrainerLookup[]>(`${environment.apiUrl}/trainers/lookup`, { params: toParams({ branchId, activeOnly: true }) });
  }

  private changed(): void {
    this.changes.update(n => n + 1);
  }
}
