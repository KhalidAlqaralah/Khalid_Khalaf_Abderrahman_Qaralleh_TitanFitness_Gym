import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FreezeApplied, FreezeRequest, Plan } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class MembershipService {
  private readonly api = `${environment.apiUrl}/memberships`;

  constructor(private http: HttpClient) {}

  freeze(membershipId: string, request: FreezeRequest): Observable<FreezeApplied> {
    return this.http.post<FreezeApplied>(`${this.api}/${membershipId}/freezes`, request);
  }

  purchase(memberId: string, planId: string | null, startDate: string | null): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.api, { memberId, planId, startDate });
  }

  renew(membershipId: string, planId: string | null): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.api}/${membershipId}/renewals`, { planId });
  }

  /** Published plans, for the Sell Plan dialog. */
  sellablePlans(): Observable<Plan[]> {
    return this.http.get<Plan[]>(`${environment.apiUrl}/plans/lookup`);
  }
}
