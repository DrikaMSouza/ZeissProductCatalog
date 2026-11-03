/** Product as returned by the API. */
export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  categoryId: number;
  categoryName: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

/** Stock thresholds used to highlight products in the UI. */
export const LOW_STOCK_THRESHOLD = 10;

export type StockStatus = 'out-of-stock' | 'low' | 'in-stock';

export function stockStatusOf(stockQuantity: number): StockStatus {
  if (stockQuantity === 0) {
    return 'out-of-stock';
  }
  return stockQuantity <= LOW_STOCK_THRESHOLD ? 'low' : 'in-stock';
}
