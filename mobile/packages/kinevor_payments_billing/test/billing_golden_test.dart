import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

import 'fake_billing_repository.dart';

Future<void> _loadFonts() async {
  final bytes = await File('assets/fonts/Sora-Variable.ttf').readAsBytes();
  final sora = FontLoader('Sora')
    ..addFont(Future<ByteData>.value(ByteData.sublistView(bytes)));
  await sora.load();

  final material = await rootBundle.load('fonts/MaterialIcons-Regular.otf');
  final icons = FontLoader('MaterialIcons')
    ..addFont(Future<ByteData>.value(material));
  await icons.load();
}

void main() {
  setUpAll(_loadFonts);

  testWidgets('invoice list golden', (tester) async {
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      MaterialApp(
        theme: KinevorBillingTheme.dark(),
        home: InvoiceListScreen(
          repository: FakeBillingRepository(),
          memberName: 'Maya Thompson',
        ),
      ),
    );
    await tester.pumpAndSettle();

    await expectLater(
      find.byType(InvoiceListScreen),
      matchesGoldenFile('goldens/invoice_list.png'),
    );
  });

  testWidgets('invoice detail golden', (tester) async {
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      MaterialApp(
        theme: KinevorBillingTheme.dark(),
        home: InvoiceDetailScreen(
          repository: FakeBillingRepository(),
          invoiceId: 'invoice-overdue',
        ),
      ),
    );
    await tester.pumpAndSettle();

    await expectLater(
      find.byType(InvoiceDetailScreen),
      matchesGoldenFile('goldens/invoice_detail.png'),
    );
  });
}
