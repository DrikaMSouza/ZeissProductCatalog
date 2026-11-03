import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Category } from '../../../categories/models/category.model';
import { Product } from '../../models/product.model';
import { SaveProductRequest } from '../../models/product-request.model';

/** Limits mirror the API validation rules, so most mistakes are caught before a round-trip. */
export const PRODUCT_LIMITS = {
  skuMaxLength: 32,
  nameMaxLength: 150,
  descriptionMaxLength: 1000,
  maxPrice: 1_000_000,
} as const;

/** Presentational create/edit form. Shows client-side and server-side (field) validation errors. */
@Component({
  selector: 'app-product-form',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent {
  readonly product = input<Product | null>(null);
  readonly categories = input.required<Category[]>();
  readonly saving = input(false);
  /** Field errors returned by the API (ProblemDetails "errors"), keyed by camelCase property name. */
  readonly serverErrors = input<Readonly<Record<string, string[]>>>({});

  readonly save = output<SaveProductRequest>();
  readonly cancelled = output<void>();

  protected readonly limits = PRODUCT_LIMITS;

  protected readonly form = inject(FormBuilder).group({
    sku: [
      '',
      [
        Validators.required,
        Validators.maxLength(PRODUCT_LIMITS.skuMaxLength),
        Validators.pattern(/^[A-Za-z0-9-]+$/),
      ],
    ],
    name: ['', [Validators.required, Validators.maxLength(PRODUCT_LIMITS.nameMaxLength)]],
    description: ['', Validators.maxLength(PRODUCT_LIMITS.descriptionMaxLength)],
    price: [
      null as number | null,
      [
        Validators.required,
        Validators.min(0.01),
        Validators.max(PRODUCT_LIMITS.maxPrice),
        Validators.pattern(/^\d+(\.\d{1,2})?$/),
      ],
    ],
    stockQuantity: [0 as number | null, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]],
    categoryId: [null as number | null, Validators.required],
  });

  constructor() {
    effect(() => {
      const product = this.product();
      if (product) {
        this.form.reset({
          sku: product.sku,
          name: product.name,
          description: product.description ?? '',
          price: product.price,
          stockQuantity: product.stockQuantity,
          categoryId: product.categoryId,
        });
      }
    });

    effect(() => {
      for (const [field, messages] of Object.entries(this.serverErrors())) {
        const control = this.form.get(field);
        control?.setErrors({ ...control.errors, server: messages.join(' ') });
        control?.markAsTouched();
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      sku: value.sku!.trim(),
      name: value.name!.trim(),
      description: value.description?.trim() || null,
      price: Number(value.price),
      stockQuantity: Number(value.stockQuantity),
      categoryId: value.categoryId!,
    });
  }
}
