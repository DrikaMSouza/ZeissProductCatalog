import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiError } from '../../../core/http/api-error';
import { ProductsApiService } from '../data-access/products-api.service';
import { EMPTY_PRODUCT_FILTER } from '../models/product-filter.model';
import { Product } from '../models/product.model';
import { ProductsStore, applyClientFilters } from './products.store';

function product(overrides: Partial<Product> = {}): Product {
  return {
    id: 100000,
    sku: 'SKU',
    name: 'Lens',
    description: null,
    price: 10,
    stockQuantity: 5,
    categoryId: 1,
    categoryName: 'Spectacle Lenses',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

describe('ProductsStore', () => {
  let api: {
    getAll: ReturnType<typeof vi.fn>;
    searchByName: ReturnType<typeof vi.fn>;
    getByStockLevel: ReturnType<typeof vi.fn>;
    changeStock: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };
  let store: ProductsStore;

  beforeEach(() => {
    api = {
      getAll: vi.fn(() => of([product({ id: 1 }), product({ id: 2, stockQuantity: 0 })])),
      searchByName: vi.fn(() => of([product({ id: 3, name: 'Progressive Lens' })])),
      getByStockLevel: vi.fn(() => of([product({ id: 2, stockQuantity: 0 })])),
      changeStock: vi.fn(),
      delete: vi.fn(() => of(undefined)),
    };

    TestBed.configureTestingModule({
      providers: [ProductsStore, { provide: ProductsApiService, useValue: api }],
    });
    store = TestBed.inject(ProductsStore);
  });

  it('loads every product without filters', () => {
    store.load();

    expect(api.getAll).toHaveBeenCalled();
    expect(store.products().map((p) => p.id)).toEqual([1, 2]);
    expect(store.loading()).toBe(false);
    expect(store.lastSyncedAt()).not.toBeNull();
  });

  it('uses the search endpoint when a name is given', () => {
    store.setFilter({ ...EMPTY_PRODUCT_FILTER, name: '  lens ' });

    expect(api.searchByName).toHaveBeenCalledWith('lens');
    expect(store.products()[0].name).toBe('Progressive Lens');
  });

  it('uses the stock-level endpoint when only a stock range is given', () => {
    store.setFilter({ ...EMPTY_PRODUCT_FILTER, minStock: 0, maxStock: 0 });

    expect(api.getByStockLevel).toHaveBeenCalledWith(0, 0);
    expect(api.getAll).not.toHaveBeenCalled();
  });

  it('exposes load errors as a message', () => {
    api.getAll.mockReturnValue(throwError(() => new ApiError(0, 'The server could not be reached.')));

    store.load();

    expect(store.error()).toBe('The server could not be reached.');
    expect(store.loading()).toBe(false);
  });

  it('replaces the product with the server response after a stock change', async () => {
    store.load();
    api.changeStock.mockReturnValue(of(product({ id: 1, stockQuantity: 2 })));
    api.getAll.mockReturnValue(of([product({ id: 1, stockQuantity: 2 }), product({ id: 2, stockQuantity: 0 })]));

    await firstValueFrom(store.changeStock(1, 'decrement', 3));

    expect(api.changeStock).toHaveBeenCalledWith(1, 'decrement', 3);
    expect(store.products().find((p) => p.id === 1)?.stockQuantity).toBe(2);
  });

  it('removes a deleted product from the list', async () => {
    store.load();

    await firstValueFrom(store.delete(1));

    expect(store.products().map((p) => p.id)).toEqual([2]);
  });

  it('summarises stock levels', () => {
    store.load();

    expect(store.summary()).toEqual({ count: 2, totalUnits: 5, lowStock: 1, outOfStock: 1 });
  });
});

describe('applyClientFilters', () => {
  const products = [
    product({ id: 1, categoryId: 1, stockQuantity: 0 }),
    product({ id: 2, categoryId: 2, stockQuantity: 20 }),
  ];

  it('filters by category and inclusive stock range', () => {
    expect(applyClientFilters(products, { ...EMPTY_PRODUCT_FILTER, categoryId: 2 }).map((p) => p.id)).toEqual([2]);
    expect(applyClientFilters(products, { ...EMPTY_PRODUCT_FILTER, minStock: 0, maxStock: 0 }).map((p) => p.id)).toEqual([1]);
    expect(applyClientFilters(products, EMPTY_PRODUCT_FILTER)).toHaveLength(2);
  });
});
