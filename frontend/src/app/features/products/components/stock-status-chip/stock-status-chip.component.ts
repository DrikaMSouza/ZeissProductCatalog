import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { stockStatusOf } from '../../models/product.model';

const LABELS = {
  'out-of-stock': 'Out of stock',
  low: 'Low stock',
  'in-stock': 'In stock',
} as const;

@Component({
  selector: 'app-stock-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="chip" [class]="status()">{{ quantity() }} · {{ label() }}</span>`,
  styles: `
    .chip {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 999px;
      font: var(--mat-sys-label-medium);
      white-space: nowrap;
    }
    .in-stock {
      background: #e3f4e8;
      color: #1b5e20;
    }
    .low {
      background: #fff4d6;
      color: #8a5300;
    }
    .out-of-stock {
      background: #fde4e4;
      color: #b3261e;
    }
  `,
})
export class StockStatusChipComponent {
  readonly quantity = input.required<number>();

  protected readonly status = computed(() => stockStatusOf(this.quantity()));
  protected readonly label = computed(() => LABELS[this.status()]);
}
