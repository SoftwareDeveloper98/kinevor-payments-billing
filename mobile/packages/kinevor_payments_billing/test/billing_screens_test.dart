import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

import 'fake_billing_repository.dart';

Widget _app(Widget home) =>
    MaterialApp(theme: KinevorBillingTheme.dark(), home: home);

void main() {
  testWidgets('invoice list renders balance, stats and invoice cards', (
    tester,
  ) async {
    final repository = FakeBillingRepository();
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      _app(
        InvoiceListScreen(repository: repository, memberName: 'Maya Thompson'),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text(r'$178.95'), findsOneWidget);
    expect(find.text(r'$52.95 overdue'), findsOneWidget);
    expect(find.text('INV-2026-000003'), findsOneWidget);
    expect(find.text('4 invoices'), findsOneWidget);

    final scrollable = find.byType(Scrollable).first;
    await tester.scrollUntilVisible(
      find.text('INV-2026-000002'),
      250,
      scrollable: scrollable,
    );
    expect(find.text('INV-2026-000002'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('INV-2026-000004'),
      250,
      scrollable: scrollable,
    );
    expect(find.text('INV-2026-000004'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('INV-2026-000001'),
      250,
      scrollable: scrollable,
    );
    expect(find.text('INV-2026-000001'), findsOneWidget);
    expect(repository.summaryCalls, 1);
    expect(repository.invoiceCalls, 1);
  });

  testWidgets('invoice list filters by status', (tester) async {
    final repository = FakeBillingRepository();

    await tester.pumpWidget(_app(InvoiceListScreen(repository: repository)));
    await tester.pumpAndSettle();

    final overdueFilter = find.descendant(
      of: find.byType(ChoiceChip),
      matching: find.text('Overdue'),
    );
    await tester.tap(overdueFilter);
    await tester.pumpAndSettle();

    expect(repository.lastStatus, 'Overdue');
    expect(find.text('INV-2026-000002'), findsOneWidget);
    expect(find.text('INV-2026-000003'), findsNothing);
    expect(find.text('1 invoice'), findsOneWidget);
  });

  testWidgets('invoice detail renders lines, totals and payment history', (
    tester,
  ) async {
    final repository = FakeBillingRepository();
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      _app(
        InvoiceDetailScreen(
          repository: repository,
          invoiceId: 'invoice-overdue',
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('INV-2026-000002'), findsOneWidget);
    expect(find.text('Standard membership - monthly'), findsOneWidget);
    expect(find.text('Guest day passes'), findsOneWidget);
    expect(find.text('Pay invoice'), findsOneWidget);
    expect(find.text('2'), findsWidgets);

    final scrollable = find.byType(Scrollable).first;
    await tester.scrollUntilVisible(
      find.text('TXN-SEED-MAYA-002'),
      250,
      scrollable: scrollable,
    );
    expect(find.text('TXN-SEED-MAYA-002'), findsOneWidget);
    expect(find.text('Cash · Oct 12, 2026'), findsOneWidget);
  });

  testWidgets('payment sheet records a simulated payment', (tester) async {
    final repository = FakeBillingRepository();
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      _app(
        InvoiceDetailScreen(
          repository: repository,
          invoiceId: 'invoice-overdue',
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Pay invoice'));
    await tester.pumpAndSettle();

    expect(find.text('Pay this invoice'), findsOneWidget);
    expect(find.text(r'$52.95'), findsWidgets);

    await tester.tap(find.text('Bank transfer'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).at(1), 'BACS-42');
    await tester.tap(find.text('Pay now'));
    await tester.pumpAndSettle();

    expect(repository.payCalls, 1);
    expect(repository.lastAmount, 52.95);
    expect(repository.lastMethod, 'BankTransfer');
    expect(repository.lastReference, 'BACS-42');
    expect(find.text('Pay this invoice'), findsNothing);
  });

  testWidgets('load failures show a retry action', (tester) async {
    final repository = FakeBillingRepository(
      failure: const BillingApiException('Billing API unavailable.'),
    );
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(_app(InvoiceListScreen(repository: repository)));
    await tester.pumpAndSettle();

    expect(find.text('Could not load your billing details.'), findsOneWidget);
    expect(find.text('Try again'), findsOneWidget);
  });
}
