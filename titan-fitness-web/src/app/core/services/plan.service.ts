import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatedId, PagedResult, Plan, PlanFilterOptions, PlanQuery, PlanRequest } from '../models/api.models';
import { listContext, pageLoad, toParams } from './http-options';

@Injectable({ providedIn: 'root' })
export class PlanService {
  private readonly api = `${environment.apiUrl}/plans`;

  constructor(private http: HttpClient) {}

  list(query: PlanQuery, retry?: () => void): Observable<PagedResult<Plan>> {
    return this.http.get<PagedResult<Plan>>(this.api, {
      params: toParams(query),
      context: listContext(retry)
    });
  }

  filterOptions(): Observable<PlanFilterOptions> {
    return this.http.get<PlanFilterOptions>(`${this.api}/filter-options`);
  }

  getById(id: string): Observable<Plan> {
    return this.http.get<Plan>(`${this.api}/${id}`, { context: pageLoad() });
  }

  create(request: PlanRequest): Observable<CreatedId> {
    return this.http.post<CreatedId>(this.api, request);
  }

  update(id: string, request: PlanRequest): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}`, request);
  }
}
