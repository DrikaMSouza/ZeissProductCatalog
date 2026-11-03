import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, map, of, switchMap, tap } from 'rxjs';
import { ApiError } from '../../../core/http/api-error';
import { ProductsApiService } from '../data-access/products-api.service';
import { EMPTY_PRODUCT_FILTER, ProductFilter } from '../models/product-filter.model';
import { LOW_STOCK_THRESHOLD, Product } from '../models/product.model';
import { SaveProductRequest, StockMovement } from '../models/product-request.model';

interface ProductsState {
  products: Product[];
  filter: ProductFilter;
  loading: boolean;
  error: string | null;
  lastSyncedAt: Date | null;
}

const initialState: ProductsState = {
  products: [],
  filter: EMPTY_PRODUCT_FILTER,
  loading: false,
  error: null,
  lastSyncedAt: null,
};

/**
 * State of the products feature. Components read signals and call intention methods;
 * the store decides which endpoint to call and keeps the list in sync with the API.
 * Provided at feature-route level, so it lives as long as the products feature is used.
 */
@Injectable()
export class ProductsStore {
  private readonly api = inject(ProductsApiService);
  private readonly state = signal<ProductsState>(initialState);
  private readonly reload$ = new Subject<{ silent: boolean }>();

  readonly filter = computed(() => this.state().filter);
  readonly loading = computed(() => this.state().loading);
  readonly error = computed(() => this.state().error);
  readonly lastSyncedAt = computed(() => this.state().lastSyncedAt);

  /** Products from the API, narrowed by the filters that the endpoint used did not apply. */
  readonly products = computed(() => applyClientFilters(this.state().products, this.state().filter));

  readonly summary = computed(() => {
    const products = this.products();
    return {
      count: products.length,
      totalUnits: products.reduce((sum, p) => sum + p.stockQuantity, 0),
      lowStock: products.filter((p) => p.stockQuantity > 0 && p.stockQuantity <= LOW_STOCK_THRESHOLD)
        .length,
      outOfStock: products.filter((p) => p.stockQuantity === 0).length,
    };
  });

  constructor() {
    this.reload$
      .pipe(
        tap(({ silent }) => !silent && this.patch({ loading: true, error: null })),
        // switchMap drops responses of outdated requests when the filter changes quickly.
        switchMap(() =>
          this.fetch(this.state().filter).pipe(
            map((products) => ({ products, error: null })),
            catchError((error: unknown) => of({ products: null, error: messageOf(error) })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ products, error }) =>
        this.patch(
          products
            ? { products, loading: false, error: null, lastSyncedAt: new Date() }
            : { loading: false, error },
        ),
      );
  }

  load(): void {
    this.reload$.next({ silent: false });
  }

  /** Re-reads the current list without showing the loading state (used for auto refresh). */
  refreshSilently(): void {
    this.reload$.next({ silent: true });
  }

  setFilter(filter: ProductFilter): void {
    this.patch({ filter });
    this.load();
  }

  resetFilter(): void {
    this.setFilter(EMPTY_PRODUCT_FILTER);
  }

  getById(id: number): Observable<Product> {
    return this.api.getById(id);
  }

  create(request: SaveProductRequest): Observable<Product> {
    return this.api.create(request).pipe(tap((product) => this.upsertAndResync(product)));
  }

  update(id: number, request: SaveProductRequest): Observable<Product> {
    return this.api.update(id, request).pipe(tap((product) => this.upsertAndResync(product)));
  }

  delete(id: number): Observable<void> {
    return this.api.delete(id).pipe(
      tap(() => this.patch({ products: this.state().products.filter((p) => p.id !== id) })),
    );
  }

  changeStock(id: number, movement: StockMovement, quantity: number): Observable<Product> {
    return this.api
      .changeStock(id, movement, quantity)
      .pipe(tap((product) => this.upsertAndResync(product)));
  }

  /** Shows the server's version immediately, then re-syncs so server-side filters stay correct. */
  private upsertAndResync(product: Product): void {
    const products = this.state().products;
    const exists = products.some((p) => p.id === product.id);
    this.patch({
      products: exists
        ? products.map((p) => (p.id === product.id ? product : p))
        : [...products, product],
    });
    this.refreshSilently();
  }

  private fetch(filter: ProductFilter): Observable<Product[]> {
    const name = filter.name.trim();
    if (name) {
      return this.api.searchByName(name);
    }
    if (filter.minStock !== null || filter.maxStock !== null) {
      return this.api.getByStockLevel(filter.minStock, filter.maxStock);
    }
    return this.api.getAll();
  }

  private patch(partial: Partial<ProductsState>): void {
    this.state.update((state) => ({ ...state, ...partial }));
  }
}

/** Applies category and (when a name search was used) the stock range on the client. */
export function applyClientFilters(products: Product[], filter: ProductFilter): Product[] {
  return products.filter(
    (p) =>
      (filter.categoryId === null || p.categoryId === filter.categoryId) &&
      (filter.minStock === null || p.stockQuantity >= filter.minStock) &&
      (filter.maxStock === null || p.stockQuantity <= filter.maxStock),
  );
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Unexpected error while loading products.';
}
