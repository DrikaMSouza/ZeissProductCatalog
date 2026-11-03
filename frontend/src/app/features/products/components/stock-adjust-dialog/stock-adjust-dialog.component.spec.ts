import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Product } from '../../models/product.model';
import { StockAdjustDialogComponent, StockAdjustDialogData } from './stock-adjust-dialog.component';

const product = { id: 123456, name: 'Lens', stockQuantity: 2 } as Product;

describe('StockAdjustDialogComponent', () => {
  let fixture: ComponentFixture<StockAdjustDialogComponent>;
  let close: ReturnType<typeof vi.fn>;

  async function open(data: StockAdjustDialogData): Promise<HTMLElement> {
    close = vi.fn();
    await TestBed.configureTestingModule({
      imports: [StockAdjustDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StockAdjustDialogComponent);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function setQuantity(element: HTMLElement, value: string): void {
    const input = element.querySelector('input')!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  it('closes with the quantity when the form is submitted', async () => {
    const element = await open({ product, movement: 'decrement' });

    setQuantity(element, '2');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    expect(close).toHaveBeenCalledWith(2);
  });

  it('does not allow removing more than the available stock', async () => {
    const element = await open({ product, movement: 'decrement' });

    setQuantity(element, '3');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(close).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Only 2 unit(s) available.');
  });

  it('allows adding more than the current stock', async () => {
    const element = await open({ product, movement: 'add' });

    setQuantity(element, '50');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    expect(close).toHaveBeenCalledWith(50);
  });
});
