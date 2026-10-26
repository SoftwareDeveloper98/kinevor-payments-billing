import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  ApiEnvelope,
  BILLING_API_BASE_URL,
  BillingGateway,
  BillingStats,
  CreateInvoiceRequest,
  InvoiceCreated,
  InvoiceDetail,
  InvoiceListQuery,
  InvoiceSummary,
  PagedResult,
  PaymentDetail,
  PaymentListQuery,
  PaymentRecorded,
  PaymentSummary,
  RecordPaymentRequest,
  RefundPaymentRequest,
  RevenueSummary,
} from './billing.model';

@Injectable()
export class HttpBillingGateway implements BillingGateway {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(BILLING_API_BASE_URL).replace(/\/$/, '');

  async stats(): Promise<BillingStats> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<BillingStats>>(`${this.baseUrl}/billing/stats`),
    );
    return response.data;
  }

  async revenue(from?: string, to?: string): Promise<RevenueSummary> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);

    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<RevenueSummary>>(`${this.baseUrl}/billing/revenue`, { params }),
    );
    return response.data;
  }

  async listInvoices(query: InvoiceListQuery): Promise<PagedResult<InvoiceSummary>> {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 20);

    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);

    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<PagedResult<InvoiceSummary>>>(`${this.baseUrl}/billing/invoices`, {
        params,
      }),
    );
    return response.data;
  }

  async invoice(id: string): Promise<InvoiceDetail> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<InvoiceDetail>>(`${this.baseUrl}/billing/invoices/${id}`),
    );
    return response.data;
  }

  async createInvoice(request: CreateInvoiceRequest): Promise<InvoiceCreated> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<InvoiceCreated>>(`${this.baseUrl}/billing/invoices`, request),
    );
    return response.data;
  }

  async markInvoicePaid(id: string): Promise<InvoiceDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<InvoiceDetail>>(
        `${this.baseUrl}/billing/invoices/${id}/paid`,
        {},
      ),
    );
    return response.data;
  }

  async voidInvoice(id: string, reason: string): Promise<InvoiceDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<InvoiceDetail>>(
        `${this.baseUrl}/billing/invoices/${id}/void`,
        { reason },
      ),
    );
    return response.data;
  }

  async markInvoiceOverdue(id: string): Promise<InvoiceDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<InvoiceDetail>>(
        `${this.baseUrl}/billing/invoices/${id}/overdue`,
        {},
      ),
    );
    return response.data;
  }

  async listPayments(query: PaymentListQuery): Promise<PagedResult<PaymentSummary>> {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 20);

    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);
    if (query.method) params = params.set('method', query.method);

    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<PagedResult<PaymentSummary>>>(`${this.baseUrl}/billing/payments`, {
        params,
      }),
    );
    return response.data;
  }

  async payment(id: string): Promise<PaymentDetail> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<PaymentDetail>>(`${this.baseUrl}/billing/payments/${id}`),
    );
    return response.data;
  }

  async recordPayment(request: RecordPaymentRequest): Promise<PaymentRecorded> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<PaymentRecorded>>(
        `${this.baseUrl}/billing/payments`,
        { ...request, currency: 'USD' },
      ),
    );
    return response.data;
  }

  async refundPayment(id: string, request: RefundPaymentRequest): Promise<PaymentDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<PaymentDetail>>(
        `${this.baseUrl}/billing/payments/${id}/refund`,
        request,
      ),
    );
    return response.data;
  }
}
