import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/billing_models.dart';
import 'billing_api_exception.dart';

/// HTTP client for the Kinevor member billing API.
class KinevorBillingClient {
  /// Creates a client rooted at [baseUrl], for example
  /// `https://api.example.com/api/v1`.
  KinevorBillingClient({
    required Uri baseUrl,
    http.Client? httpClient,
    Map<String, String> headers = const {},
  }) : _baseUrl = baseUrl,
       _http = httpClient ?? http.Client(),
       _ownsClient = httpClient == null,
       _headers = Map.unmodifiable(headers);

  final Uri _baseUrl;
  final http.Client _http;
  final bool _ownsClient;
  final Map<String, String> _headers;

  /// Loads the member's outstanding balance summary.
  Future<BillingSummary> fetchSummary() async {
    final data = await _object('GET', 'me/invoices/summary');
    return BillingSummary.fromJson(data);
  }

  /// Loads the member's invoices, optionally filtered by [status].
  Future<List<InvoiceSummary>> fetchInvoices({String? status}) async {
    final data = await _object(
      'GET',
      'me/invoices',
      query: {
        'page': '1',
        'pageSize': '50',
        if (status != null && status.isNotEmpty) 'status': status,
      },
    );
    final items = data['items'];
    if (items is! List) {
      throw const BillingApiException(
        'The billing response has no invoice items.',
      );
    }
    return [
      for (final item in items)
        if (item is Map)
          InvoiceSummary.fromJson(Map<String, dynamic>.from(item)),
    ];
  }

  /// Loads one invoice including its line items.
  Future<InvoiceDetail> fetchInvoice(String id) async {
    final data = await _object('GET', 'me/invoices/$id');
    return InvoiceDetail.fromJson(data);
  }

  /// Loads the member's recent payments.
  Future<List<MemberPayment>> fetchPayments() async {
    final data = await _object(
      'GET',
      'me/payments',
      query: const {'page': '1', 'pageSize': '50'},
    );
    final items = data['items'];
    if (items is! List) {
      throw const BillingApiException(
        'The billing response has no payment items.',
      );
    }
    return [
      for (final item in items)
        if (item is Map)
          MemberPayment.fromJson(Map<String, dynamic>.from(item)),
    ];
  }

  /// Pays [amount] against an invoice with the simulated gateway.
  Future<PaymentRecorded> payInvoice(
    String id, {
    double? amount,
    required String method,
    String? reference,
  }) async {
    final data = await _object(
      'POST',
      'me/invoices/$id/pay',
      body: {
        'amount': ?amount,
        'method': method,
        if (reference != null && reference.trim().isNotEmpty)
          'reference': reference.trim(),
      },
    );
    return PaymentRecorded.fromJson(data);
  }

  /// Closes the underlying HTTP client when this instance created it.
  void close() {
    if (_ownsClient) _http.close();
  }

  Uri _endpoint(String path, [Map<String, String>? query]) {
    final base = _baseUrl.toString().replaceFirst(RegExp(r'/+$'), '');
    final uri = Uri.parse('$base/$path');
    if (query == null || query.isEmpty) return uri;
    return uri.replace(queryParameters: {...uri.queryParameters, ...query});
  }

  Future<Map<String, dynamic>> _object(
    String method,
    String path, {
    Map<String, String>? query,
    Map<String, dynamic>? body,
  }) async {
    final request = http.Request(method, _endpoint(path, query))
      ..headers.addAll({
        'Accept': 'application/json',
        if (body != null) 'Content-Type': 'application/json',
        ..._headers,
      });
    if (body != null) request.body = jsonEncode(body);

    final http.StreamedResponse streamed;
    try {
      streamed = await _http.send(request);
    } on Exception catch (error) {
      throw BillingApiException('Could not reach the billing API: $error');
    }

    final response = await http.Response.fromStream(streamed);
    final Object? decoded;
    try {
      decoded = response.body.isEmpty ? null : jsonDecode(response.body);
    } on FormatException {
      throw BillingApiException(
        'The billing API returned invalid JSON.',
        statusCode: response.statusCode,
      );
    }

    if (decoded is! Map<String, dynamic>) {
      throw BillingApiException(
        'The billing API returned an unexpected payload.',
        statusCode: response.statusCode,
      );
    }

    final success = decoded['success'] != false;
    if (response.statusCode < 200 || response.statusCode >= 300 || !success) {
      throw BillingApiException(
        _errorMessage(decoded) ?? 'The billing request failed.',
        statusCode: response.statusCode,
      );
    }

    final data = decoded['data'];
    if (data is Map<String, dynamic>) return data;
    if (data is Map) return Map<String, dynamic>.from(data);
    throw BillingApiException(
      'The billing response has no data object.',
      statusCode: response.statusCode,
    );
  }

  String? _errorMessage(Map<String, dynamic> body) {
    final message = body['message'];
    if (message is String && message.isNotEmpty) return message;
    final errors = body['errors'];
    if (errors is List) {
      for (final error in errors) {
        if (error is String && error.isNotEmpty) return error;
        if (error is Map && error['message'] is String) {
          return error['message'] as String;
        }
      }
    }
    final detail = body['detail'];
    return detail is String && detail.isNotEmpty ? detail : null;
  }
}
