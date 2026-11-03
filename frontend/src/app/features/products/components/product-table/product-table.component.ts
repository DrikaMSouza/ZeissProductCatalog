import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Product } from '../../models/product.model';
import { StockMovement } from '../../models/product-request.model';
import { StockStatusChipComponent } from '../stock-status-chip/stock-status-chip.component';

export interface StockChangeIntent {
  product: Product;
  movement: StockMovement;
}

/** Presentational, sortable and paginated product list. Raises intents; never calls the API. */
@Component({
  selector: 'app-product-table',
  imports: [
    CurrencyPipe,
    DatePipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    StockStatusChipComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-table.component.html',
  styleUrl: './product-table.component.scss',
})
export class ProductTableComponent {
  readonly products = input.required<Product[]>();

  readonly edit = output<Product>();
  readonly remove = output<Product>();
  readonly changeStock = output<StockChangeIntent>();

  protected readonly columns = ['id', 'sku', 'name', 'categoryName', 'price', 'stockQuantity', 'updatedAtUtc', 'actions'];
  protected readonly dataSource = new MatTableDataSource<Product>([]);

  private readonly sort = viewChild.required(MatSort);
  private readonly paginator = viewChild.required(MatPaginator);

  constructor() {
    effect(() => {
      this.dataSource.sort = this.sort();
      this.dataSource.paginator = this.paginator();
    });

    effect(() => {
      this.dataSource.data = this.products();
    });
  }
}
