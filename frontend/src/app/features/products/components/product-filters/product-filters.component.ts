import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged, filter, map } from 'rxjs';
import { Category } from '../../../categories/models/category.model';
import { EMPTY_PRODUCT_FILTER, LOW_STOCK_FILTER, ProductFilter } from '../../models/product-filter.model';

function stockRangeValidator(group: AbstractControl): ValidationErrors | null {
  const { minStock, maxStock } = group.value as { minStock: number | null; maxStock: number | null };
  return minStock !== null && maxStock !== null && minStock > maxStock ? { range: true } : null;
}

/** Presentational filter bar. Emits a new filter (debounced) whenever the user changes a valid value. */
@Component({
  selector: 'app-product-filters',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-filters.component.html',
  styleUrl: './product-filters.component.scss',
})
export class ProductFiltersComponent {
  readonly filter = input.required<ProductFilter>();
  readonly categories = input.required<Category[]>();
  readonly filterChange = output<ProductFilter>();

  protected readonly form = inject(FormBuilder).group(
    {
      name: [''],
      minStock: [null as number | null, Validators.min(0)],
      maxStock: [null as number | null, Validators.min(0)],
      categoryId: [null as number | null],
    },
    { validators: stockRangeValidator },
  );

  constructor() {
    // Keep the form in sync when the filter is changed from outside (e.g. "clear filters").
    effect(() => this.form.setValue(toFormValue(this.filter()), { emitEvent: false }));

    this.form.valueChanges
      .pipe(
        debounceTime(300),
        filter(() => this.form.valid),
        map(() => normalize(this.form.getRawValue())),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        takeUntilDestroyed(),
      )
      .subscribe((value) => this.filterChange.emit(value));
  }

  protected clear(): void {
    this.filterChange.emit(EMPTY_PRODUCT_FILTER);
  }

  protected showLowStock(): void {
    this.filterChange.emit(LOW_STOCK_FILTER);
  }

  protected get hasActiveFilter(): boolean {
    return JSON.stringify(this.filter()) !== JSON.stringify(EMPTY_PRODUCT_FILTER);
  }
}

function toFormValue(filter: ProductFilter) {
  return {
    name: filter.name,
    minStock: filter.minStock,
    maxStock: filter.maxStock,
    categoryId: filter.categoryId,
  };
}

function normalize(value: {
  name: string | null;
  minStock: number | null;
  maxStock: number | null;
  categoryId: number | null;
}): ProductFilter {
  const toNumber = (n: number | null) => (n === null || Number.isNaN(Number(n)) ? null : Number(n));
  return {
    name: (value.name ?? '').trim(),
    minStock: toNumber(value.minStock),
    maxStock: toNumber(value.maxStock),
    categoryId: value.categoryId,
  };
}
