import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import {
  BILLING_GATEWAY,
  BillingStats,
  InvoiceDetail,
  InvoiceListQuery,
  InvoiceSummary,
  PaymentMethod,
  PaymentSummary,
  RevenueSummary,
} from './billing.model';

@Injectable()
export class BillingFacade {
  private readonly gateway = inject(BILLING_GATEWAY);

  private readonly statsState = signal<BillingStats | null>(null);
  private readonly revenueState = signal<RevenueSummary | null>(null);
  private readonly recentState = signal<InvoiceSummary[]>([]);
  private readonly invoicesState = signal<InvoiceSummary[]>([]);
  private readonly totalState = signal(0);
  private readonly invoiceState = signal<InvoiceDetail | null>(null);
  private readonly invoicePaymentsState = signal<PaymentSummary[]>([]);
  private readonly loadingState = signal(false);
  private readonly detailLoadingState = signal(false);
  private readonly actionState = signal(false);
  private readonly errorState = signal<string | null>(null);
  private readonly noticeState = signal<string | null>(null);
  private readonly queryState = signal<InvoiceListQuery>({ page: 1, pageSize: 20 });

  readonly stats = this.statsState.asReadonly();
  readonly revenue = this.revenueState.asReadonly();
  readonly recentInvoices = this.recentState.asReadonly();
  readonly invoices = this.invoicesState.asReadonly();
  readonly total = this.totalState.asReadonly();
  readonly invoice = this.invoiceState.asReadonly();
  readonly invoicePayments = this.invoicePaymentsState.asReadonly();
  readonly loading = this.loadingState.asReadonly();
  readonly detailLoading = this.detailLoadingState.asReadonly();
  readonly actionLoading = this.actionState.asReadonly();
  readonly error = this.errorState.asReadonly();
  readonly notice = this.noticeState.asReadonly();
  readonly query = this.queryState.asReadonly();

  readonly collectionRate = computed(() => {
    const stats = this.statsState();
    if (!stats || stats.totalInvoiceCount === 0) return 0;
    return Math.round((stats.paidInvoiceCount / stats.totalInvoiceCount) * 100);
  });

  readonly overdueShare = computed(() => {
    const stats = this.statsState();
    if (!stats || stats.outstandingAmount === 0) return 0;
    return Math.round((stats.overdueAmount / stats.outstandingAmount) * 100);
  });

  readonly revenuePeak = computed(() => {
    const buckets = this.revenueState()?.buckets ?? [];
    return Math.max(1, ...buckets.map((bucket) => bucket.grossCollected));
  });

  readonly paidInvoicePayments = computed(() =>
    this.invoicePaymentsState().filter(
      (payment) => payment.status === 'Paid' || payment.status === 'PartiallyRefunded',
    ),
  );

  async loadDashboard(): Promise<void> {
    this.loadingState.set(true);
    this.errorState.set(null);

    try {
      const [stats, revenue, recent] = await Promise.all([
        this.gateway.stats(),
        this.gateway.revenue(),
        this.gateway.listInvoices({ page: 1, pageSize: 6 }),
      ]);
      this.statsState.set(stats);
      this.revenueState.set(revenue);
      this.recentState.set(recent.items);
    } catch (error) {
      this.errorState.set(this.messageFor(error));
    } finally {
      this.loadingState.set(false);
    }
  }

  async loadInvoices(): Promise<void> {
    this.loadingState.set(true);
    this.errorState.set(null);

    try {
      const page = await this.gateway.listInvoices(this.queryState());
      this.invoicesState.set(page.items);
      this.totalState.set(page.totalCount);
    } catch (error) {
      this.errorState.set(this.messageFor(error));
    } finally {
      this.loadingState.set(false);
    }
  }

  async updateQuery(patch: Partial<InvoiceListQuery>): Promise<void> {
    this.queryState.update((query) => ({ ...query, ...patch }));
    await this.loadInvoices();
  }

  async loadInvoice(id: string): Promise<InvoiceDetail | null> {
    this.detailLoadingState.set(true);
    this.errorState.set(null);
    this.noticeState.set(null);

    try {
      const invoice = await this.gateway.invoice(id);
      this.invoiceState.set(invoice);
      await this.loadInvoicePayments(invoice);
      return invoice;
    } catch (error) {
      this.invoiceState.set(null);
      this.invoicePaymentsState.set([]);
      this.errorState.set(this.messageFor(error));
      return null;
    } finally {
      this.detailLoadingState.set(false);
    }
  }

  async recordPayment(
    amount: number,
    method: PaymentMethod,
    reference: string,
  ): Promise<boolean> {
    const invoice = this.invoiceState();
    if (!invoice) return false;

    this.actionState.set(true);
    this.errorState.set(null);
    this.noticeState.set(null);

    try {
      const payment = await this.gateway.recordPayment({
        memberId: invoice.memberId,
        memberName: invoice.memberName,
        invoiceId: invoice.id,
        amount,
        method,
        reference: reference.trim() || null,
        description: `Payment for ${invoice.number}`,
      });
      this.noticeState.set(`Payment ${payment.reference} recorded.`);
      await this.refreshInvoice(invoice.id);
      await this.refreshInsights();
      return true;
    } catch (error) {
      this.errorState.set(this.messageFor(error));
      return false;
    } finally {
      this.actionState.set(false);
    }
  }

  async refundPayment(paymentId: string, amount: number, reason: string): Promise<boolean> {
    this.actionState.set(true);
    this.errorState.set(null);
    this.noticeState.set(null);

    try {
      await this.gateway.refundPayment(paymentId, { amount, reason: reason.trim() });
      this.noticeState.set('Refund recorded and the invoice balance refreshed.');
      const invoice = this.invoiceState();
      if (invoice) await this.refreshInvoice(invoice.id);
      await this.refreshInsights();
      return true;
    } catch (error) {
      this.errorState.set(this.messageFor(error));
      return false;
    } finally {
      this.actionState.set(false);
    }
  }

  async markPaid(): Promise<boolean> {
    return this.runInvoiceAction((id) => this.gateway.markInvoicePaid(id), 'Invoice marked paid.');
  }

  async markOverdue(): Promise<boolean> {
    return this.runInvoiceAction(
      (id) => this.gateway.markInvoiceOverdue(id),
      'Invoice marked overdue.',
    );
  }

  async voidInvoice(reason: string): Promise<boolean> {
    return this.runInvoiceAction(
      (id) => this.gateway.voidInvoice(id, reason.trim()),
      'Invoice voided.',
    );
  }

  clearNotice(): void {
    this.noticeState.set(null);
  }

  private async loadInvoicePayments(invoice: InvoiceDetail): Promise<void> {
    const page = await this.gateway.listPayments({
      search: invoice.number,
      pageSize: 50,
    });
    this.invoicePaymentsState.set(page.items);
  }

  private async refreshInvoice(id: string): Promise<void> {
    const invoice = await this.gateway.invoice(id);
    this.invoiceState.set(invoice);
    await this.loadInvoicePayments(invoice);
  }

  private async refreshInsights(): Promise<void> {
    const [stats, revenue] = await Promise.all([
      this.gateway.stats(),
      this.gateway.revenue(),
    ]);
    this.statsState.set(stats);
    this.revenueState.set(revenue);
  }

  private async runInvoiceAction(
    action: (id: string) => Promise<InvoiceDetail>,
    successMessage: string,
  ): Promise<boolean> {
    const invoice = this.invoiceState();
    if (!invoice) return false;

    this.actionState.set(true);
    this.errorState.set(null);
    this.noticeState.set(null);

    try {
      const updated = await action(invoice.id);
      this.invoiceState.set(updated);
      await this.loadInvoicePayments(updated);
      await this.refreshInsights();
      this.noticeState.set(successMessage);
      return true;
    } catch (error) {
      this.errorState.set(this.messageFor(error));
      return false;
    } finally {
      this.actionState.set(false);
    }
  }

  private messageFor(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'The billing API is not reachable. Start the demo host on port 5087.';
      }
      if (error.status === 404) return 'The invoice or payment could not be found.';
      const body = error.error as { message?: string; errors?: string[] } | null;
      return body?.message ?? body?.errors?.[0] ?? 'The request failed. Try again.';
    }

    return 'The request failed. Try again.';
  }
}
