import { Routes } from '@angular/router';
import { ProductsStore } from './state/products.store';

export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    // One store instance shared by every page of the feature.
    providers: [ProductsStore],
    children: [
      {
        path: '',
        title: 'Products',
        loadComponent: () =>
          import('./pages/product-list-page/product-list-page.component').then(
            (m) => m.ProductListPageComponent,
          ),
      },
      {
        path: 'new',
        title: 'New product',
        loadComponent: () =>
          import('./pages/product-edit-page/product-edit-page.component').then(
            (m) => m.ProductEditPageComponent,
          ),
      },
      {
        path: ':id/edit',
        title: 'Edit product',
        loadComponent: () =>
          import('./pages/product-edit-page/product-edit-page.component').then(
            (m) => m.ProductEditPageComponent,
          ),
      },
    ],
  },
];
