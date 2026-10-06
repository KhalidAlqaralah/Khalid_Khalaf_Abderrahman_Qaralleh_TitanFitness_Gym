import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CheckIn, CheckInRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class CheckInService {
  private readonly api = `${environment.apiUrl}/check-ins`;

  /** The last saved check-in. The dashboard and the member directory react to it and refresh at once. */
  readonly lastCheckIn = signal<CheckIn | null>(null);

  constructor(private http: HttpClient) {}

  record(request: CheckInRequest): Observable<CheckIn> {
    return this.http.post<CheckIn>(this.api, request).pipe(tap(saved => this.lastCheckIn.set(saved)));
  }
}
