import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api.config';
import { Category } from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoriesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/categories`;

  /** Categories are reference data, so the response is cached for the lifetime of the app. */
  private readonly categories$ = this.http.get<Category[]>(this.baseUrl).pipe(shareReplay(1));

  getAll(): Observable<Category[]> {
    return this.categories$;
  }
}
