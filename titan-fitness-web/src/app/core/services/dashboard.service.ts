import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActiveMembers, CheckInsToday, ClassSession } from '../models/api.models';
import { listContext, toParams } from './http-options';

/** One call per dashboard card: each card loads and refreshes on its own. */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) {}

  checkInsToday(branchId: string | null): Observable<CheckInsToday> {
    return this.http.get<CheckInsToday>(`${this.api}/check-ins-today`, { params: toParams({ branchId }) });
  }

  activeMembers(branchId: string | null): Observable<ActiveMembers> {
    return this.http.get<ActiveMembers>(`${this.api}/active-members`, { params: toParams({ branchId }) });
  }

  upcomingClasses(branchId: string | null, take: number, retry?: () => void): Observable<ClassSession[]> {
    return this.http.get<ClassSession[]>(`${this.api}/upcoming-classes`, {
      params: toParams({ branchId, take }),
      context: listContext(retry)
    });
  }
}
