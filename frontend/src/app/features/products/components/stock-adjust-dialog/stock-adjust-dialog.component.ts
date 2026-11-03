import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Product } from '../../models/product.model';
import { StockMovement } from '../../models/product-request.model';

export interface StockAdjustDialogData {
  product: Product;
  movement: StockMovement;
}

/** Asks for the quantity of a stock movement. Closes with the quantity, or undefined when cancelled. */
@Component({
  selector: 'app-stock-adjust-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ isAdd ? 'Add to stock' : 'Remove from stock' }}</h2>
    <form [formGroup]="form" (ngSubmit)="confirm()">
      <mat-dialog-content>
        <p>
          <strong>{{ data.product.name }}</strong> currently has
          <strong>{{ data.product.stockQuantity }}</strong> unit(s) in stock.
        </p>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Quantity</mat-label>
          <input matInput type="number" min="1" formControlName="quantity" cdkFocusInitial />
          @if (quantity.hasError('required')) {
            <mat-error>Quantity is required.</mat-error>
          } @else if (quantity.hasError('min')) {
            <mat-error>Quantity must be at least 1.</mat-error>
          } @else if (quantity.hasError('max')) {
            <mat-error>Only {{ data.product.stockQuantity }} unit(s) available.</mat-error>
          } @else if (quantity.hasError('pattern')) {
            <mat-error>Quantity must be a whole number.</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit" [disabled]="quantity.invalid">
          {{ isAdd ? 'Add' : 'Remove' }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .full-width {
      width: 100%;
    }
  `,
})
export class StockAdjustDialogComponent {
  protected readonly data = inject<StockAdjustDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<StockAdjustDialogComponent, number>>(MatDialogRef);

  protected readonly isAdd = this.data.movement === 'add';

  protected readonly quantity = new FormControl<number | null>(1, [
    Validators.required,
    Validators.min(1),
    Validators.pattern(/^\d+$/),
    ...(this.isAdd ? [] : [Validators.max(this.data.product.stockQuantity)]),
  ]);

  protected readonly form = new FormGroup({ quantity: this.quantity });

  protected confirm(): void {
    if (this.quantity.valid && this.quantity.value !== null) {
      this.dialogRef.close(Number(this.quantity.value));
    }
  }
}
