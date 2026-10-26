/// Outstanding balance summary for the authenticated member.
class BillingSummary {
  /// Creates a billing summary.
  const BillingSummary({
    required this.memberId,
    required this.outstandingAmount,
    required this.overdueAmount,
    required this.openInvoiceCount,
    required this.overdueInvoiceCount,
    required this.paidThisYear,
    required this.currency,
    this.nextDueAt,
  });

  /// Member these figures belong to.
  final String memberId;

  /// Total unpaid balance.
  final double outstandingAmount;

  /// Portion of the balance that is past due.
  final double overdueAmount;

  /// Number of open invoices.
  final int openInvoiceCount;

  /// Number of overdue invoices.
  final int overdueInvoiceCount;

  /// Amount paid during the current year.
  final double paidThisYear;

  /// Next invoice due date, when one exists.
  final DateTime? nextDueAt;

  /// Currency code used by every amount.
  final String currency;

  /// Whether the member has no unpaid invoices.
  bool get isSettled => outstandingAmount <= 0;

  /// Parses a defensive summary payload.
  factory BillingSummary.fromJson(Map<String, dynamic> json) => BillingSummary(
    memberId: json['memberId']?.toString() ?? '',
    outstandingAmount: _decimal(json['outstandingAmount']),
    overdueAmount: _decimal(json['overdueAmount']),
    openInvoiceCount: _integer(json['openInvoiceCount']),
    overdueInvoiceCount: _integer(json['overdueInvoiceCount']),
    paidThisYear: _decimal(json['paidThisYear']),
    nextDueAt: _optionalDateTime(json['nextDueAt']),
    currency: json['currency']?.toString() ?? 'USD',
  );
}

/// Compact invoice row shown in the member ledger.
class InvoiceSummary {
  /// Creates an invoice summary.
  const InvoiceSummary({
    required this.id,
    required this.number,
    required this.memberId,
    required this.memberName,
    required this.status,
    required this.issuedAt,
    required this.dueAt,
    required this.subTotal,
    required this.taxAmount,
    required this.total,
    required this.amountPaid,
    required this.outstandingAmount,
    required this.currency,
    required this.lineCount,
    this.memberEmail,
  });

  /// Stable invoice identifier.
  final String id;

  /// Human-readable invoice number.
  final String number;

  /// Member identifier.
  final String memberId;

  /// Member display name.
  final String memberName;

  /// Optional member e-mail address.
  final String? memberEmail;

  /// Lifecycle status, for example `Open` or `Paid`.
  final String status;

  /// Date the invoice was issued.
  final DateTime issuedAt;

  /// Date payment is due.
  final DateTime dueAt;

  /// Amount before tax.
  final double subTotal;

  /// Tax amount.
  final double taxAmount;

  /// Total invoice amount.
  final double total;

  /// Amount paid so far.
  final double amountPaid;

  /// Remaining amount due.
  final double outstandingAmount;

  /// Currency code.
  final String currency;

  /// Number of line items.
  final int lineCount;

  /// Whether the invoice can still accept a payment.
  bool get isPayable =>
      (status.toLowerCase() == 'open' || status.toLowerCase() == 'overdue') &&
      outstandingAmount > 0;

  /// Parses a defensive invoice summary payload.
  factory InvoiceSummary.fromJson(Map<String, dynamic> json) => InvoiceSummary(
    id: json['id']?.toString() ?? '',
    number: json['number']?.toString() ?? '',
    memberId: json['memberId']?.toString() ?? '',
    memberName: json['memberName']?.toString() ?? '',
    memberEmail: json['memberEmail']?.toString(),
    status: json['status']?.toString() ?? 'Open',
    issuedAt: _dateTime(json['issuedAt']),
    dueAt: _dateTime(json['dueAt']),
    subTotal: _decimal(json['subTotal']),
    taxAmount: _decimal(json['taxAmount']),
    total: _decimal(json['total']),
    amountPaid: _decimal(json['amountPaid']),
    outstandingAmount: _decimal(json['outstandingAmount']),
    currency: json['currency']?.toString() ?? 'USD',
    lineCount: _integer(json['lineCount']),
  );
}

/// One line item on an invoice.
class InvoiceLine {
  /// Creates an invoice line.
  const InvoiceLine({
    required this.id,
    required this.description,
    required this.quantity,
    required this.unitPrice,
    required this.taxRate,
    required this.subtotal,
    required this.taxAmount,
    required this.total,
  });

  /// Stable line identifier.
  final String id;

  /// Description of the charge.
  final String description;

  /// Quantity billed.
  final int quantity;

  /// Price per unit.
  final double unitPrice;

  /// Tax rate as a fraction, for example `0.05`.
  final double taxRate;

  /// Subtotal before tax.
  final double subtotal;

  /// Tax amount.
  final double taxAmount;

  /// Line total.
  final double total;

  /// Parses a defensive line payload.
  factory InvoiceLine.fromJson(Map<String, dynamic> json) => InvoiceLine(
    id: json['id']?.toString() ?? '',
    description: json['description']?.toString() ?? '',
    quantity: _integer(json['quantity']),
    unitPrice: _decimal(json['unitPrice']),
    taxRate: _decimal(json['taxRate']),
    subtotal: _decimal(json['subtotal']),
    taxAmount: _decimal(json['taxAmount']),
    total: _decimal(json['total']),
  );
}

/// Full invoice projection including line items.
class InvoiceDetail extends InvoiceSummary {
  /// Creates an invoice detail.
  const InvoiceDetail({
    required super.id,
    required super.number,
    required super.memberId,
    required super.memberName,
    required super.status,
    required super.issuedAt,
    required super.dueAt,
    required super.subTotal,
    required super.taxAmount,
    required super.total,
    required super.amountPaid,
    required super.outstandingAmount,
    required super.currency,
    required super.lineCount,
    required this.lines,
    super.memberEmail,
    this.paidAt,
    this.voidedAt,
    this.voidReason,
    this.notes,
  });

  /// When the invoice was fully paid.
  final DateTime? paidAt;

  /// When the invoice was voided.
  final DateTime? voidedAt;

  /// Reason recorded for the void.
  final String? voidReason;

  /// Invoice notes.
  final String? notes;

  /// Line items on the invoice.
  final List<InvoiceLine> lines;

  /// Parses a defensive invoice detail payload.
  factory InvoiceDetail.fromJson(Map<String, dynamic> json) => InvoiceDetail(
    id: json['id']?.toString() ?? '',
    number: json['number']?.toString() ?? '',
    memberId: json['memberId']?.toString() ?? '',
    memberName: json['memberName']?.toString() ?? '',
    memberEmail: json['memberEmail']?.toString(),
    status: json['status']?.toString() ?? 'Open',
    issuedAt: _dateTime(json['issuedAt']),
    dueAt: _dateTime(json['dueAt']),
    paidAt: _optionalDateTime(json['paidAt']),
    voidedAt: _optionalDateTime(json['voidedAt']),
    voidReason: json['voidReason']?.toString(),
    notes: json['notes']?.toString(),
    subTotal: _decimal(json['subTotal']),
    taxAmount: _decimal(json['taxAmount']),
    total: _decimal(json['total']),
    amountPaid: _decimal(json['amountPaid']),
    outstandingAmount: _decimal(json['outstandingAmount']),
    currency: json['currency']?.toString() ?? 'USD',
    lines: _list(json['lines'], InvoiceLine.fromJson),
    lineCount: json['lineCount'] == null
        ? _list(json['lines'], InvoiceLine.fromJson).length
        : _integer(json['lineCount']),
  );
}

/// Payment projection shown in the member's invoice history.
class MemberPayment {
  /// Creates a payment projection.
  const MemberPayment({
    required this.id,
    required this.reference,
    required this.memberId,
    required this.memberName,
    required this.description,
    required this.method,
    required this.status,
    required this.amount,
    required this.refundedAmount,
    required this.netAmount,
    required this.currency,
    required this.paidAt,
    required this.createdAt,
    this.invoiceId,
    this.invoiceNumber,
    this.refundedAt,
  });

  /// Stable payment identifier.
  final String id;

  /// Human-readable payment reference.
  final String reference;

  /// Member identifier.
  final String memberId;

  /// Member display name.
  final String memberName;

  /// Payment description.
  final String description;

  /// Method, for example `Card`.
  final String method;

  /// Payment lifecycle status.
  final String status;

  /// Captured amount.
  final double amount;

  /// Amount already refunded.
  final double refundedAmount;

  /// Amount retained after refunds.
  final double netAmount;

  /// Currency code.
  final String currency;

  /// Optional linked invoice.
  final String? invoiceId;

  /// Optional linked invoice number.
  final String? invoiceNumber;

  /// Capture timestamp.
  final DateTime paidAt;

  /// Refund timestamp, when fully refunded.
  final DateTime? refundedAt;

  /// Creation timestamp.
  final DateTime createdAt;

  /// Parses a defensive payment payload.
  factory MemberPayment.fromJson(Map<String, dynamic> json) => MemberPayment(
    id: json['id']?.toString() ?? '',
    reference: json['reference']?.toString() ?? '',
    memberId: json['memberId']?.toString() ?? '',
    memberName: json['memberName']?.toString() ?? '',
    invoiceId: json['invoiceId']?.toString(),
    invoiceNumber: json['invoiceNumber']?.toString(),
    description: json['description']?.toString() ?? '',
    method: json['method']?.toString() ?? '',
    status: json['status']?.toString() ?? '',
    amount: _decimal(json['amount']),
    refundedAmount: _decimal(json['refundedAmount']),
    netAmount: _decimal(json['netAmount']),
    currency: json['currency']?.toString() ?? 'USD',
    paidAt: _dateTime(json['paidAt']),
    refundedAt: _optionalDateTime(json['refundedAt']),
    createdAt: _dateTime(json['createdAt']),
  );
}

/// Result of paying an invoice.
class PaymentRecorded {
  /// Creates a payment result.
  const PaymentRecorded({
    required this.id,
    required this.reference,
    required this.status,
  });

  /// Stable payment identifier.
  final String id;

  /// Generated or supplied payment reference.
  final String reference;

  /// Payment status.
  final String status;

  /// Parses a defensive payment result.
  factory PaymentRecorded.fromJson(Map<String, dynamic> json) =>
      PaymentRecorded(
        id: json['id']?.toString() ?? '',
        reference: json['reference']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
      );
}

double _decimal(Object? value) => switch (value) {
  num number => number.toDouble(),
  String text => double.tryParse(text) ?? 0,
  _ => 0,
};

int _integer(Object? value) => switch (value) {
  num number => number.toInt(),
  String text => int.tryParse(text) ?? 0,
  _ => 0,
};

DateTime _dateTime(Object? value) => _optionalDateTime(value) ?? DateTime(1970);

DateTime? _optionalDateTime(Object? value) =>
    DateTime.tryParse(value?.toString() ?? '');

List<T> _list<T>(Object? value, T Function(Map<String, dynamic>) parse) {
  if (value is! List) return const [];
  return [
    for (final item in value)
      if (item is Map) parse(Map<String, dynamic>.from(item)),
  ];
}
