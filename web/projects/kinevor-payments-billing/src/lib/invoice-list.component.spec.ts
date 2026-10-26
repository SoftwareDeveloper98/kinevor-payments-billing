import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BillingFacade } from './billing.facade';
import {
  BILLING_GATEWAY,
  BillingGateway,
  BillingStats,
  InvoiceSummary,
  PagedResult,
} from './billing.model';
import { InvoiceListComponent } from './invoice-list.component';

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

const invoice: InvoiceSummary = {
  id: 'inv-1',
  number: 'INV-2026-0001',
  memberId: 'maya',
  memberName: 'Maya Chen',
  memberEmail: 'maya@example.com',
  status: 'Open',
  issuedAt: '2026-10-01T00:00:00Z',
  dueAt: '2026-10-15T00:00:00Z',
  subTotal: 120,
  taxAmount: 12,
  total: 132,
  amountPaid: 0,
  outstandingAmount: 132,
  currency: 'USD',
  lineCount: 2,
};

function page(items: InvoiceSummary[]): PagedResult<InvoiceSummary> {
  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasNext: false,
    hasPrev: false,
  };
}

function gateway(items: InvoiceSummary[] = [invoice]): BillingGateway {
  return {
    stats: vi.fn().mockResolvedValue(stats),
    revenue: vi.fn().mockResolvedValue({}),
    listInvoices: vi.fn().mockResolvedValue(page(items)),
  } as unknown as BillingGateway;
}

describe('InvoiceListComponent', () => {
  it('renders invoice rows and sends status filters to the gateway', async () => {
    const testGateway = gateway();
    TestBed.configureTestingModule({
      imports: [InvoiceListComponent],
      providers: [
        BillingFacade,
        provideRouter([]),
        { provide: BILLING_GATEWAY, useValue: testGateway },
      ],
    });

    const fixture = TestBed.createComponent(InvoiceListComponent);
    fixture.detectChanges();
    await vi.waitFor(() =>
      expect(fixture.componentInstance.facade.invoices().length).toBeGreaterThan(0),
    );
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('INV-2026-0001');
    expect(text).toContain('Maya Chen');
    expect(text).toContain('$132.00');
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);

    fixture.componentInstance.filters.controls.status.setValue('Overdue');
    fixture.componentInstance.applyFilters();
    await fixture.whenStable();

    expect(testGateway.listInvoices).toHaveBeenLastCalledWith({
      search: undefined,
      status: 'Overdue',
      page: 1,
      pageSize: 20,
    });
  });

  it('shows an empty state when no invoices match', async () => {
    TestBed.configureTestingModule({
      imports: [InvoiceListComponent],
      providers: [
        BillingFacade,
        provideRouter([]),
        { provide: BILLING_GATEWAY, useValue: gateway([]) },
      ],
    });

    const fixture = TestBed.createComponent(InvoiceListComponent);
    fixture.detectChanges();
    await vi.waitFor(() =>
      expect(fixture.componentInstance.facade.loading()).toBe(false),
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No matching invoices');
  });
});
