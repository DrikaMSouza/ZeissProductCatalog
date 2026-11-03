/** Payload accepted by POST /api/products and PUT /api/products/{id}. */
export interface SaveProductRequest {
  sku: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  categoryId: number;
}

export type StockMovement = 'add' | 'decrement';
