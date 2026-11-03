import { LOW_STOCK_THRESHOLD } from './product.model';

export interface ProductFilter {
  /** Partial, case-insensitive name match (server side, /products/search). */
  name: string;
  /** Inclusive stock range (server side, /products/stock-level). */
  minStock: number | null;
  maxStock: number | null;
  /** Narrows the loaded list on the client. */
  categoryId: number | null;
}

export const EMPTY_PRODUCT_FILTER: ProductFilter = {
  name: '',
  minStock: null,
  maxStock: null,
  categoryId: null,
};

/** Shortcut for products that need replenishing. */
export const LOW_STOCK_FILTER: ProductFilter = {
  ...EMPTY_PRODUCT_FILTER,
  minStock: 0,
  maxStock: LOW_STOCK_THRESHOLD,
};
