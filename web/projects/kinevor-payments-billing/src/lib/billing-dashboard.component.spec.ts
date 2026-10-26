import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BillingDashboardComponent } from './billing-dashboard.component';
import { BillingFacade } from './billing.facade';
import {
  BILLING_GATEWAY,
  BillingGateway,
  BillingStats,
  RevenueSummary,
} from './billing.model';

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

const revenue: RevenueSummary = {
  from: '2026-05-01T00:00:00Z',
  to: '2026-10-31T23:59:59Z',
  grossCollected: 310,
  refunded: 40,
  netRevenue: 270,
  paymentCount: 3,
  currency: 'USD',
  buckets: [
    {
      label: 'Oct',
      periodStart: '2026-10-01T00:00:00Z',
      grossCollected: 310,
      refunded: 40,
      netRevenue: 270,
    },
  ],
};

function gateway(): BillingGateway {
  return {
    stats: vi.fn().mockResolvedValue(stats),
    revenue: vi.fn().mockResolvedValue(revenue),
    listInvoices: vi.fn().mockResolvedValue({
      items: [
        {
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
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 6,
      totalPages: 1,
      hasNext: false,
      hasPrev: false,
    }),
  } as unknown as BillingGateway;
}

describe('BillingDashboardComponent', () => {
  it('renders billing totals, revenue and recent invoices', async () => {
    const testGateway = gateway();
    TestBed.configureTestingModule({
      imports: [BillingDashboardComponent],
      providers: [
        BillingFacade,
        provideRouter([]),
        { provide: BILLING_GATEWAY, useValue: testGateway },
      ],
    });

    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();
    await vi.waitFor(() =>
      expect(fixture.componentInstance.facade.stats()).not.toBeNull(),
    );
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Every dollar');
    expect(text).toContain('$373.20');
    expect(text).toContain('$136.95');
    expect(text).toContain('INV-2026-0001');
    expect(text).toContain('Maya Chen');
    expect(fixture.nativeElement.querySelectorAll('.chart-bar')).toHaveLength(1);
    expect(testGateway.stats).toHaveBeenCalledTimes(1);
  });
});
