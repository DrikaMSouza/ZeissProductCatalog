import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { EMPTY, filter, interval, switchMap } from 'rxjs';
import { ApiError } from '../../../../core/http/api-error';
import { NotificationService } from '../../../../core/notifications/notification.service';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { CategoriesApiService } from '../../../categories/data-access/categories-api.service';
import { ProductFiltersComponent } from '../../components/product-filters/product-filters.component';
import {
  ProductTableComponent,
  StockChangeIntent,
} from '../../components/product-table/product-table.component';
import {
  StockAdjustDialogComponent,
  StockAdjustDialogData,
} from '../../components/stock-adjust-dialog/stock-adjust-dialog.component';
import { Product } from '../../models/product.model';
import { ProductsStore } from '../../state/products.store';

const AUTO_REFRESH_INTERVAL_MS = 15_000;

/** Container page: wires the store and dialogs to the presentational components. */
@Component({
  selector: 'app-product-list-page',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    ProductFiltersComponent,
    ProductTableComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-list-page.component.html',
  styleUrl: './product-list-page.component.scss',
})
export class ProductListPageComponent {
  protected readonly store = inject(ProductsStore);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly categories = toSignal(inject(CategoriesApiService).getAll(), {
    initialValue: [],
  });
  protected readonly autoRefresh = signal(true);

  constructor() {
    this.store.load();

    // Keeps the list in sync with changes made by other users or clients of the API.
    interval(AUTO_REFRESH_INTERVAL_MS)
      .pipe(
        filter(() => this.autoRefresh() && document.visibilityState === 'visible'),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.store.refreshSilently());
  }

  protected edit(product: Product): void {
    void this.router.navigate(['/products', product.id, 'edit']);
  }

  protected changeStock({ product, movement }: StockChangeIntent): void {
    this.dialog
      .open<StockAdjustDialogComponent, StockAdjustDialogData, number>(StockAdjustDialogComponent, {
        data: { product, movement },
        width: '420px',
      })
      .afterClosed()
      .pipe(
        switchMap((quantity) =>
          quantity ? this.store.changeStock(product.id, movement, quantity) : EMPTY,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) =>
          this.notifications.success(`${updated.name}: stock is now ${updated.stockQuantity}.`),
        error: (error: ApiError) => {
          this.notifications.error(error.message);
          this.store.refreshSilently();
        },
      });
  }

  protected remove(product: Product): void {
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: 'Delete product',
          message: `Delete "${product.name}" (${product.sku})? This cannot be undone.`,
          confirmLabel: 'Delete',
        },
      })
      .afterClosed()
      .pipe(
        switchMap((confirmed) => (confirmed ? this.store.delete(product.id) : EMPTY)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.notifications.success(`"${product.name}" was deleted.`),
        error: (error: ApiError) => this.notifications.error(error.message),
      });
  }
}
