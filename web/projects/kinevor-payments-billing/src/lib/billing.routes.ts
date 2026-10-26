import { Routes } from '@angular/router';
import { BillingDashboardComponent } from './billing-dashboard.component';
import { InvoiceDetailComponent } from './invoice-detail.component';
import { InvoiceListComponent } from './invoice-list.component';

export const BILLING_ROUTES: Routes = [
  { path: 'billing/invoices/:id', component: InvoiceDetailComponent },
  { path: 'billing/invoices', component: InvoiceListComponent },
  { path: 'billing', component: BillingDashboardComponent },
];
