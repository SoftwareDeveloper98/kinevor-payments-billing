import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BILLING_API_BASE_URL, BillingStats } from './billing.model';
import { HttpBillingGateway } from './http-billing.gateway';

const stats: BillingStats = {
  openInvoiceCount: 4,
  overdueInvoiceCount: 2,
  paidInvoiceCount: 1,
  totalInvoiceCount: 7,
  outstandingAmount: 373.2,
  overdueAmount: 136.95,
  collectedThisMonth: 310,
  refundedThisMonth: 40,
  netThisMonth: 270,
  currency: 'USD',
};

describe('HttpBillingGateway', () => {
  let gateway: HttpBillingGateway;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        HttpBillingGateway,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BILLING_API_BASE_URL, useValue: 'http://localhost:5087/api/v1/' },
      ],
    });
    gateway = TestBed.inject(HttpBillingGateway);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the stats envelope', async () => {
    const promise = gateway.stats();
    const request = http.expectOne('http://localhost:5087/api/v1/billing/stats');
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, data: stats });

    await expect(promise).resolves.toEqual(stats);
  });

  it('serializes invoice filters and paging', async () => {
    const promise = gateway.listInvoices({
      search: 'maya',
      status: 'Overdue',
      page: 2,
      pageSize: 10,
    });
    const request = http.expectOne(
      (candidate) => candidate.url === 'http://localhost:5087/api/v1/billing/invoices',
    );

    expect(request.request.params.get('search')).toBe('maya');
    expect(request.request.params.get('status')).toBe('Overdue');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');

    request.flush({
      success: true,
      data: {
        items: [],
        totalCount: 0,
        page: 2,
        pageSize: 10,
        totalPages: 0,
        hasNext: false,
        hasPrev: true,
      },
    });

    await expect(promise).resolves.toMatchObject({ totalCount: 0, page: 2 });
  });

  it('posts a payment request and returns its reference', async () => {
    const promise = gateway.recordPayment({
      memberId: 'maya',
      memberName: 'Maya Chen',
      invoiceId: 'inv-1',
      amount: 50,
      method: 'BankTransfer',
      reference: 'TRANSFER-9',
    });
    const request = http.expectOne('http://localhost:5087/api/v1/billing/payments');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      invoiceId: 'inv-1',
      amount: 50,
      method: 'BankTransfer',
      currency: 'USD',
    });

    request.flush({
      success: true,
      data: { id: 'pay-2', reference: 'TRANSFER-9', status: 'Paid' },
    });

    await expect(promise).resolves.toMatchObject({ reference: 'TRANSFER-9' });
  });
});
