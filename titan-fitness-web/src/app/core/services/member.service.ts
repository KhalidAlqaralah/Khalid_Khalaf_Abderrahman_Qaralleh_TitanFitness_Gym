import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Member, MemberActivity, MemberCreated, MemberListItem, MemberQuery, Membership, PagedResult } from '../models/api.models';
import { listContext, pageLoad, toParams } from './http-options';

@Injectable({ providedIn: 'root' })
export class MemberService {
  private readonly api = `${environment.apiUrl}/members`;

  constructor(private http: HttpClient) {}

  list(query: MemberQuery, retry?: () => void): Observable<PagedResult<MemberListItem>> {
    return this.http.get<PagedResult<MemberListItem>>(this.api, {
      params: toParams(query),
      context: listContext(retry)
    });
  }

  /** Quick search for the member pickers (check-in, booking). */
  search(term: string): Observable<PagedResult<MemberListItem>> {
    return this.http.get<PagedResult<MemberListItem>>(this.api, { params: toParams({ search: term, pageSize: 8, sortBy: 'name' }) });
  }

  getById(id: string): Observable<Member> {
    return this.http.get<Member>(`${this.api}/${id}`, { context: pageLoad() });
  }

  /** 404 when the member holds no current membership. */
  currentMembership(id: string): Observable<Membership> {
    return this.http.get<Membership>(`${this.api}/${id}/current-membership`, { context: pageLoad() });
  }

  activity(id: string, take = 7): Observable<MemberActivity[]> {
    return this.http.get<MemberActivity[]>(`${this.api}/${id}/activity`, { params: toParams({ take }) });
  }

  create(fullName: string, homeBranchId: string | null): Observable<MemberCreated> {
    return this.http.post<MemberCreated>(this.api, { fullName, homeBranchId });
  }

  update(id: string, fullName: string, homeBranchId: string | null): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}`, { fullName, homeBranchId });
  }
}
