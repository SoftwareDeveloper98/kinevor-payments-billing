import '../models/billing_models.dart';
import 'kinevor_billing_client.dart';

/// Data source for the member invoice screens.
abstract interface class BillingRepository {
  /// Loads the member's balance summary.
  Future<BillingSummary> getSummary();

  /// Loads invoices, optionally filtered by [status].
  Future<List<InvoiceSummary>> getInvoices({String? status});

  /// Loads one invoice with its line items.
  Future<InvoiceDetail> getInvoice(String id);

  /// Loads the member's recent payments.
  Future<List<MemberPayment>> getPayments();

  /// Pays an invoice with the simulated gateway.
  Future<PaymentRecorded> payInvoice(
    String id, {
    double? amount,
    required String method,
    String? reference,
  });
}

/// [BillingRepository] backed by [KinevorBillingClient].
class RemoteBillingRepository implements BillingRepository {
  /// Creates a repository over [client].
  const RemoteBillingRepository(this.client);

  /// HTTP client used for every call.
  final KinevorBillingClient client;

  @override
  Future<BillingSummary> getSummary() => client.fetchSummary();

  @override
  Future<List<InvoiceSummary>> getInvoices({String? status}) =>
      client.fetchInvoices(status: status);

  @override
  Future<InvoiceDetail> getInvoice(String id) => client.fetchInvoice(id);

  @override
  Future<List<MemberPayment>> getPayments() => client.fetchPayments();

  @override
  Future<PaymentRecorded> payInvoice(
    String id, {
    double? amount,
    required String method,
    String? reference,
  }) => client.payInvoice(
    id,
    amount: amount,
    method: method,
    reference: reference,
  );
}
