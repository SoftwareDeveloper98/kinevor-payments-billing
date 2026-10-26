import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BillingFacade } from './billing.facade';
import {
  BILLING_GATEWAY,
  BillingGateway,
  BillingStats,
  InvoiceDetail,
  PaymentSummary,
  RevenueSummary,
} from './billing.model';
import { InvoiceDetailComponent } from './invoice-detail.component';

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
  buckets: [],
};

const invoice: InvoiceDetail = {
  id: 'inv-1',
  number: 'INV-2026-0001',
  memberId: 'maya',
  memberName: 'Maya Chen',
  memberEmail: 'maya@example.com',
  status: 'Open',
  issuedAt: '2026-10-01T00:00:00Z',
  dueAt: '2026-10-15T00:00:00Z',
  paidAt: null,
  voidedAt: null,
  voidReason: null,
  notes: 'Autopay enabled',
  subTotal: 120,
  taxAmount: 12,
  total: 132,
  amountPaid: 0,
  outstandingAmount: 132,
  currency: 'USD',
  lines: [
    {
      id: 'line-1',
      description: 'Monthly membership',
      quantity: 1,
      unitPrice: 120,
      taxRate: 0.1,
      subtotal: 120,
      taxAmount: 12,
      total: 132,
    },
  ],
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

function gateway(): BillingGateway {
  return {
    stats: vi.fn().mockResolvedValue(stats),
    revenue: vi.fn().mockResolvedValue(revenue),
    invoice: vi.fn().mockResolvedValue(invoice),
    listPayments: vi.fn().mockResolvedValue({
      items: [payment],
      totalCount: 1,
      page: 1,
      pageSize: 50,
      totalPages: 1,
      hasNext: false,
      hasPrev: false,
    }),
    recordPayment: vi.fn().mockResolvedValue({
      id: 'pay-2',
      reference: 'CARD-1043',
      status: 'Paid',
    }),
    refundPayment: vi.fn().mockResolvedValue({ ...payment, status: 'Refunded' }),
    listInvoices: vi.fn(),
    createInvoice: vi.fn(),
    markInvoicePaid: vi.fn(),
    voidInvoice: vi.fn(),
    markInvoiceOverdue: vi.fn(),
    payment: vi.fn(),
  };
}

async function setup(): Promise<{
  fixture: ComponentFixture<InvoiceDetailComponent>;
  testGateway: BillingGateway;
}> {
  const testGateway = gateway();
  TestBed.configureTestingModule({
    imports: [InvoiceDetailComponent],
    providers: [
      BillingFacade,
      provideRouter([]),
      { provide: BILLING_GATEWAY, useValue: testGateway },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap({ id: 'inv-1' }) } },
      },
    ],
  });

  const fixture = TestBed.createComponent(InvoiceDetailComponent);
  fixture.detectChanges();
  await vi.waitFor(() =>
    expect(fixture.componentInstance.facade.invoice()).not.toBeNull(),
  );
  fixture.detectChanges();
  return { fixture, testGateway };
}

describe('InvoiceDetailComponent', () => {
  it('renders the invoice, payment form and ledger history', async () => {
    const { fixture } = await setup();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('INV-2026-0001');
    expect(text).toContain('Maya Chen');
    expect(text).toContain('Monthly membership');
    expect(text).toContain('Record a payment');
    expect(text).toContain('CARD-1042');
    expect(text).toContain('Autopay enabled');
  });

  it('records a simulated payment with the loaded member context', async () => {
    const { fixture, testGateway } = await setup();

    fixture.componentInstance.paymentForm.patchValue({
      amount: 50,
      method: 'Card',
      reference: 'CARD-1043',
    });
    fixture.componentInstance.recordPayment();

    await vi.waitFor(() => expect(testGateway.recordPayment).toHaveBeenCalled());
    expect(testGateway.recordPayment).toHaveBeenCalledWith({
      memberId: 'maya',
      memberName: 'Maya Chen',
      invoiceId: 'inv-1',
      amount: 50,
      method: 'Card',
      reference: 'CARD-1043',
      description: 'Payment for INV-2026-0001',
    });
  });

  it('submits a partial refund for a captured payment', async () => {
    const { fixture, testGateway } = await setup();

    fixture.componentInstance.startRefund(payment);
    fixture.componentInstance.refundForm.patchValue({
      amount: 25,
      reason: 'Member request',
    });
    fixture.componentInstance.refund(payment);

    await vi.waitFor(() => expect(testGateway.refundPayment).toHaveBeenCalled());
    expect(testGateway.refundPayment).toHaveBeenCalledWith('pay-1', {
      amount: 25,
      reason: 'Member request',
    });
  });
});
