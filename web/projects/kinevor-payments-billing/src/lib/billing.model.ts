import { InjectionToken } from '@angular/core';

export type InvoiceStatus = 'Open' | 'Overdue' | 'Paid' | 'Void';
export type PaymentStatus = 'Pending' | 'Paid' | 'Failed' | 'PartiallyRefunded' | 'Refunded';
export type PaymentMethod = 'Card' | 'Cash' | 'BankTransfer';

export interface InvoiceLine {
  readonly id: string;
  readonly description: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly taxRate: number;
  readonly subtotal: number;
  readonly taxAmount: number;
  readonly total: number;
}

export interface InvoiceSummary {
  readonly id: string;
  readonly number: string;
  readonly memberId: string;
  readonly memberName: string;
  readonly memberEmail: string | null;
  readonly status: InvoiceStatus;
  readonly issuedAt: string;
  readonly dueAt: string;
  readonly subTotal: number;
  readonly taxAmount: number;
  readonly total: number;
  readonly amountPaid: number;
  readonly outstandingAmount: number;
  readonly currency: string;
  readonly lineCount: number;
}

export interface InvoiceDetail {
  readonly id: string;
  readonly number: string;
  readonly memberId: string;
  readonly memberName: string;
  readonly memberEmail: string | null;
  readonly status: InvoiceStatus;
  readonly issuedAt: string;
  readonly dueAt: string;
  readonly paidAt: string | null;
  readonly voidedAt: string | null;
  readonly voidReason: string | null;
  readonly notes: string | null;
  readonly subTotal: number;
  readonly taxAmount: number;
  readonly total: number;
  readonly amountPaid: number;
  readonly outstandingAmount: number;
  readonly currency: string;
  readonly lines: InvoiceLine[];
}

export interface PaymentSummary {
  readonly id: string;
  readonly reference: string;
  readonly memberId: string;
  readonly memberName: string;
  readonly invoiceId: string | null;
  readonly invoiceNumber: string | null;
  readonly description: string;
  readonly method: PaymentMethod;
  readonly status: PaymentStatus;
  readonly amount: number;
  readonly refundedAmount: number;
  readonly netAmount: number;
  readonly currency: string;
  readonly paidAt: string;
  readonly refundedAt: string | null;
  readonly createdAt: string;
}

export interface Refund {
  readonly id: string;
  readonly amount: number;
  readonly reason: string;
  readonly refundedAt: string;
}

export interface PaymentDetail {
  readonly id: string;
  readonly reference: string;
  readonly memberId: string;
  readonly memberName: string;
  readonly invoiceId: string | null;
  readonly invoiceNumber: string | null;
  readonly description: string;
  readonly method: PaymentMethod;
  readonly status: PaymentStatus;
  readonly amount: number;
  readonly refundedAmount: number;
  readonly netAmount: number;
  readonly currency: string;
  readonly notes: string | null;
  readonly paidAt: string;
  readonly refundedAt: string | null;
  readonly createdAt: string;
  readonly refunds: Refund[];
}

export interface BillingStats {
  readonly openInvoiceCount: number;
  readonly overdueInvoiceCount: number;
  readonly paidInvoiceCount: number;
  readonly totalInvoiceCount: number;
  readonly outstandingAmount: number;
  readonly overdueAmount: number;
  readonly collectedThisMonth: number;
  readonly refundedThisMonth: number;
  readonly netThisMonth: number;
  readonly currency: string;
}

export interface RevenueBucket {
  readonly label: string;
  readonly periodStart: string;
  readonly grossCollected: number;
  readonly refunded: number;
  readonly netRevenue: number;
}

export interface RevenueSummary {
  readonly from: string;
  readonly to: string;
  readonly grossCollected: number;
  readonly refunded: number;
  readonly netRevenue: number;
  readonly paymentCount: number;
  readonly currency: string;
  readonly buckets: RevenueBucket[];
}

export interface PagedResult<T> {
  readonly items: T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly totalPages: number;
  readonly hasNext: boolean;
  readonly hasPrev: boolean;
}

export interface ApiEnvelope<T> {
  readonly success: boolean;
  readonly data: T;
  readonly message?: string | null;
  readonly errors?: string[] | null;
}

export interface InvoiceListQuery {
  readonly search?: string;
  readonly status?: InvoiceStatus;
  readonly page?: number;
  readonly pageSize?: number;
}

export interface PaymentListQuery {
  readonly search?: string;
  readonly status?: PaymentStatus;
  readonly method?: PaymentMethod;
  readonly page?: number;
  readonly pageSize?: number;
}

export interface RecordPaymentRequest {
  readonly memberId: string;
  readonly memberName: string;
  readonly invoiceId: string;
  readonly amount: number;
  readonly method: PaymentMethod;
  readonly reference?: string | null;
  readonly description?: string | null;
}

export interface RefundPaymentRequest {
  readonly amount: number;
  readonly reason: string;
}

export interface PaymentRecorded {
  readonly id: string;
  readonly reference: string;
  readonly status: PaymentStatus;
}

export interface CreateInvoiceLineRequest {
  readonly description: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly taxRate: number;
}

export interface CreateInvoiceRequest {
  readonly memberId: string;
  readonly memberName: string;
  readonly memberEmail?: string | null;
  readonly issuedAt?: string | null;
  readonly dueAt?: string | null;
  readonly currency?: string | null;
  readonly lines: CreateInvoiceLineRequest[];
  readonly notes?: string | null;
}

export interface InvoiceCreated {
  readonly id: string;
  readonly number: string;
  readonly total: number;
  readonly currency: string;
  readonly dueAt: string;
}

export const BILLING_GATEWAY = new InjectionToken<BillingGateway>('BillingGateway');
export const BILLING_API_BASE_URL = new InjectionToken<string>('BILLING_API_BASE_URL');

export interface BillingGateway {
  stats(): Promise<BillingStats>;
  revenue(from?: string, to?: string): Promise<RevenueSummary>;
  listInvoices(query: InvoiceListQuery): Promise<PagedResult<InvoiceSummary>>;
  invoice(id: string): Promise<InvoiceDetail>;
  createInvoice(request: CreateInvoiceRequest): Promise<InvoiceCreated>;
  markInvoicePaid(id: string): Promise<InvoiceDetail>;
  voidInvoice(id: string, reason: string): Promise<InvoiceDetail>;
  markInvoiceOverdue(id: string): Promise<InvoiceDetail>;
  listPayments(query: PaymentListQuery): Promise<PagedResult<PaymentSummary>>;
  payment(id: string): Promise<PaymentDetail>;
  recordPayment(request: RecordPaymentRequest): Promise<PaymentRecorded>;
  refundPayment(id: string, request: RefundPaymentRequest): Promise<PaymentDetail>;
}

export const INVOICE_STATUSES: readonly InvoiceStatus[] = ['Open', 'Overdue', 'Paid', 'Void'];
export const PAYMENT_STATUSES: readonly PaymentStatus[] = [
  'Paid',
  'PartiallyRefunded',
  'Refunded',
  'Pending',
  'Failed',
];
export const PAYMENT_METHODS: readonly PaymentMethod[] = ['Card', 'Cash', 'BankTransfer'];

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  Card: 'Card',
  Cash: 'Cash',
  BankTransfer: 'Bank transfer',
};

export function statusLabel(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2');
}
