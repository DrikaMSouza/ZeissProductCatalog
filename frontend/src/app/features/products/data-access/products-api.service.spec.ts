import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../../core/http/api-error';
import { apiErrorInterceptor } from '../../../core/http/api-error.interceptor';
import { ProductsApiService } from './products-api.service';

describe('ProductsApiService', () => {
  let service: ProductsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ProductsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches by name using the search endpoint', () => {
    service.searchByName('lens').subscribe();

    const request = http.expectOne((r) => r.url === '/api/products/search');
    expect(request.request.params.get('name')).toBe('lens');
    request.flush([]);
  });

  it('sends only the provided stock bounds', () => {
    service.getByStockLevel(null, 5).subscribe();

    const request = http.expectOne((r) => r.url === '/api/products/stock-level');
    expect(request.request.params.has('min')).toBe(false);
    expect(request.request.params.get('max')).toBe('5');
    request.flush([]);
  });

  it.each([
    ['add', '/api/products/123456/add-to-stock/3'],
    ['decrement', '/api/products/123456/decrement-stock/3'],
  ] as const)('maps the "%s" stock movement to %s', (movement, url) => {
    service.changeStock(123456, movement, 3).subscribe();

    const request = http.expectOne(url);
    expect(request.request.method).toBe('POST');
    request.flush({});
  });

  it('turns problem details into an ApiError with field errors', async () => {
    const result = firstValueFrom(service.create({} as never));

    http.expectOne('/api/products').flush(
      { title: 'One or more validation errors occurred.', status: 400, errors: { name: ['Name is required.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    const error = await result.catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).isValidation).toBe(true);
    expect((error as ApiError).fieldErrors['name']).toEqual(['Name is required.']);
  });

  it('uses the problem detail as message for conflicts', async () => {
    const result = firstValueFrom(service.changeStock(1, 'decrement', 9));

    http.expectOne('/api/products/1/decrement-stock/9').flush(
      { title: 'Insufficient stock', status: 409, detail: 'Only 2 unit(s) available.' },
      { status: 409, statusText: 'Conflict' },
    );

    const error = (await result.catch((e: unknown) => e)) as ApiError;
    expect(error.status).toBe(409);
    expect(error.message).toBe('Only 2 unit(s) available.');
  });
});
