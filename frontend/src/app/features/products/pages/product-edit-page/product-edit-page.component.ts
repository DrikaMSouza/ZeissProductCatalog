import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { of } from 'rxjs';
import { ApiError } from '../../../../core/http/api-error';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { CategoriesApiService } from '../../../categories/data-access/categories-api.service';
import { ProductFormComponent } from '../../components/product-form/product-form.component';
import { SaveProductRequest } from '../../models/product-request.model';
import { ProductsStore } from '../../state/products.store';

/** Container page for both "new product" (no id) and "edit product" (id route parameter). */
@Component({
  selector: 'app-product-edit-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    ProductFormComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-edit-page.component.html',
  styleUrl: './product-edit-page.component.scss',
})
export class ProductEditPageComponent {
  private readonly store = inject(ProductsStore);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  /** Bound from the ":id" route parameter (withComponentInputBinding). */
  readonly id = input<string>();

  protected readonly productId = computed(() => (this.id() ? Number(this.id()) : null));
  protected readonly isEdit = computed(() => this.productId() !== null);

  protected readonly product = rxResource({
    params: () => this.productId(),
    stream: ({ params: id }) => (id === null ? of(null) : this.store.getById(id)),
  });

  protected readonly categories = toSignal(inject(CategoriesApiService).getAll(), {
    initialValue: [],
  });

  protected readonly saving = signal(false);
  protected readonly serverErrors = signal<Readonly<Record<string, string[]>>>({});

  protected loadErrorMessage(): string {
    const error = this.product.error();
    return error instanceof ApiError ? error.message : 'The product could not be loaded.';
  }

  protected save(request: SaveProductRequest): void {
    const id = this.productId();
    this.saving.set(true);
    this.serverErrors.set({});

    const save$ = id === null ? this.store.create(request) : this.store.update(id, request);

    save$.subscribe({
      next: (product) => {
        this.notifications.success(`"${product.name}" was saved (ID ${product.id}).`);
        void this.router.navigate(['/products']);
      },
      error: (error: ApiError) => {
        this.saving.set(false);
        this.serverErrors.set(error.fieldErrors);
        if (!error.isValidation) {
          this.notifications.error(error.message);
        }
      },
    });
  }

  protected cancel(): void {
    void this.router.navigate(['/products']);
  }
}
