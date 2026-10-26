import 'package:flutter_test/flutter_test.dart';
import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

void main() {
  test('parses a summary with defensive defaults', () {
    final summary = BillingSummary.fromJson({
      'memberId': 'm1',
      'outstandingAmount': '178.95',
      'overdueAmount': 52.95,
      'openInvoiceCount': '2',
      'overdueInvoiceCount': 1,
      'paidThisYear': 144.45,
      'currency': 'USD',
    });

    expect(summary.outstandingAmount, 178.95);
    expect(summary.openInvoiceCount, 2);
    expect(summary.nextDueAt, isNull);
    expect(summary.isSettled, isFalse);
  });

  test('parses invoice details and derives a line count', () {
    final invoice = InvoiceDetail.fromJson({
      'id': 'i1',
      'number': 'INV-2026-000002',
      'memberId': 'm1',
      'memberName': 'Maya Thompson',
      'status': 'Overdue',
      'issuedAt': '2026-10-01T00:00:00Z',
      'dueAt': '2026-10-15T00:00:00Z',
      'subTotal': 79,
      'taxAmount': 3.95,
      'total': 82.95,
      'amountPaid': 30,
      'outstandingAmount': 52.95,
      'currency': 'USD',
      'lines': [
        {
          'id': 'l1',
          'description': 'Membership',
          'quantity': 1,
          'unitPrice': 49,
          'taxRate': 0.05,
          'subtotal': 49,
          'taxAmount': 2.45,
          'total': 51.45,
        },
        {
          'id': 'l2',
          'description': 'Guest passes',
          'quantity': 2,
          'unitPrice': 15,
          'taxRate': 0.05,
          'subtotal': 30,
          'taxAmount': 1.5,
          'total': 31.5,
        },
      ],
    });

    expect(invoice.lineCount, 2);
    expect(invoice.lines.last.quantity, 2);
    expect(invoice.lines.last.total, 31.5);
    expect(invoice.isPayable, isTrue);
  });

  test('parses payments and maps missing fields safely', () {
    final payment = MemberPayment.fromJson({
      'id': 'p1',
      'reference': 'TXN-1',
      'method': 'Cash',
      'status': 'Paid',
      'amount': 30,
      'netAmount': 30,
      'currency': 'USD',
      'paidAt': '2026-10-12T00:00:00Z',
      'createdAt': '2026-10-12T00:00:00Z',
    });

    expect(payment.refundedAmount, 0);
    expect(payment.invoiceId, isNull);
    expect(payment.memberName, '');
    expect(payment.amount, 30);
  });

  test('formats money, dates, labels and counts', () {
    const strings = BillingStrings();

    expect(strings.money('USD', 1234.5), r'$1,234.50');
    expect(strings.money('EUR', -12), '-€12.00');
    expect(strings.money('CAD', 9.9), 'CAD 9.90');
    expect(strings.date(DateTime(2026, 10, 15)), 'Oct 15, 2026');
    expect(strings.monthDay(DateTime(2026, 11, 1)), 'Nov 1');
    expect(strings.statusLabel('PartiallyRefunded'), 'Part refunded');
    expect(strings.methodLabel('BankTransfer'), 'Bank transfer');
    expect(strings.invoiceCount(1), '1 invoice');
    expect(strings.paymentCount(2), '2 payments');
  });
}
