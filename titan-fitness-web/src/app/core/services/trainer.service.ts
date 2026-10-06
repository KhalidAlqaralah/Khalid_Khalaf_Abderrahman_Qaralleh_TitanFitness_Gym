import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult, Trainer, TrainerCreated, TrainerListItem, TrainerQuery, TrainerRequest } from '../models/api.models';
import { listContext, pageLoad, toParams } from './http-options';

@Injectable({ providedIn: 'root' })
export class TrainerService {
  private readonly api = `${environment.apiUrl}/trainers`;

  constructor(private http: HttpClient) {}

  list(query: TrainerQuery, retry?: () => void): Observable<PagedResult<TrainerListItem>> {
    return this.http.get<PagedResult<TrainerListItem>>(this.api, {
      params: toParams(query),
      context: listContext(retry)
    });
  }

  specialties(): Observable<string[]> {
    return this.http.get<string[]>(`${this.api}/specialties`);
  }

  getById(id: string): Observable<Trainer> {
    return this.http.get<Trainer>(`${this.api}/${id}`, { context: pageLoad() });
  }

  create(request: TrainerRequest): Observable<TrainerCreated> {
    return this.http.post<TrainerCreated>(this.api, request);
  }

  update(id: string, request: TrainerRequest): Observable<void> {
    return this.http.put<void>(`${this.api}/${id}`, request);
  }
}
