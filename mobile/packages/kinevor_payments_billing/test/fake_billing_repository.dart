import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

class FakeBillingRepository implements BillingRepository {
  FakeBillingRepository({this.failure});

  final Object? failure;
  int summaryCalls = 0;
  int invoiceCalls = 0;
  int detailCalls = 0;
  int paymentCalls = 0;
  int payCalls = 0;
  String? lastStatus;
  double? lastAmount;
  String? lastMethod;
  String? lastReference;

  void _check() {
    if (failure != null) throw failure!;
  }

  @override
  Future<BillingSummary> getSummary() async {
    summaryCalls++;
    _check();
    return sampleSummary;
  }

  @override
  Future<List<InvoiceSummary>> getInvoices({String? status}) async {
    invoiceCalls++;
    lastStatus = status;
    _check();
    if (status == null) return [...sampleInvoices];
    return [
      for (final invoice in sampleInvoices)
        if (invoice.status.toLowerCase() == status.toLowerCase()) invoice,
    ];
  }

  @override
  Future<InvoiceDetail> getInvoice(String id) async {
    detailCalls++;
    _check();
    return sampleDetails.firstWhere((invoice) => invoice.id == id);
  }

  @override
  Future<List<MemberPayment>> getPayments() async {
    paymentCalls++;
    _check();
    return [...samplePayments];
  }

  @override
  Future<PaymentRecorded> payInvoice(
    String id, {
    double? amount,
    required String method,
    String? reference,
  }) async {
    payCalls++;
    lastAmount = amount;
    lastMethod = method;
    lastReference = reference;
    _check();
    return const PaymentRecorded(
      id: 'payment-new',
      reference: 'TXN-TEST-001',
      status: 'Paid',
    );
  }
}

final sampleSummary = BillingSummary(
  memberId: '11111111-1111-1111-1111-111111111111',
  outstandingAmount: 178.95,
  overdueAmount: 52.95,
  openInvoiceCount: 2,
  overdueInvoiceCount: 1,
  paidThisYear: 144.45,
  nextDueAt: DateTime(2026, 11, 1),
  currency: 'USD',
);

final sampleInvoices = [
  InvoiceSummary(
    id: 'invoice-open',
    number: 'INV-2026-000003',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Open',
    issuedAt: DateTime(2026, 10, 18),
    dueAt: DateTime(2026, 11, 1),
    subTotal: 120,
    taxAmount: 6,
    total: 126,
    amountPaid: 0,
    outstandingAmount: 126,
    currency: 'USD',
    lineCount: 1,
  ),
  InvoiceSummary(
    id: 'invoice-overdue',
    number: 'INV-2026-000002',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Overdue',
    issuedAt: DateTime(2026, 10, 1),
    dueAt: DateTime(2026, 10, 15),
    subTotal: 79,
    taxAmount: 3.95,
    total: 82.95,
    amountPaid: 30,
    outstandingAmount: 52.95,
    currency: 'USD',
    lineCount: 2,
  ),
  InvoiceSummary(
    id: 'invoice-void',
    number: 'INV-2026-000004',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Void',
    issuedAt: DateTime(2026, 9, 26),
    dueAt: DateTime(2026, 10, 10),
    subTotal: 40,
    taxAmount: 2,
    total: 42,
    amountPaid: 0,
    outstandingAmount: 0,
    currency: 'USD',
    lineCount: 1,
  ),
  InvoiceSummary(
    id: 'invoice-paid',
    number: 'INV-2026-000001',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Paid',
    issuedAt: DateTime(2026, 9, 1),
    dueAt: DateTime(2026, 9, 15),
    subTotal: 109,
    taxAmount: 5.45,
    total: 114.45,
    amountPaid: 114.45,
    outstandingAmount: 0,
    currency: 'USD',
    lineCount: 2,
  ),
];

final sampleDetails = [
  InvoiceDetail(
    id: 'invoice-open',
    number: 'INV-2026-000003',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Open',
    issuedAt: DateTime(2026, 10, 18),
    dueAt: DateTime(2026, 11, 1),
    subTotal: 120,
    taxAmount: 6,
    total: 126,
    amountPaid: 0,
    outstandingAmount: 126,
    currency: 'USD',
    lineCount: 1,
    notes: 'Locker 128, renews annually.',
    lines: const [
      InvoiceLine(
        id: 'line-locker',
        description: 'Annual locker rental',
        quantity: 1,
        unitPrice: 120,
        taxRate: 0.05,
        subtotal: 120,
        taxAmount: 6,
        total: 126,
      ),
    ],
  ),
  InvoiceDetail(
    id: 'invoice-overdue',
    number: 'INV-2026-000002',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Overdue',
    issuedAt: DateTime(2026, 10, 1),
    dueAt: DateTime(2026, 10, 15),
    subTotal: 79,
    taxAmount: 3.95,
    total: 82.95,
    amountPaid: 30,
    outstandingAmount: 52.95,
    currency: 'USD',
    lineCount: 2,
    lines: const [
      InvoiceLine(
        id: 'line-membership',
        description: 'Standard membership - monthly',
        quantity: 1,
        unitPrice: 49,
        taxRate: 0.05,
        subtotal: 49,
        taxAmount: 2.45,
        total: 51.45,
      ),
      InvoiceLine(
        id: 'line-guest',
        description: 'Guest day passes',
        quantity: 2,
        unitPrice: 15,
        taxRate: 0.05,
        subtotal: 30,
        taxAmount: 1.5,
        total: 31.5,
      ),
    ],
  ),
  InvoiceDetail(
    id: 'invoice-void',
    number: 'INV-2026-000004',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Void',
    issuedAt: DateTime(2026, 9, 26),
    dueAt: DateTime(2026, 10, 10),
    voidedAt: DateTime(2026, 9, 30),
    voidReason: 'Duplicate registration',
    subTotal: 40,
    taxAmount: 2,
    total: 42,
    amountPaid: 0,
    outstandingAmount: 0,
    currency: 'USD',
    lineCount: 1,
    lines: const [
      InvoiceLine(
        id: 'line-nutrition',
        description: 'Nutrition consultation',
        quantity: 1,
        unitPrice: 40,
        taxRate: 0.05,
        subtotal: 40,
        taxAmount: 2,
        total: 42,
      ),
    ],
  ),
  InvoiceDetail(
    id: 'invoice-paid',
    number: 'INV-2026-000001',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    memberEmail: 'maya.thompson@kinevor.demo',
    status: 'Paid',
    issuedAt: DateTime(2026, 9, 1),
    dueAt: DateTime(2026, 9, 15),
    paidAt: DateTime(2026, 9, 5),
    subTotal: 109,
    taxAmount: 5.45,
    total: 114.45,
    amountPaid: 114.45,
    outstandingAmount: 0,
    currency: 'USD',
    lineCount: 2,
    lines: const [
      InvoiceLine(
        id: 'line-paid-membership',
        description: 'Standard membership - monthly',
        quantity: 1,
        unitPrice: 49,
        taxRate: 0.05,
        subtotal: 49,
        taxAmount: 2.45,
        total: 51.45,
      ),
      InvoiceLine(
        id: 'line-paid-pt',
        description: 'Personal training session',
        quantity: 2,
        unitPrice: 30,
        taxRate: 0.05,
        subtotal: 60,
        taxAmount: 3,
        total: 63,
      ),
    ],
  ),
];

final samplePayments = [
  MemberPayment(
    id: 'payment-cash',
    reference: 'TXN-SEED-MAYA-002',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    invoiceId: 'invoice-overdue',
    invoiceNumber: 'INV-2026-000002',
    description: 'Part payment at front desk',
    method: 'Cash',
    status: 'Paid',
    amount: 30,
    refundedAmount: 0,
    netAmount: 30,
    currency: 'USD',
    paidAt: DateTime(2026, 10, 12),
    createdAt: DateTime(2026, 10, 12),
  ),
  MemberPayment(
    id: 'payment-card',
    reference: 'TXN-SEED-MAYA-001',
    memberId: '11111111-1111-1111-1111-111111111111',
    memberName: 'Maya Thompson',
    invoiceId: 'invoice-paid',
    invoiceNumber: 'INV-2026-000001',
    description: 'Membership and PT',
    method: 'Card',
    status: 'Paid',
    amount: 114.45,
    refundedAmount: 0,
    netAmount: 114.45,
    currency: 'USD',
    paidAt: DateTime(2026, 9, 5),
    createdAt: DateTime(2026, 9, 5),
  ),
];
