# Kinevor Payments & Billing Flutter

Member invoice ledger and simulated payment UI for the Kinevor gym platform. The
package talks to the Kinevor Payments & Billing API through a typed JSON client,
but every screen depends only on `BillingRepository`, so hosts can substitute
their own transport or cached data source.

## Features

- Balance hero with outstanding and overdue totals plus the next due date.
- Invoice filters for all, open, overdue and paid states.
- Invoice cards with status, issued/due dates, totals and amount outstanding.
- Invoice detail with line items, tax, totals, notes, payment history and a
  sticky pay action for payable invoices.
- Simulated payment sheet with amount, `Card`, `Cash` or `BankTransfer` method and
  optional reference.
- "Architectural Obsidian" dark theme, Sora typography and status icons that do
  not rely on color alone.
- Injectable strings, repository, base URL and demo headers.
- Golden tests at 390x844 for the member ledger and detail screen.

## Install

```yaml
dependencies:
  kinevor_payments_billing:
    git:
      url: https://github.com/SoftwareDeveloper98/kinevor-payments-billing.git
      path: mobile/packages/kinevor_payments_billing
      ref: v0.1.0
```

## Use

```dart
import 'package:flutter/material.dart';
import 'package:kinevor_payments_billing/kinevor_payments_billing.dart';

void main() {
  final client = KinevorBillingClient(
    baseUrl: Uri.parse('https://api.example.com/api/v1'),
    headers: {'Authorization': 'Bearer $token'},
  );

  runApp(
    MaterialApp(
      theme: KinevorBillingTheme.dark(),
      home: InvoiceListScreen(
        repository: RemoteBillingRepository(client),
        memberName: 'Maya Thompson',
      ),
    ),
  );
}
```

The host supplies authentication headers, routing and any app shell. The package
renders the screens; it does not own navigation beyond invoice detail.

### Custom data source

Implement `BillingRepository` to connect a cache, another HTTP stack or a test
fixture:

```dart
class MyBillingRepository implements BillingRepository {
  @override
  Future<BillingSummary> getSummary() async => ...;

  @override
  Future<List<InvoiceSummary>> getInvoices({String? status}) async => ...;

  @override
  Future<InvoiceDetail> getInvoice(String id) async => ...;

  @override
  Future<List<MemberPayment>> getPayments() async => ...;

  @override
  Future<PaymentRecorded> payInvoice(
    String id, {
    double? amount,
    required String method,
    String? reference,
  }) async => ...;
}
```

### Labels and branding

Pass a custom `BillingStrings` to either screen. The theme is available as
`KinevorBillingTheme.dark(fontFamily: ...)` and exposes the shared palette
constants for host chrome.

## API client

`KinevorBillingClient` calls:

- `GET /me/invoices/summary`
- `GET /me/invoices?status=&page=&pageSize=`
- `GET /me/invoices/{id}`
- `GET /me/payments?page=&pageSize=`
- `POST /me/invoices/{id}/pay`

It unwraps the platform `{ success, data, message, errors }` envelope and throws
`BillingApiException` with the server message and status code on failure.

## Demo

The example authenticates as Maya Thompson against the standalone API:

```powershell
cd example
flutter run -d windows
```

The default base URL is `http://localhost:5087/api/v1`. Override it with:

```powershell
flutter run -d chrome `
  --dart-define=KINEVOR_API_BASE_URL=http://localhost:5087/api/v1
```

The example sends the development-only `X-Demo-Role`, `X-Demo-Member` and
`X-Demo-Email` headers. Never use those headers in production.

## Test

```powershell
flutter pub get
flutter analyze
flutter test
```

Regenerate the golden images after an intentional visual change:

```powershell
flutter test --update-goldens test\billing_golden_test.dart
```

The suite covers model parsing, money formatting, the HTTP client, error paths,
both screens, the payment sheet and the goldens.

## Limitations

- Payments are simulated; no card data or real gateway is involved.
- The package is member-facing; staff invoice management lives in the Angular
  library.
- Refunds are not exposed in the Flutter flow.
- Amounts are formatted for display with the currency code supplied by the API;
  the demo backend reports in USD.
