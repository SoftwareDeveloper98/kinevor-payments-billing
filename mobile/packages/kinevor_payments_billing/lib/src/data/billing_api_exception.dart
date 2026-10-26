/// Failure reported by the Kinevor payments and billing API.
class BillingApiException implements Exception {
  /// Creates an exception with a message and optional HTTP status.
  const BillingApiException(this.message, {this.statusCode});

  /// Human-readable failure.
  final String message;

  /// HTTP status code, when a response was received.
  final int? statusCode;

  @override
  String toString() => 'BillingApiException($statusCode): $message';
}
