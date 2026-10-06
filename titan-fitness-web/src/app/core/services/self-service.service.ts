import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BookingResult, ClassSession, Eligibility } from '../models/api.models';
import { pageLoad, toParams } from './http-options';

/** The member's own booking API (Book Session — Member View). */
@Injectable({ providedIn: 'root' })
export class SelfServiceService {
  private readonly api = `${environment.apiUrl}/me`;

  constructor(private http: HttpClient) {}

  eligibility(sessionId?: string): Observable<Eligibility> {
    return this.http.get<Eligibility>(`${this.api}/eligibility`, { params: toParams({ sessionId }) });
  }

  classes(date: string, days = 7): Observable<ClassSession[]> {
    return this.http.get<ClassSession[]>(`${this.api}/classes`, { params: toParams({ date, days }) });
  }

  classById(id: string): Observable<ClassSession> {
    return this.http.get<ClassSession>(`${this.api}/classes/${id}`, { context: pageLoad() });
  }

  book(sessionId: string, note: string | null): Observable<BookingResult> {
    return this.http.post<BookingResult>(`${this.api}/bookings`, { sessionId, note });
  }
}
