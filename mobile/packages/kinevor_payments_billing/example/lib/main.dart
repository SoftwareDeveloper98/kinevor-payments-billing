import 'package:flutter/material.dart';
import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

const String _apiBaseUrl = String.fromEnvironment(
  'KINEVOR_API_BASE_URL',
  defaultValue: 'http://localhost:5087/api/v1',
);

void main() {
  final client = KinevorBillingClient(
    baseUrl: Uri.parse(_apiBaseUrl),
    headers: const {
      'X-Demo-Role': 'Member',
      'X-Demo-Email': 'maya.thompson@kinevor.demo',
      'X-Demo-Member': '11111111-1111-1111-1111-111111111111',
    },
  );
  runApp(BillingDemoApp(repository: RemoteBillingRepository(client)));
}

/// Demo application hosting the member billing screens against the DemoApi.
class BillingDemoApp extends StatelessWidget {
  /// Creates the demo app.
  const BillingDemoApp({super.key, required this.repository});

  /// Data source used by the billing screens.
  final BillingRepository repository;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Kinevor Billing',
      debugShowCheckedModeBanner: false,
      theme: KinevorBillingTheme.dark(),
      home: InvoiceListScreen(
        repository: repository,
        memberName: 'Maya Thompson',
      ),
    );
  }
}
