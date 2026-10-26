import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../data/billing_api_exception.dart';
import '../data/billing_repository.dart';
import '../models/billing_models.dart';
import '../theme/billing_strings.dart';
import '../theme/kinevor_billing_theme.dart';
import '../widgets/billing_shared.dart';

/// Invoice detail with line items, totals, history, and simulated payment.
class InvoiceDetailScreen extends StatefulWidget {
  /// Creates an invoice detail screen.
  const InvoiceDetailScreen({
    super.key,
    required this.repository,
    required this.invoiceId,
    this.strings = const BillingStrings(),
  });

  /// Billing data source.
  final BillingRepository repository;

  /// Invoice to display.
  final String invoiceId;

  /// Labels used by the screen.
  final BillingStrings strings;

  @override
  State<InvoiceDetailScreen> createState() => _InvoiceDetailScreenState();
}

class _InvoiceDetailScreenState extends State<InvoiceDetailScreen> {
  InvoiceDetail? _invoice;
  List<MemberPayment> _payments = const [];
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    if (_invoice != null) setState(() => _loading = true);
    try {
      final invoice = await widget.repository.getInvoice(widget.invoiceId);
      final payments = await widget.repository.getPayments();
      if (!mounted) return;
      setState(() {
        _invoice = invoice;
        _payments =
            payments
                .where((payment) => payment.invoiceId == invoice.id)
                .toList()
              ..sort((a, b) => b.paidAt.compareTo(a.paidAt));
        _error = null;
        _loading = false;
      });
    } on Object catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error;
        _loading = false;
      });
    }
  }

  Future<void> _pay(InvoiceDetail invoice) async {
    final paid = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (sheetContext) => _PaymentSheet(
        repository: widget.repository,
        invoice: invoice,
        strings: widget.strings,
      ),
    );
    if (!mounted || paid != true) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(widget.strings.paymentRecorded)));
    await _load();
  }

  @override
  Widget build(BuildContext context) {
    final invoice = _invoice;
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.strings.invoiceDetails),
        leading: IconButton(
          tooltip: 'Back',
          onPressed: () => Navigator.of(context).maybePop(),
          icon: const Icon(Icons.arrow_back_rounded),
        ),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh_rounded),
          ),
          const SizedBox(width: 6),
        ],
      ),
      body: SafeArea(
        bottom: invoice?.isPayable ?? false,
        child: invoice == null
            ? (_error != null
                  ? BillingErrorView(
                      message: widget.strings.loadError,
                      retryLabel: widget.strings.retry,
                      onRetry: _load,
                    )
                  : const Center(child: CircularProgressIndicator()))
            : RefreshIndicator(
                onRefresh: _load,
                color: KinevorBillingTheme.accent,
                child: ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
                  children: [
                    _InvoiceHero(invoice: invoice, strings: widget.strings),
                    if (invoice.notes != null &&
                        invoice.notes!.trim().isNotEmpty) ...[
                      const SizedBox(height: 12),
                      BillingSectionCard(
                        title: widget.strings.notes,
                        child: Text(
                          invoice.notes!,
                          style: const TextStyle(
                            color: KinevorBillingTheme.textMuted,
                            fontSize: 12,
                            height: 1.55,
                          ),
                        ),
                      ),
                    ],
                    if (invoice.voidReason != null &&
                        invoice.voidReason!.trim().isNotEmpty) ...[
                      const SizedBox(height: 12),
                      BillingSectionCard(
                        title: widget.strings.voidReason,
                        child: Text(
                          invoice.voidReason!,
                          style: const TextStyle(
                            color: KinevorBillingTheme.error,
                            fontSize: 12,
                            height: 1.55,
                          ),
                        ),
                      ),
                    ],
                    const SizedBox(height: 16),
                    _LineItemsCard(invoice: invoice, strings: widget.strings),
                    const SizedBox(height: 12),
                    _TotalsCard(invoice: invoice, strings: widget.strings),
                    const SizedBox(height: 16),
                    _PaymentHistoryCard(
                      payments: _payments,
                      strings: widget.strings,
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: 12),
                      Text(
                        widget.strings.loadError,
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          color: KinevorBillingTheme.error,
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
      ),
      bottomNavigationBar: invoice != null && invoice.isPayable
          ? _PayBar(
              invoice: invoice,
              strings: widget.strings,
              busy: _loading,
              onPay: () => _pay(invoice),
            )
          : null,
    );
  }
}

class _InvoiceHero extends StatelessWidget {
  const _InvoiceHero({required this.invoice, required this.strings});

  final InvoiceDetail invoice;
  final BillingStrings strings;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final overdue = invoice.status.toLowerCase() == 'overdue';
    final paid = invoice.status.toLowerCase() == 'paid';
    final voided = invoice.status.toLowerCase() == 'void';
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        border: Border.all(
          color: KinevorBillingTheme.statusColor(
            invoice.status,
          ).withValues(alpha: 0.32),
        ),
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            KinevorBillingTheme.statusColor(
              invoice.status,
            ).withValues(alpha: 0.15),
            KinevorBillingTheme.surfaceRaised,
          ],
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                strings.invoice,
                style: const TextStyle(
                  color: KinevorBillingTheme.accent,
                  fontSize: 9,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 1.7,
                ),
              ),
              const Spacer(),
              BillingStatusChip(status: invoice.status, strings: strings),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            invoice.number,
            style: theme.textTheme.headlineMedium?.copyWith(
              fontWeight: FontWeight.w800,
              letterSpacing: 0.2,
            ),
          ),
          const SizedBox(height: 18),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      paid ? strings.total : strings.outstanding,
                      style: const TextStyle(
                        color: KinevorBillingTheme.textMuted,
                        fontSize: 11,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 2),
                    FittedBox(
                      fit: BoxFit.scaleDown,
                      alignment: Alignment.centerLeft,
                      child: Text(
                        strings.money(
                          invoice.currency,
                          paid ? invoice.total : invoice.outstandingAmount,
                        ),
                        style: TextStyle(
                          color: paid
                              ? KinevorBillingTheme.success
                              : KinevorBillingTheme.text,
                          fontSize: 34,
                          fontWeight: FontWeight.w800,
                          height: 1.15,
                          letterSpacing: -1,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              if (invoice.amountPaid > 0 && !paid)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: KinevorBillingTheme.success.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(9),
                  ),
                  child: Text(
                    '${strings.paid} ${strings.money(invoice.currency, invoice.amountPaid)}',
                    style: const TextStyle(
                      color: KinevorBillingTheme.success,
                      fontSize: 10,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.symmetric(vertical: 11),
            decoration: const BoxDecoration(
              border: Border(
                top: BorderSide(color: KinevorBillingTheme.border),
              ),
            ),
            child: Row(
              children: [
                Expanded(
                  child: _dateBlock(
                    label: strings.issued,
                    value: strings.date(invoice.issuedAt),
                  ),
                ),
                Container(
                  width: 1,
                  height: 26,
                  color: KinevorBillingTheme.border,
                ),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(left: 16),
                    child: _dateBlock(
                      label: strings.due,
                      value: strings.date(invoice.dueAt),
                      color: overdue ? KinevorBillingTheme.warning : null,
                    ),
                  ),
                ),
              ],
            ),
          ),
          if (paid || voided) ...[
            const SizedBox(height: 10),
            Row(
              children: [
                Icon(
                  paid ? Icons.verified_outlined : Icons.block_outlined,
                  size: 14,
                  color: paid
                      ? KinevorBillingTheme.success
                      : KinevorBillingTheme.textMuted,
                ),
                const SizedBox(width: 6),
                Text(
                  paid ? strings.paidBanner : strings.voidBanner,
                  style: TextStyle(
                    color: paid
                        ? KinevorBillingTheme.success
                        : KinevorBillingTheme.textMuted,
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Widget _dateBlock({
    required String label,
    required String value,
    Color? color,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label.toUpperCase(),
          style: const TextStyle(
            color: KinevorBillingTheme.textMuted,
            fontSize: 8,
            fontWeight: FontWeight.w700,
            letterSpacing: 0.9,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
            color: color ?? KinevorBillingTheme.text,
            fontSize: 11.5,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

class _LineItemsCard extends StatelessWidget {
  const _LineItemsCard({required this.invoice, required this.strings});

  final InvoiceDetail invoice;
  final BillingStrings strings;

  @override
  Widget build(BuildContext context) {
    return BillingSectionCard(
      title: strings.lineItems,
      trailing: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(
          color: KinevorBillingTheme.surface,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(
          '${invoice.lines.length}',
          style: const TextStyle(
            color: KinevorBillingTheme.textMuted,
            fontSize: 10,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      child: Column(
        children: [
          for (var index = 0; index < invoice.lines.length; index++) ...[
            if (index > 0) const Divider(height: 21),
            _line(invoice.lines[index]),
          ],
        ],
      ),
    );
  }

  Widget _line(InvoiceLine line) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 7,
          height: 7,
          margin: const EdgeInsets.only(top: 5),
          decoration: BoxDecoration(
            color: KinevorBillingTheme.accent.withValues(alpha: 0.7),
            shape: BoxShape.circle,
          ),
        ),
        const SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                line.description,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: KinevorBillingTheme.text,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  height: 1.35,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                '${line.quantity} × ${strings.money(invoice.currency, line.unitPrice)}',
                style: const TextStyle(
                  color: KinevorBillingTheme.textMuted,
                  fontSize: 10,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(width: 10),
        Text(
          strings.money(invoice.currency, line.total),
          style: const TextStyle(
            color: KinevorBillingTheme.text,
            fontSize: 12,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    );
  }
}

class _TotalsCard extends StatelessWidget {
  const _TotalsCard({required this.invoice, required this.strings});

  final InvoiceDetail invoice;
  final BillingStrings strings;

  @override
  Widget build(BuildContext context) {
    return BillingSectionCard(
      child: Column(
        children: [
          BillingAmountRow(
            label: strings.subtotal,
            value: strings.money(invoice.currency, invoice.subTotal),
          ),
          BillingAmountRow(
            label: strings.tax,
            value: strings.money(invoice.currency, invoice.taxAmount),
          ),
          const Divider(height: 15),
          BillingAmountRow(
            label: strings.total,
            value: strings.money(invoice.currency, invoice.total),
          ),
          if (invoice.amountPaid > 0)
            BillingAmountRow(
              label: strings.paid,
              value: strings.money(invoice.currency, invoice.amountPaid),
              color: KinevorBillingTheme.success,
            ),
          const Divider(height: 15),
          BillingAmountRow(
            label: strings.outstanding,
            value: strings.money(invoice.currency, invoice.outstandingAmount),
            emphasized: true,
            color: invoice.outstandingAmount > 0
                ? KinevorBillingTheme.warning
                : KinevorBillingTheme.success,
          ),
        ],
      ),
    );
  }
}

class _PaymentHistoryCard extends StatelessWidget {
  const _PaymentHistoryCard({required this.payments, required this.strings});

  final List<MemberPayment> payments;
  final BillingStrings strings;

  @override
  Widget build(BuildContext context) {
    return BillingSectionCard(
      title: strings.paymentHistory,
      trailing: Text(
        strings.paymentCount(payments.length),
        style: const TextStyle(
          color: KinevorBillingTheme.textMuted,
          fontSize: 10,
          fontWeight: FontWeight.w600,
        ),
      ),
      child: payments.isEmpty
          ? Padding(
              padding: const EdgeInsets.symmetric(vertical: 14),
              child: Row(
                children: [
                  const Icon(
                    Icons.hourglass_empty_rounded,
                    size: 15,
                    color: KinevorBillingTheme.textMuted,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      strings.noPayments,
                      style: const TextStyle(
                        color: KinevorBillingTheme.textMuted,
                        fontSize: 11.5,
                      ),
                    ),
                  ),
                ],
              ),
            )
          : Column(
              children: [
                for (var index = 0; index < payments.length; index++) ...[
                  if (index > 0) const Divider(height: 21),
                  _payment(payments[index]),
                ],
              ],
            ),
    );
  }

  Widget _payment(MemberPayment payment) {
    return Row(
      children: [
        Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            color: KinevorBillingTheme.surface,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: KinevorBillingTheme.border),
          ),
          child: Icon(
            KinevorBillingTheme.paymentMethodIcon(payment.method),
            size: 16,
            color: KinevorBillingTheme.textMuted,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                payment.reference,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: KinevorBillingTheme.text,
                  fontSize: 11.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                '${strings.methodLabel(payment.method)} · ${strings.date(payment.paidAt)}',
                style: const TextStyle(
                  color: KinevorBillingTheme.textMuted,
                  fontSize: 9.5,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(width: 8),
        Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              strings.money(payment.currency, payment.amount),
              style: const TextStyle(
                color: KinevorBillingTheme.text,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 2),
            BillingStatusChip(
              status: payment.status,
              strings: strings,
              compact: true,
            ),
          ],
        ),
      ],
    );
  }
}

class _PayBar extends StatelessWidget {
  const _PayBar({
    required this.invoice,
    required this.strings,
    required this.busy,
    required this.onPay,
  });

  final InvoiceDetail invoice;
  final BillingStrings strings;
  final bool busy;
  final VoidCallback onPay;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      top: false,
      child: Container(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 12),
        decoration: const BoxDecoration(
          color: KinevorBillingTheme.ink,
          border: Border(top: BorderSide(color: KinevorBillingTheme.border)),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(
                    'BALANCE DUE',
                    style: TextStyle(
                      color: KinevorBillingTheme.textMuted,
                      fontSize: 8,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.9,
                    ),
                  ),
                  Text(
                    strings.money(invoice.currency, invoice.outstandingAmount),
                    style: const TextStyle(
                      color: KinevorBillingTheme.text,
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 14),
            FilledButton.icon(
              onPressed: busy ? null : onPay,
              icon: const Icon(Icons.credit_card_rounded, size: 17),
              label: Text(strings.payInvoice),
              style: FilledButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 18),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PaymentSheet extends StatefulWidget {
  const _PaymentSheet({
    required this.repository,
    required this.invoice,
    required this.strings,
  });

  final BillingRepository repository;
  final InvoiceDetail invoice;
  final BillingStrings strings;

  @override
  State<_PaymentSheet> createState() => _PaymentSheetState();
}

class _PaymentSheetState extends State<_PaymentSheet> {
  late final TextEditingController _amount = TextEditingController(
    text: widget.invoice.outstandingAmount.toStringAsFixed(2),
  );
  final TextEditingController _reference = TextEditingController();
  String _method = 'Card';
  String? _error;
  bool _busy = false;

  @override
  void dispose() {
    _amount.dispose();
    _reference.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final amount = double.tryParse(_amount.text.trim().replaceAll(',', '.'));
    if (amount == null ||
        amount <= 0 ||
        amount > widget.invoice.outstandingAmount + 0.001) {
      setState(() => _error = widget.strings.amountInvalid);
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await widget.repository.payInvoice(
        widget.invoice.id,
        amount: amount,
        method: _method,
        reference: _reference.text,
      );
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on BillingApiException catch (error) {
      if (!mounted) return;
      setState(() {
        _busy = false;
        _error = error.message;
      });
    } on Object {
      if (!mounted) return;
      setState(() {
        _busy = false;
        _error = widget.strings.actionFailed;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final methods = <String, String>{
      'Card': widget.strings.methodLabel('Card'),
      'Cash': widget.strings.methodLabel('Cash'),
      'BankTransfer': widget.strings.methodLabel('BankTransfer'),
    };
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        20,
        4,
        20,
        MediaQuery.viewInsetsOf(context).bottom + 20,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            widget.strings.payTitle,
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            widget.strings.payBody,
            style: theme.textTheme.bodySmall?.copyWith(
              color: KinevorBillingTheme.textMuted,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 18),
          TextField(
            controller: _amount,
            enabled: !_busy,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            inputFormatters: [
              FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
            ],
            decoration: InputDecoration(
              labelText: widget.strings.amount,
              prefixText: widget.invoice.currency == 'USD' ? r'$ ' : null,
            ),
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 14),
          Text(
            widget.strings.method.toUpperCase(),
            style: const TextStyle(
              color: KinevorBillingTheme.textMuted,
              fontSize: 8,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.9,
            ),
          ),
          const SizedBox(height: 7),
          Row(
            children: [
              for (final entry in methods.entries) ...[
                if (entry.key != methods.keys.first) const SizedBox(width: 7),
                Expanded(
                  child: _MethodTile(
                    label: entry.value,
                    icon: KinevorBillingTheme.paymentMethodIcon(entry.key),
                    selected: _method == entry.key,
                    onTap: _busy
                        ? null
                        : () => setState(() => _method = entry.key),
                  ),
                ),
              ],
            ],
          ),
          const SizedBox(height: 14),
          TextField(
            controller: _reference,
            enabled: !_busy,
            textCapitalization: TextCapitalization.characters,
            decoration: InputDecoration(
              labelText: widget.strings.referenceOptional,
              prefixIcon: const Icon(Icons.tag_rounded, size: 17),
            ),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(
                  Icons.error_outline,
                  size: 15,
                  color: KinevorBillingTheme.error,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    _error!,
                    style: const TextStyle(
                      color: KinevorBillingTheme.error,
                      fontSize: 11.5,
                    ),
                  ),
                ),
              ],
            ),
          ],
          const SizedBox(height: 18),
          FilledButton(
            onPressed: _busy ? null : _submit,
            child: _busy
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: KinevorBillingTheme.onAccent,
                    ),
                  )
                : Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.lock_outline_rounded, size: 16),
                      const SizedBox(width: 7),
                      Text(widget.strings.payNow),
                    ],
                  ),
          ),
          const SizedBox(height: 10),
          Center(
            child: Text(
              widget.strings.payBody,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: KinevorBillingTheme.textMuted,
                fontSize: 9.5,
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MethodTile extends StatelessWidget {
  const _MethodTile({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        padding: const EdgeInsets.symmetric(vertical: 11, horizontal: 5),
        decoration: BoxDecoration(
          color: selected
              ? KinevorBillingTheme.accent.withValues(alpha: 0.13)
              : KinevorBillingTheme.surface,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: selected
                ? KinevorBillingTheme.accent
                : KinevorBillingTheme.border,
          ),
        ),
        child: Column(
          children: [
            Icon(
              icon,
              size: 18,
              color: selected
                  ? KinevorBillingTheme.accent
                  : KinevorBillingTheme.textMuted,
            ),
            const SizedBox(height: 4),
            FittedBox(
              fit: BoxFit.scaleDown,
              child: Text(
                label,
                maxLines: 1,
                style: TextStyle(
                  color: selected
                      ? KinevorBillingTheme.text
                      : KinevorBillingTheme.textMuted,
                  fontSize: 9.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
