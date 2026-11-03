import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api.config';
import { Product } from '../models/product.model';
import { SaveProductRequest, StockMovement } from '../models/product-request.model';

/** HTTP access to the products endpoints. Contains no state and no UI logic. */
@Injectable({ providedIn: 'root' })
export class ProductsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/products`;

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.baseUrl);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  searchByName(name: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/search`, { params: { name } });
  }

  getByStockLevel(min: number | null, max: number | null): Observable<Product[]> {
    let params = new HttpParams();
    if (min !== null) {
      params = params.set('min', min);
    }
    if (max !== null) {
      params = params.set('max', max);
    }
    return this.http.get<Product[]>(`${this.baseUrl}/stock-level`, { params });
  }

  create(request: SaveProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request);
  }

  update(id: number, request: SaveProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  changeStock(id: number, movement: StockMovement, quantity: number): Observable<Product> {
    const action = movement === 'add' ? 'add-to-stock' : 'decrement-stock';
    return this.http.post<Product>(`${this.baseUrl}/${id}/${action}/${quantity}`, null);
  }
}
