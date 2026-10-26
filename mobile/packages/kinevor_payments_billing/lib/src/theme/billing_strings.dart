/// User-facing labels used by the billing widgets.
class BillingStrings {
  /// Creates a label bundle.
  const BillingStrings({
    this.brand = 'Kinevor Billing',
    this.title = 'Billing',
    this.eyebrow = 'YOUR LEDGER',
    this.balanceDue = 'Balance due',
    this.allSettled = 'All settled',
    this.openInvoices = 'Open invoices',
    this.overdue = 'Overdue',
    this.paidThisYear = 'Paid this year',
    this.nextDue = 'Next due',
    this.invoicesTitle = 'Invoices',
    this.filterAll = 'All',
    this.filterOpen = 'Open',
    this.filterOverdue = 'Overdue',
    this.filterPaid = 'Paid',
    this.noInvoices = 'No invoices here',
    this.noInvoicesBody = 'Invoices matching this filter will appear here.',
    this.issued = 'Issued',
    this.due = 'Due',
    this.total = 'Total',
    this.outstanding = 'Outstanding',
    this.invoiceDetails = 'Invoice details',
    this.invoice = 'INVOICE',
    this.paymentHistory = 'Payment history',
    this.noPayments = 'No payments recorded for this invoice.',
    this.lineItems = 'Line items',
    this.description = 'Description',
    this.quantity = 'Qty',
    this.subtotal = 'Subtotal',
    this.tax = 'Tax',
    this.paid = 'Paid',
    this.notes = 'Notes',
    this.voidReason = 'Void reason',
    this.payInvoice = 'Pay invoice',
    this.payTitle = 'Pay this invoice',
    this.payBody =
        'This is a simulated payment. No real card or bank account is charged.',
    this.amount = 'Amount',
    this.method = 'Method',
    this.referenceOptional = 'Reference (optional)',
    this.payNow = 'Pay now',
    this.paymentRecorded = 'Payment recorded.',
    this.loadError = 'Could not load your billing details.',
    this.retry = 'Try again',
    this.actionFailed = 'The request could not be completed.',
    this.paidBanner = 'This invoice is fully paid.',
    this.voidBanner = 'This invoice was voided.',
    this.partlyPaid = 'Part paid',
    this.processing = 'Processing payment…',
    this.amountInvalid = 'Enter an amount within the balance due.',
    this.daysLate = 'days past due',
  });

  /// Small brand label.
  final String brand;

  /// App bar title.
  final String title;

  /// Eyebrow above the balance.
  final String eyebrow;

  /// Label for the amount owed.
  final String balanceDue;

  /// Label when nothing is owed.
  final String allSettled;

  /// Open invoice count label.
  final String openInvoices;

  /// Overdue label.
  final String overdue;

  /// Paid-this-year label.
  final String paidThisYear;

  /// Next-due label.
  final String nextDue;

  /// Invoice list title.
  final String invoicesTitle;

  /// All-invoices filter.
  final String filterAll;

  /// Open-invoices filter.
  final String filterOpen;

  /// Overdue filter.
  final String filterOverdue;

  /// Paid filter.
  final String filterPaid;

  /// Empty list title.
  final String noInvoices;

  /// Empty list body.
  final String noInvoicesBody;

  /// Issue date label.
  final String issued;

  /// Due date label.
  final String due;

  /// Invoice total label.
  final String total;

  /// Outstanding amount label.
  final String outstanding;

  /// Detail screen title.
  final String invoiceDetails;

  /// Invoice eyebrow.
  final String invoice;

  /// Payment history section title.
  final String paymentHistory;

  /// Empty payment history message.
  final String noPayments;

  /// Line items section title.
  final String lineItems;

  /// Description column label.
  final String description;

  /// Quantity label.
  final String quantity;

  /// Subtotal label.
  final String subtotal;

  /// Tax label.
  final String tax;

  /// Paid amount label.
  final String paid;

  /// Notes section title.
  final String notes;

  /// Void reason section title.
  final String voidReason;

  /// Pay button label.
  final String payInvoice;

  /// Payment sheet title.
  final String payTitle;

  /// Payment sheet explanation.
  final String payBody;

  /// Amount field label.
  final String amount;

  /// Payment method label.
  final String method;

  /// Optional reference label.
  final String referenceOptional;

  /// Payment confirmation action.
  final String payNow;

  /// Success message after a payment.
  final String paymentRecorded;

  /// Initial load failure.
  final String loadError;

  /// Retry action.
  final String retry;

  /// Generic action failure.
  final String actionFailed;

  /// Banner for a paid invoice.
  final String paidBanner;

  /// Banner for a void invoice.
  final String voidBanner;

  /// Label when an invoice is partly paid.
  final String partlyPaid;

  /// Busy label while paying.
  final String processing;

  /// Validation message for an invalid amount.
  final String amountInvalid;

  /// Suffix shown for past-due invoices.
  final String daysLate;

  /// Formats [amount] with the [currency] symbol.
  String money(String currency, double amount) {
    final fixed = amount.abs().toStringAsFixed(2);
    final parts = fixed.split('.');
    final grouped = parts.first.replaceAllMapped(
      RegExp(r'\B(?=(\d{3})+(?!\d))'),
      (_) => ',',
    );
    final symbol = switch (currency.toUpperCase()) {
      'USD' => r'$',
      'EUR' => '€',
      'GBP' => '£',
      _ => '$currency ',
    };
    return '${amount < 0 ? '-' : ''}$symbol$grouped.${parts.last}';
  }

  /// Formats a full date.
  String date(DateTime value) =>
      '${_months[value.month - 1]} ${value.day}, ${value.year}';

  /// Formats a month and day.
  String monthDay(DateTime value) => '${_months[value.month - 1]} ${value.day}';

  /// Formats an invoice count.
  String invoiceCount(int count) =>
      count == 1 ? '1 invoice' : '$count invoices';

  /// Formats a payment count.
  String paymentCount(int count) =>
      count == 1 ? '1 payment' : '$count payments';

  /// Human label for an invoice or payment status.
  String statusLabel(String status) => switch (status.toLowerCase()) {
    'partiallyrefunded' => 'Part refunded',
    'banktransfer' => 'Bank transfer',
    'void' => 'Void',
    'open' => 'Open',
    'overdue' => 'Overdue',
    'paid' => 'Paid',
    'pending' => 'Pending',
    'failed' => 'Failed',
    'refunded' => 'Refunded',
    _ => status,
  };

  /// Human label for a payment method.
  String methodLabel(String method) => switch (method) {
    'BankTransfer' => 'Bank transfer',
    'Card' => 'Card',
    'Cash' => 'Cash',
    _ => method,
  };

  static const List<String> _months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
}
