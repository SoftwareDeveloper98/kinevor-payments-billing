import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { BillingFacade } from './billing.facade';
import {
  BILLING_GATEWAY,
  BillingGateway,
  BillingStats,
  InvoiceDetail,
  InvoiceSummary,
  PaymentSummary,
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

const invoiceSummary: InvoiceSummary = {
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

const invoiceDetail: InvoiceDetail = {
  ...invoiceSummary,
  paidAt: null,
  voidedAt: null,
  voidReason: null,
  notes: 'Autopay enabled',
  lines: [],
};

const payment: PaymentSummary = {
  id: 'pay-1',
  reference: 'CARD-1042',
  memberId: 'maya',
  memberName: 'Maya Chen',
  invoiceId: 'inv-1',
  invoiceNumber: 'INV-2026-0001',
  description: 'Payment for INV-2026-0001',
  method: 'Card',
  status: 'Paid',
  amount: 50,
  refundedAmount: 0,
  netAmount: 50,
  currency: 'USD',
  paidAt: '2026-10-10T00:00:00Z',
  refundedAt: null,
  createdAt: '2026-10-10T00:00:00Z',
};

function createGateway(): BillingGateway {
  return {
    stats: vi.fn().mockResolvedValue(stats),
    revenue: vi.fn().mockResolvedValue(revenue),
    listInvoices: vi.fn().mockResolvedValue({
      items: [invoiceSummary],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
      hasNext: false,
      hasPrev: false,
    }),
    invoice: vi.fn().mockResolvedValue(invoiceDetail),
    createInvoice: vi.fn(),
    markInvoicePaid: vi.fn(),
    voidInvoice: vi.fn(),
    markInvoiceOverdue: vi.fn(),
    listPayments: vi.fn().mockResolvedValue({
      items: [payment],
      totalCount: 1,
      page: 1,
      pageSize: 50,
      totalPages: 1,
      hasNext: false,
      hasPrev: false,
    }),
    payment: vi.fn(),
    recordPayment: vi.fn().mockResolvedValue({
      id: 'pay-2',
      reference: 'CARD-1043',
      status: 'Paid',
    }),
    refundPayment: vi.fn().mockResolvedValue({ ...payment, status: 'Refunded' }),
  };
}

describe('BillingFacade', () => {
  let gateway: BillingGateway;
  let facade: BillingFacade;

  beforeEach(() => {
    gateway = createGateway();
    TestBed.configureTestingModule({
      providers: [BillingFacade, { provide: BILLING_GATEWAY, useValue: gateway }],
    });
    facade = TestBed.inject(BillingFacade);
  });

  it('loads dashboard data and derives collection metrics', async () => {
    await facade.loadDashboard();

    expect(gateway.stats).toHaveBeenCalled();
    expect(gateway.revenue).toHaveBeenCalled();
    expect(gateway.listInvoices).toHaveBeenCalledWith({ page: 1, pageSize: 6 });
    expect(facade.stats()).toEqual(stats);
    expect(facade.recentInvoices()).toHaveLength(1);
    expect(facade.collectionRate()).toBe(14);
    expect(facade.overdueShare()).toBe(37);
    expect(facade.revenuePeak()).toBe(310);
  });

  it('records a payment against the loaded invoice and refreshes the ledger', async () => {
    await facade.loadInvoice('inv-1');

    const recorded = await facade.recordPayment(50, 'Card', 'CARD-1043');

    expect(recorded).toBe(true);
    expect(gateway.recordPayment).toHaveBeenCalledWith({
      memberId: 'maya',
      memberName: 'Maya Chen',
      invoiceId: 'inv-1',
      amount: 50,
      method: 'Card',
      reference: 'CARD-1043',
      description: 'Payment for INV-2026-0001',
    });
    expect(gateway.listPayments).toHaveBeenCalledWith({
      search: 'INV-2026-0001',
      pageSize: 50,
    });
    expect(facade.notice()).toContain('CARD-1043');
    expect(facade.stats()).toEqual(stats);
  });

  it('refunds a captured payment and refreshes invoice insights', async () => {
    await facade.loadInvoice('inv-1');

    const refunded = await facade.refundPayment('pay-1', 25, 'Member request');

    expect(refunded).toBe(true);
    expect(gateway.refundPayment).toHaveBeenCalledWith('pay-1', {
      amount: 25,
      reason: 'Member request',
    });
    expect(facade.notice()).toContain('Refund recorded');
    expect(facade.stats()).toEqual(stats);
  });

  it('turns an unreachable API into an actionable message', async () => {
    vi.mocked(gateway.listInvoices).mockRejectedValueOnce(
      new HttpErrorResponse({ status: 0 }),
    );

    await facade.loadInvoices();

    expect(facade.error()).toContain('port 5087');
    expect(facade.loading()).toBe(false);
  });
});
