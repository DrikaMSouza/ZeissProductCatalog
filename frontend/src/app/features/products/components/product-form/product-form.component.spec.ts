import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductFormComponent } from './product-form.component';
import { SaveProductRequest } from '../../models/product-request.model';

describe('ProductFormComponent', () => {
  let fixture: ComponentFixture<ProductFormComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ProductFormComponent] }).compileComponents();

    fixture = TestBed.createComponent(ProductFormComponent);
    fixture.componentRef.setInput('categories', [{ id: 1, name: 'Frames' }]);
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  function submit(): void {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('does not emit and shows errors when required fields are missing', async () => {
    const emitted: SaveProductRequest[] = [];
    fixture.componentInstance.save.subscribe((value) => emitted.push(value));

    submit();
    await fixture.whenStable();

    expect(emitted).toHaveLength(0);
    expect(element.textContent).toContain('SKU is required.');
    expect(element.textContent).toContain('Name is required.');
  });

  it('pre-fills the form when editing and emits a trimmed request', async () => {
    fixture.componentRef.setInput('product', {
      id: 123456,
      sku: 'FRM-1',
      name: ' Titanium Frame ',
      description: '',
      price: 229,
      stockQuantity: 4,
      categoryId: 1,
      categoryName: 'Frames',
      createdAtUtc: '',
      updatedAtUtc: '',
    });
    await fixture.whenStable();

    const emitted: SaveProductRequest[] = [];
    fixture.componentInstance.save.subscribe((value) => emitted.push(value));
    submit();

    expect(emitted).toEqual([
      { sku: 'FRM-1', name: 'Titanium Frame', description: null, price: 229, stockQuantity: 4, categoryId: 1 },
    ]);
  });

  it('shows field errors returned by the API', async () => {
    const skuInput = element.querySelector<HTMLInputElement>('input[formControlName="sku"]')!;
    skuInput.value = 'X';
    skuInput.dispatchEvent(new Event('input'));

    fixture.componentRef.setInput('serverErrors', { sku: ['A product with SKU "X" already exists.'] });
    await fixture.whenStable();

    expect(element.textContent).toContain('A product with SKU "X" already exists.');
  });
});
