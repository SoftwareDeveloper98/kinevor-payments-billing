import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

void main() {
  test('loads a member summary and sends demo headers', () async {
    late http.Request captured;
    final client = KinevorBillingClient(
      baseUrl: Uri.parse('https://billing.test/api/v1/'),
      headers: const {
        'X-Demo-Role': 'Member',
        'X-Demo-Member': '11111111-1111-1111-1111-111111111111',
      },
      httpClient: MockClient((request) async {
        captured = request;
        return http.Response(
          jsonEncode({
            'success': true,
            'data': {
              'memberId': 'm1',
              'outstandingAmount': 178.95,
              'overdueAmount': 52.95,
              'openInvoiceCount': 2,
              'overdueInvoiceCount': 1,
              'paidThisYear': 144.45,
              'nextDueAt': '2026-11-01T00:00:00Z',
              'currency': 'USD',
            },
          }),
          200,
        );
      }),
    );
    addTearDown(client.close);

    final summary = await client.fetchSummary();

    expect(
      captured.url.toString(),
      'https://billing.test/api/v1/me/invoices/summary',
    );
    expect(captured.headers['X-Demo-Role'], 'Member');
    expect(captured.headers['Accept'], 'application/json');
    expect(summary.outstandingAmount, 178.95);
    expect(summary.openInvoiceCount, 2);
    expect(summary.nextDueAt, DateTime.utc(2026, 11, 1));
  });

  test('loads filtered invoices and payments from paged envelopes', () async {
    final requests = <http.Request>[];
    final client = KinevorBillingClient(
      baseUrl: Uri.parse('https://billing.test/api/v1'),
      httpClient: MockClient((request) async {
        requests.add(request);
        if (request.url.path.endsWith('/payments')) {
          return http.Response(
            jsonEncode({
              'success': true,
              'data': {
                'items': [
                  {
                    'id': 'p1',
                    'reference': 'TXN-1',
                    'memberId': 'm1',
                    'memberName': 'Maya Thompson',
                    'invoiceId': 'i1',
                    'invoiceNumber': 'INV-2026-000002',
                    'description': 'Part payment',
                    'method': 'Cash',
                    'status': 'Paid',
                    'amount': 30,
                    'refundedAmount': 0,
                    'netAmount': 30,
                    'currency': 'USD',
                    'paidAt': '2026-10-12T00:00:00Z',
                    'createdAt': '2026-10-12T00:00:00Z',
                  },
                ],
              },
            }),
            200,
          );
        }
        return http.Response(
          jsonEncode({
            'success': true,
            'data': {
              'items': [
                {
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
                  'lineCount': 2,
                },
              ],
            },
          }),
          200,
        );
      }),
    );
    addTearDown(client.close);

    final invoices = await client.fetchInvoices(status: 'Overdue');
    final payments = await client.fetchPayments();

    expect(requests.first.url.queryParameters['status'], 'Overdue');
    expect(requests.first.url.queryParameters['pageSize'], '50');
    expect(invoices.single.number, 'INV-2026-000002');
    expect(invoices.single.isPayable, isTrue);
    expect(payments.single.invoiceId, 'i1');
    expect(payments.single.method, 'Cash');
  });

  test('posts a simulated payment and parses the result', () async {
    late String body;
    final client = KinevorBillingClient(
      baseUrl: Uri.parse('https://billing.test/api/v1'),
      httpClient: MockClient((request) async {
        body = request.body;
        expect(request.method, 'POST');
        expect(request.url.path, '/api/v1/me/invoices/i1/pay');
        return http.Response(
          jsonEncode({
            'success': true,
            'data': {'id': 'p2', 'reference': 'TXN-NEW', 'status': 'Paid'},
          }),
          200,
        );
      }),
    );
    addTearDown(client.close);

    final payment = await client.payInvoice(
      'i1',
      amount: 52.95,
      method: 'BankTransfer',
      reference: ' BACS-42 ',
    );

    final parsed = jsonDecode(body) as Map<String, dynamic>;
    expect(parsed['amount'], 52.95);
    expect(parsed['method'], 'BankTransfer');
    expect(parsed['reference'], 'BACS-42');
    expect(payment.reference, 'TXN-NEW');
  });

  test('surfaces API failure messages', () async {
    final client = KinevorBillingClient(
      baseUrl: Uri.parse('https://billing.test/api/v1'),
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode({
            'success': false,
            'message': 'Invoice is not payable.',
            'errors': null,
          }),
          409,
        ),
      ),
    );
    addTearDown(client.close);

    await expectLater(
      client.payInvoice('i1', method: 'Card'),
      throwsA(
        isA<BillingApiException>()
            .having(
              (error) => error.message,
              'message',
              'Invoice is not payable.',
            )
            .having((error) => error.statusCode, 'statusCode', 409),
      ),
    );
  });
}
