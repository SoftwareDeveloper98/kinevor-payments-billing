import 'package:flutter/material.dart';

import '../data/billing_repository.dart';
import '../models/billing_models.dart';
import '../theme/billing_strings.dart';
import '../theme/kinevor_billing_theme.dart';
import '../widgets/billing_shared.dart';
import 'invoice_detail_screen.dart';

/// Member-facing ledger with balance summary, filters, and invoice cards.
class InvoiceListScreen extends StatefulWidget {
  /// Creates the member invoice list.
  const InvoiceListScreen({
    super.key,
    required this.repository,
    this.strings = const BillingStrings(),
    this.memberName,
  });

  /// Billing data source.
  final BillingRepository repository;

  /// Labels used by the screen.
  final BillingStrings strings;

  /// Optional member name rendered in the header.
  final String? memberName;

  @override
  State<InvoiceListScreen> createState() => _InvoiceListScreenState();
}

class _InvoiceListScreenState extends State<InvoiceListScreen> {
  BillingSummary? _summary;
  List<InvoiceSummary> _invoices = const [];
  String? _filter;
  Object? _error;
  bool _loading = true;
  bool _filtering = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load({bool showSpinner = true}) async {
    if (showSpinner) setState(() => _loading = true);
    try {
      final summary = await widget.repository.getSummary();
      final invoices = await widget.repository.getInvoices(status: _filter);
      if (!mounted) return;
      setState(() {
        _summary = summary;
        _invoices = invoices;
        _error = null;
        _loading = false;
        _filtering = false;
      });
    } on Object catch (error) {
      if (!mounted) return;
      setState(() {
        if (_summary == null) _error = error;
        _loading = false;
        _filtering = false;
      });
      if (_summary != null) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(widget.strings.loadError)));
      }
    }
  }

  Future<void> _selectFilter(String? filter) async {
    if (_filter == filter) return;
    setState(() {
      _filter = filter;
      _filtering = true;
    });
    try {
      final invoices = await widget.repository.getInvoices(status: filter);
      if (!mounted) return;
      setState(() {
        _invoices = invoices;
        _filtering = false;
      });
    } on Object {
      if (!mounted) return;
      setState(() => _filtering = false);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(widget.strings.loadError)));
    }
  }

  Future<void> _openInvoice(InvoiceSummary invoice) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute<void>(
        builder: (_) => InvoiceDetailScreen(
          repository: widget.repository,
          invoiceId: invoice.id,
          strings: widget.strings,
        ),
      ),
    );
    if (mounted) await _load(showSpinner: false);
  }

  @override
  Widget build(BuildContext context) {
    final summary = _summary;
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: _loading && summary == null
            ? const Center(child: CircularProgressIndicator())
            : _error != null && summary == null
            ? BillingErrorView(
                message: widget.strings.loadError,
                retryLabel: widget.strings.retry,
                onRetry: _load,
              )
            : RefreshIndicator(
                onRefresh: () => _load(showSpinner: false),
                color: KinevorBillingTheme.accent,
                child: ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(20, 18, 20, 32),
                  children: [
                    _Header(
                      strings: widget.strings,
                      memberName: widget.memberName,
                    ),
                    const SizedBox(height: 20),
                    if (summary != null)
                      _BalanceHero(summary: summary, strings: widget.strings),
                    const SizedBox(height: 12),
                    if (summary != null)
                      _StatsRow(summary: summary, strings: widget.strings),
                    const SizedBox(height: 26),
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            widget.strings.invoicesTitle,
                            style: Theme.of(context).textTheme.headlineSmall
                                ?.copyWith(fontWeight: FontWeight.w800),
                          ),
                        ),
                        Text(
                          widget.strings.invoiceCount(_invoices.length),
                          style: const TextStyle(
                            color: KinevorBillingTheme.textMuted,
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    _FilterBar(
                      selected: _filter,
                      strings: widget.strings,
                      onSelected: _selectFilter,
                    ),
                    AnimatedSize(
                      duration: const Duration(milliseconds: 180),
                      child: _filtering
                          ? const Padding(
                              padding: EdgeInsets.only(top: 10),
                              child: LinearProgressIndicator(minHeight: 2),
                            )
                          : const SizedBox(width: double.infinity),
                    ),
                    const SizedBox(height: 12),
                    if (_invoices.isEmpty)
                      BillingEmptyView(
                        title: widget.strings.noInvoices,
                        body: widget.strings.noInvoicesBody,
                      )
                    else
                      for (final invoice in _invoices) ...[
                        _InvoiceCard(
                          invoice: invoice,
                          strings: widget.strings,
                          onTap: () => _openInvoice(invoice),
                        ),
                        const SizedBox(height: 10),
                      ],
                  ],
                ),
              ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.strings, this.memberName});

  final BillingStrings strings;
  final String? memberName;

  @override
  Widget build(BuildContext context) {
    final initials = (memberName?.trim().isNotEmpty ?? false)
        ? memberName!
              .trim()
              .split(RegExp(r'\s+'))
              .take(2)
              .map((part) => part.characters.first.toUpperCase())
              .join()
        : 'KB';
    return Row(
      children: [
        Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            gradient: KinevorBillingTheme.brandGradient,
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(
            Icons.receipt_long_rounded,
            color: KinevorBillingTheme.onAccent,
            size: 20,
          ),
        ),
        const SizedBox(width: 11),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'KINEVOR',
                style: Theme.of(context).textTheme.labelSmall?.copyWith(
                  color: KinevorBillingTheme.textMuted,
                  letterSpacing: 2.2,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                strings.title,
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
              ),
            ],
          ),
        ),
        Container(
          width: 38,
          height: 38,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: KinevorBillingTheme.surfaceRaised,
            shape: BoxShape.circle,
            border: Border.all(color: KinevorBillingTheme.border),
          ),
          child: Text(
            initials,
            style: const TextStyle(
              color: KinevorBillingTheme.text,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    );
  }
}

class _BalanceHero extends StatelessWidget {
  const _BalanceHero({required this.summary, required this.strings});

  final BillingSummary summary;
  final BillingStrings strings;

  @override
  Widget build(BuildContext context) {
    final metric = _MetricOf.nextDue(summary, strings);
    final settled = summary.isSettled;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        border: Border.all(
          color: settled
              ? KinevorBillingTheme.success.withValues(alpha: 0.35)
              : KinevorBillingTheme.accent.withValues(alpha: 0.28),
        ),
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: settled
              ? [
                  KinevorBillingTheme.success.withValues(alpha: 0.14),
                  KinevorBillingTheme.surfaceRaised,
                ]
              : [
                  KinevorBillingTheme.accent.withValues(alpha: 0.2),
                  KinevorBillingTheme.surfaceRaised,
                ],
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 7,
                height: 7,
                decoration: BoxDecoration(
                  color: settled
                      ? KinevorBillingTheme.success
                      : KinevorBillingTheme.accent,
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 7),
              Text(
                settled ? 'ALL CLEAR' : 'YOUR LEDGER',
                style: TextStyle(
                  color: settled
                      ? KinevorBillingTheme.success
                      : KinevorBillingTheme.accent,
                  fontSize: 9,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 1.7,
                ),
              ),
              const Spacer(),
              Icon(
                settled
                    ? Icons.verified_outlined
                    : Icons.account_balance_wallet_outlined,
                color: settled
                    ? KinevorBillingTheme.success
                    : KinevorBillingTheme.accent,
                size: 19,
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(
            settled ? strings.allSettled : strings.balanceDue,
            style: const TextStyle(
              color: KinevorBillingTheme.textMuted,
              fontSize: 12,
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 2),
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(
              settled
                  ? 'No balance'
                  : '${_currencySymbol(summary.currency)}${_amount(summary.outstandingAmount)}',
              style: const TextStyle(
                color: KinevorBillingTheme.text,
                fontSize: 38,
                fontWeight: FontWeight.w800,
                height: 1.15,
                letterSpacing: -1.2,
              ),
            ),
          ),
          const SizedBox(height: 13),
          Row(
            children: [
              Icon(
                metric.icon,
                size: 13,
                color: metric.color ?? KinevorBillingTheme.textMuted,
              ),
              const SizedBox(width: 5),
              Expanded(
                child: Text(
                  metric.label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: metric.color ?? KinevorBillingTheme.textMuted,
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              if (summary.overdueAmount > 0) ...[
                const SizedBox(width: 10),
                Container(
                  width: 3,
                  height: 3,
                  decoration: const BoxDecoration(
                    color: KinevorBillingTheme.textMuted,
                    shape: BoxShape.circle,
                  ),
                ),
                const SizedBox(width: 10),
                Flexible(
                  child: Text(
                    '${_currencySymbol(summary.currency)}${_amount(summary.overdueAmount)} ${strings.overdue.toLowerCase()}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: KinevorBillingTheme.warning,
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }

  static String _currencySymbol(String currency) => switch (currency) {
    'USD' => r'$',
    'EUR' => '€',
    'GBP' => '£',
    _ => '$currency ',
  };

  static String _amount(double value) => value.toStringAsFixed(2);
}

class _MetricOf {
  const _MetricOf({required this.label, required this.icon, this.color});

  final String label;
  final IconData icon;
  final Color? color;

  factory _MetricOf.nextDue(BillingSummary summary, BillingStrings strings) {
    final due = summary.nextDueAt;
    if (due == null) {
      return const _MetricOf(
        label: 'Nothing due',
        icon: Icons.check_circle_outline,
        color: KinevorBillingTheme.success,
      );
    }
    return _MetricOf(
      label: '${strings.nextDue} ${_monthDay(due)}',
      icon: Icons.calendar_today_outlined,
    );
  }

  static const _months = [
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

  static String _monthDay(DateTime value) =>
      '${_months[value.month - 1]} ${value.day}';
}

class _StatsRow extends StatelessWidget {
  const _StatsRow({required this.summary, required this.strings});

  final BillingSummary summary;
  final BillingStrings strings;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _StatTile(
            label: 'OPEN',
            value: summary.openInvoiceCount.toString().padLeft(2, '0'),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: _StatTile(
            label: 'OVERDUE',
            value: summary.overdueInvoiceCount.toString().padLeft(2, '0'),
            color: summary.overdueInvoiceCount > 0
                ? KinevorBillingTheme.warning
                : null,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: _StatTile(
            label: 'PAID THIS YEAR',
            value: strings.money(summary.currency, summary.paidThisYear),
          ),
        ),
      ],
    );
  }
}

class _StatTile extends StatelessWidget {
  const _StatTile({required this.label, required this.value, this.color});

  final String label;
  final String value;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(12, 12, 10, 12),
      decoration: BoxDecoration(
        color: KinevorBillingTheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: KinevorBillingTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(
              label,
              style: const TextStyle(
                color: KinevorBillingTheme.textMuted,
                fontSize: 8,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.8,
              ),
            ),
          ),
          const SizedBox(height: 5),
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(
              value,
              style: TextStyle(
                color: color ?? KinevorBillingTheme.text,
                fontSize: 18,
                fontWeight: FontWeight.w800,
                fontFeatures: const [FontFeature.tabularFigures()],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FilterBar extends StatelessWidget {
  const _FilterBar({
    required this.selected,
    required this.strings,
    required this.onSelected,
  });

  final String? selected;
  final BillingStrings strings;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) {
    final filters = <(String?, String)>[
      (null, strings.filterAll),
      ('Open', strings.filterOpen),
      ('Overdue', strings.filterOverdue),
      ('Paid', strings.filterPaid),
    ];
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          for (var index = 0; index < filters.length; index++) ...[
            if (index > 0) const SizedBox(width: 7),
            ChoiceChip(
              label: Text(filters[index].$2),
              selected: selected == filters[index].$1,
              showCheckmark: false,
              onSelected: (_) => onSelected(filters[index].$1),
            ),
          ],
        ],
      ),
    );
  }
}

class _InvoiceCard extends StatelessWidget {
  const _InvoiceCard({
    required this.invoice,
    required this.strings,
    required this.onTap,
  });

  final InvoiceSummary invoice;
  final BillingStrings strings;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final overdue = invoice.status.toLowerCase() == 'overdue';
    final muted = invoice.status.toLowerCase() == 'void';
    return Card(
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 14, 14),
          child: Column(
            children: [
              Row(
                children: [
                  Container(
                    width: 32,
                    height: 32,
                    decoration: BoxDecoration(
                      color: KinevorBillingTheme.surface,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: KinevorBillingTheme.border),
                    ),
                    child: Icon(
                      Icons.description_outlined,
                      size: 16,
                      color: muted
                          ? KinevorBillingTheme.textMuted
                          : KinevorBillingTheme.text,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          invoice.number,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.w700,
                            letterSpacing: 0.2,
                            color: muted
                                ? KinevorBillingTheme.textMuted
                                : KinevorBillingTheme.text,
                          ),
                        ),
                        Text(
                          '${strings.issued} ${strings.monthDay(invoice.issuedAt)}',
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: KinevorBillingTheme.textMuted,
                            fontSize: 10.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                  BillingStatusChip(
                    status: invoice.status,
                    strings: strings,
                    compact: true,
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.symmetric(vertical: 10),
                decoration: const BoxDecoration(
                  border: Border(
                    top: BorderSide(color: KinevorBillingTheme.border),
                    bottom: BorderSide(color: KinevorBillingTheme.border),
                  ),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: _amountColumn(
                        context,
                        strings.total,
                        strings.money(invoice.currency, invoice.total),
                      ),
                    ),
                    Container(
                      width: 1,
                      height: 26,
                      color: KinevorBillingTheme.border,
                    ),
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.only(left: 14),
                        child: _amountColumn(
                          context,
                          strings.outstanding,
                          strings.money(
                            invoice.currency,
                            invoice.outstandingAmount,
                          ),
                          color: invoice.outstandingAmount > 0
                              ? (overdue
                                    ? KinevorBillingTheme.warning
                                    : KinevorBillingTheme.text)
                              : KinevorBillingTheme.success,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Icon(
                    Icons.calendar_today_outlined,
                    size: 11,
                    color: overdue
                        ? KinevorBillingTheme.warning
                        : KinevorBillingTheme.textMuted,
                  ),
                  const SizedBox(width: 5),
                  Text(
                    '${strings.due} ${strings.monthDay(invoice.dueAt)}',
                    style: TextStyle(
                      color: overdue
                          ? KinevorBillingTheme.warning
                          : KinevorBillingTheme.textMuted,
                      fontSize: 10.5,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Container(
                    width: 3,
                    height: 3,
                    decoration: const BoxDecoration(
                      color: KinevorBillingTheme.textMuted,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    '${invoice.lineCount} ${invoice.lineCount == 1 ? 'line' : 'lines'}',
                    style: const TextStyle(
                      color: KinevorBillingTheme.textMuted,
                      fontSize: 10.5,
                    ),
                  ),
                  const Spacer(),
                  const Icon(
                    Icons.arrow_forward_ios_rounded,
                    size: 11,
                    color: KinevorBillingTheme.textMuted,
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _amountColumn(
    BuildContext context,
    String label,
    String value, {
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
            letterSpacing: 0.8,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
            color: color ?? KinevorBillingTheme.text,
            fontSize: 14,
            fontWeight: FontWeight.w800,
            fontFeatures: const [FontFeature.tabularFigures()],
          ),
        ),
      ],
    );
  }
}
