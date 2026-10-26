import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { BillingFacade } from './billing.facade';
import {
  PAYMENT_METHODS,
  PAYMENT_METHOD_LABELS,
  PaymentMethod,
  PaymentSummary,
  statusLabel,
} from './billing.model';

@Component({
  selector: 'kinevor-invoice-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    PercentPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  template: `
    <div class="page-container">
      <a class="back-link" routerLink="/billing/invoices">
        <mat-icon aria-hidden="true">arrow_back</mat-icon>
        Back to invoices
      </a>

      @if (facade.detailLoading() && !facade.invoice()) {
        <div class="table-loading" role="status">
          <mat-spinner diameter="48" />
          <span>Loading invoice…</span>
        </div>
      }

      @if (facade.error(); as error) {
        <p class="notice notice-error" role="alert"><mat-icon aria-hidden="true">error</mat-icon>{{ error }}</p>
      }

      @if (facade.notice(); as notice) {
        <p class="notice notice-success" role="status"><mat-icon aria-hidden="true">check_circle</mat-icon>{{ notice }}</p>
      }

      @if (facade.invoice(); as invoice) {
        <header class="detail-head">
          <div class="detail-title">
            <span class="eyebrow">Invoice {{ invoice.number }}</span>
            <div class="title-row">
              <h1>{{ invoice.memberName }}</h1>
              <span class="status-chip" [class]="'status-' + invoice.status.toLowerCase()">
                {{ label(invoice.status) }}
              </span>
            </div>
            <p>
              {{ invoice.memberEmail || 'No email on file' }}
              <span aria-hidden="true">·</span>
              Issued {{ invoice.issuedAt | date: 'mediumDate' }}
              <span aria-hidden="true">·</span>
              Due {{ invoice.dueAt | date: 'mediumDate' }}
            </p>
          </div>
          <div class="balance-block">
            <span>{{ invoice.outstandingAmount > 0 ? 'Balance due' : 'Invoice total' }}</span>
            <strong>{{ (invoice.outstandingAmount > 0 ? invoice.outstandingAmount : invoice.total) | currency: invoice.currency : 'symbol' : '1.2-2' }}</strong>
            <small>{{ invoice.currency }} · {{ invoice.lines.length }} line{{ invoice.lines.length === 1 ? '' : 's' }}</small>
          </div>
        </header>

        <div class="detail-grid">
          <mat-card class="gym-card items-card">
            <mat-card-content>
              <div class="card-head">
                <div>
                  <span class="eyebrow">Line items</span>
                  <h2>Invoice breakdown</h2>
                </div>
                <span class="doc-number mono">{{ invoice.number }}</span>
              </div>

              <div class="table-wrap">
                <table class="line-table">
                  <thead>
                    <tr>
                      <th scope="col">Description</th>
                      <th scope="col" class="align-end">Qty</th>
                      <th scope="col" class="align-end">Unit price</th>
                      <th scope="col" class="align-end">Tax</th>
                      <th scope="col" class="align-end">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (line of invoice.lines; track line.id) {
                      <tr>
                        <td>{{ line.description }}</td>
                        <td class="align-end">{{ line.quantity }}</td>
                        <td class="align-end amount">{{ line.unitPrice | currency: invoice.currency : 'symbol' : '1.2-2' }}</td>
                        <td class="align-end">{{ line.taxRate | percent: '1.0-1' }}</td>
                        <td class="align-end amount">{{ line.total | currency: invoice.currency : 'symbol' : '1.2-2' }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              <mat-divider />

              <div class="totals">
                <div><span>Subtotal</span><strong>{{ invoice.subTotal | currency: invoice.currency : 'symbol' : '1.2-2' }}</strong></div>
                <div><span>Tax</span><strong>{{ invoice.taxAmount | currency: invoice.currency : 'symbol' : '1.2-2' }}</strong></div>
                <div class="total-row"><span>Total</span><strong>{{ invoice.total | currency: invoice.currency : 'symbol' : '1.2-2' }}</strong></div>
                <div><span>Paid</span><strong class="positive">{{ invoice.amountPaid | currency: invoice.currency : 'symbol' : '1.2-2' }}</strong></div>
                <div class="balance-row"><span>Balance due</span><strong [class.danger]="invoice.outstandingAmount > 0">{{ invoice.outstandingAmount | currency: invoice.currency : 'symbol' : '1.2-2' }}</strong></div>
              </div>

              @if (invoice.notes) {
                <div class="note-block">
                  <span>Notes</span>
                  <p>{{ invoice.notes }}</p>
                </div>
              }

              @if (invoice.voidReason) {
                <div class="note-block notice-block">
                  <span>Void reason</span>
                  <p>{{ invoice.voidReason }}</p>
                </div>
              }
            </mat-card-content>
          </mat-card>

          <aside class="detail-side">
            @if (canAcceptPayment(invoice.status, invoice.outstandingAmount)) {
              <mat-card class="gym-card payment-card">
                <mat-card-content>
                  <div class="card-head">
                    <div>
                      <span class="eyebrow">Payment</span>
                      <h2>Record a payment</h2>
                    </div>
                    <span class="sim-chip"><mat-icon aria-hidden="true">bolt</mat-icon>Simulated</span>
                  </div>
                  <p class="card-intro">
                    Capture a card, cash or bank-transfer payment against this invoice. No live
                    processor is called.
                  </p>

                  <form [formGroup]="paymentForm" (ngSubmit)="recordPayment()" class="payment-form">
                    <mat-form-field appearance="outline" subscriptSizing="dynamic">
                      <mat-label>Amount</mat-label>
                      <span matPrefix class="currency-prefix">{{ invoice.currency }}&nbsp;</span>
                      <input
                        matInput
                        type="number"
                        min="0.01"
                        step="0.01"
                        [attr.max]="invoice.outstandingAmount"
                        formControlName="amount"
                      />
                      <mat-hint>Remaining {{ invoice.outstandingAmount | currency: invoice.currency : 'symbol' : '1.2-2' }}</mat-hint>
                    </mat-form-field>

                    <mat-form-field appearance="outline" subscriptSizing="dynamic">
                      <mat-label>Method</mat-label>
                      <mat-select formControlName="method">
                        @for (method of methods; track method) {
                          <mat-option [value]="method">{{ methodLabel(method) }}</mat-option>
                        }
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline" subscriptSizing="dynamic">
                      <mat-label>Reference (optional)</mat-label>
                      <input matInput formControlName="reference" placeholder="Terminal or transfer ID" />
                    </mat-form-field>

                    @if (paymentError) {
                      <p class="inline-error" role="alert">{{ paymentError }}</p>
                    }

                    <button
                      mat-flat-button
                      color="primary"
                      type="submit"
                      class="wide-button"
                      [disabled]="facade.actionLoading()"
                    >
                      @if (facade.actionLoading()) {
                        <mat-spinner diameter="18" />
                      } @else {
                        <mat-icon aria-hidden="true">payments</mat-icon>
                      }
                      Record payment
                    </button>
                  </form>
                </mat-card-content>
              </mat-card>
            } @else {
              <mat-card class="gym-card settled-card">
                <mat-card-content>
                  <span class="settled-icon" aria-hidden="true">
                    <mat-icon>{{ invoice.status === 'Void' ? 'block' : 'task_alt' }}</mat-icon>
                  </span>
                  <h2>{{ invoice.status === 'Void' ? 'Invoice voided' : 'Invoice settled' }}</h2>
                  <p>
                    @if (invoice.status === 'Void') {
                      This invoice was voided{{ invoice.voidedAt ? ' on ' + (invoice.voidedAt | date: 'mediumDate') : '' }}.
                    } @else {
                      The full balance has been collected{{ invoice.paidAt ? ' on ' + (invoice.paidAt | date: 'mediumDate') : '' }}.
                    }
                  </p>
                </mat-card-content>
              </mat-card>
            }

            <mat-card class="gym-card actions-card">
              <mat-card-content>
                <span class="eyebrow">Invoice actions</span>
                <div class="action-list">
                  @if (canAcceptPayment(invoice.status, invoice.outstandingAmount)) {
                    <button mat-stroked-button type="button" [disabled]="facade.actionLoading()" (click)="markPaid()">
                      <mat-icon aria-hidden="true">task_alt</mat-icon>
                      Mark paid outside payments
                    </button>
                  }
                  @if (invoice.status === 'Open') {
                    <button mat-stroked-button type="button" [disabled]="facade.actionLoading()" (click)="markOverdue()">
                      <mat-icon aria-hidden="true">schedule</mat-icon>
                      Mark overdue
                    </button>
                  }
                  @if (invoice.status === 'Open' || invoice.status === 'Overdue') {
                    @if (!voiding) {
                      <button mat-stroked-button type="button" class="danger-button" (click)="startVoid()">
                        <mat-icon aria-hidden="true">block</mat-icon>
                        Void invoice
                      </button>
                    } @else {
                      <form [formGroup]="voidForm" (ngSubmit)="voidInvoice()" class="void-form">
                        <mat-form-field appearance="outline" subscriptSizing="dynamic">
                          <mat-label>Void reason</mat-label>
                          <input matInput formControlName="reason" placeholder="Why is this invoice void?" />
                        </mat-form-field>
                        <div class="void-actions">
                          <button mat-button type="button" (click)="cancelVoid()">Cancel</button>
                          <button mat-flat-button color="warn" type="submit" [disabled]="voidForm.invalid || facade.actionLoading()">Void</button>
                        </div>
                      </form>
                    }
                  }
                </div>
              </mat-card-content>
            </mat-card>
          </aside>
        </div>

        <mat-card class="gym-card history-card">
          <mat-card-content>
            <div class="card-head">
              <div>
                <span class="eyebrow">Ledger</span>
                <h2>Payment history</h2>
              </div>
              <span class="history-count">{{ facade.invoicePayments().length }} payment{{ facade.invoicePayments().length === 1 ? '' : 's' }}</span>
            </div>

            @if (facade.invoicePayments().length > 0) {
              <div class="table-wrap">
                <table class="payment-table">
                  <thead>
                    <tr>
                      <th scope="col">Reference</th>
                      <th scope="col">Date</th>
                      <th scope="col">Method</th>
                      <th scope="col" class="align-end">Amount</th>
                      <th scope="col" class="align-end">Refunded</th>
                      <th scope="col">Status</th>
                      <th scope="col"><span class="sr-only">Actions</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (payment of facade.invoicePayments(); track payment.id) {
                      <tr>
                        <td class="mono">{{ payment.reference }}</td>
                        <td>{{ payment.paidAt | date: 'mediumDate' }}</td>
                        <td>{{ methodLabel(payment.method) }}</td>
                        <td class="align-end amount">{{ payment.amount | currency: payment.currency : 'symbol' : '1.2-2' }}</td>
                        <td class="align-end amount" [class.danger]="payment.refundedAmount > 0">
                          {{ payment.refundedAmount | currency: payment.currency : 'symbol' : '1.2-2' }}
                        </td>
                        <td>
                          <span class="status-chip" [class]="'status-' + payment.status.toLowerCase()">
                            {{ label(payment.status) }}
                          </span>
                        </td>
                        <td class="align-end">
                          @if (canRefund(payment)) {
                            <button mat-button color="primary" type="button" (click)="startRefund(payment)">
                              Refund
                            </button>
                          }
                        </td>
                      </tr>
                      @if (refundingId === payment.id) {
                        <tr class="refund-row">
                          <td colspan="7">
                            <form [formGroup]="refundForm" (ngSubmit)="refund(payment)" class="refund-form">
                              <div class="refund-copy">
                                <strong>Refund {{ payment.reference }}</strong>
                                <span>Available {{ availableToRefund(payment) | currency: payment.currency : 'symbol' : '1.2-2' }}</span>
                              </div>
                              <mat-form-field appearance="outline" subscriptSizing="dynamic">
                                <mat-label>Refund amount</mat-label>
                                <input matInput type="number" min="0.01" step="0.01" [attr.max]="availableToRefund(payment)" formControlName="amount" />
                              </mat-form-field>
                              <mat-form-field appearance="outline" subscriptSizing="dynamic" class="reason-field">
                                <mat-label>Reason</mat-label>
                                <input matInput formControlName="reason" placeholder="Duplicate charge, member request…" />
                              </mat-form-field>
                              <div class="refund-actions">
                                <button mat-button type="button" (click)="cancelRefund()">Cancel</button>
                                <button mat-flat-button color="primary" type="submit" [disabled]="refundForm.invalid || facade.actionLoading()">Confirm refund</button>
                              </div>
                            </form>
                          </td>
                        </tr>
                      }
                    }
                  </tbody>
                </table>
              </div>
            } @else {
              <div class="empty-history">
                <mat-icon aria-hidden="true">account_balance</mat-icon>
                <div>
                  <strong>No payments recorded</strong>
                  <span>Payments captured against this invoice will appear here.</span>
                </div>
              </div>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .back-link { display:inline-flex; align-items:center; gap:4px; margin-bottom:16px; color:var(--color-text-muted); font-size:12px; font-weight:600; }
    .back-link:hover { color:var(--color-accent-text); }
    .back-link mat-icon { font-size:17px; width:17px; height:17px; }
    .detail-head { display:flex; align-items:flex-end; justify-content:space-between; gap:20px; flex-wrap:wrap; margin-bottom:18px; }
    .eyebrow { display:inline-flex; align-items:center; gap:7px; font-size:11px; font-weight:700; letter-spacing:.16em; text-transform:uppercase; color:var(--color-accent-text); }
    .title-row { display:flex; align-items:center; gap:10px; flex-wrap:wrap; margin:5px 0; }
    .title-row h1 { margin:0; font-size:clamp(30px,3.6vw,44px); line-height:1.05; letter-spacing:-.045em; }
    .detail-title p { margin:0; color:var(--color-text-muted); font-size:12px; }
    .detail-title p span { padding:0 5px; }
    .balance-block { display:flex; flex-direction:column; align-items:flex-end; text-align:end; }
    .balance-block span { font-size:10px; font-weight:700; letter-spacing:.1em; text-transform:uppercase; color:var(--color-text-muted); }
    .balance-block strong { font-size:clamp(26px,3vw,36px); letter-spacing:-.04em; line-height:1.15; font-variant-numeric:tabular-nums; }
    .balance-block small { font-size:10px; color:var(--color-text-muted); }
    .detail-grid { display:grid; grid-template-columns:1fr; gap:16px; align-items:start; }
    .detail-side { display:flex; flex-direction:column; gap:16px; }
    .items-card mat-card-content, .payment-card mat-card-content, .actions-card mat-card-content, .history-card mat-card-content, .settled-card mat-card-content { padding:22px !important; }
    .card-head { display:flex; align-items:flex-start; justify-content:space-between; gap:10px; flex-wrap:wrap; }
    .card-head h2 { margin:5px 0 0; font-size:19px; letter-spacing:-.03em; }
    .doc-number { color:var(--color-text-muted); }
    .mono { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:11px; }
    .table-wrap { overflow-x:auto; margin-top:14px; }
    .line-table, .payment-table { width:100%; border-collapse:collapse; font-size:12px; min-width:620px; }
    .line-table th, .payment-table th { padding:9px 10px; text-align:start; font-size:9px; font-weight:700; text-transform:uppercase; letter-spacing:.09em; color:var(--color-text-muted); border-bottom:1px solid var(--color-border); white-space:nowrap; }
    .line-table td, .payment-table td { padding:11px 10px; border-bottom:1px solid var(--color-border); }
    .line-table tbody tr:last-child td, .payment-table tbody tr:last-child td { border-bottom:none; }
    .align-end { text-align:end !important; }
    .amount { font-variant-numeric:tabular-nums; font-weight:600; white-space:nowrap; }
    .totals { display:flex; flex-direction:column; gap:7px; margin-top:14px; padding-left:auto; margin-left:auto; max-width:310px; font-size:12px; }
    .totals div { display:flex; justify-content:space-between; gap:18px; color:var(--color-text-muted); }
    .totals strong { color:var(--color-text); font-variant-numeric:tabular-nums; }
    .totals .total-row { padding-top:7px; border-top:1px solid var(--color-border); font-weight:700; }
    .totals .balance-row { margin-top:3px; padding:10px 12px; border-radius:10px; background:var(--color-accent-soft); color:var(--color-text); font-weight:700; }
    .totals .balance-row strong { color:var(--color-accent-text); }
    .positive { color:var(--color-success) !important; }
    .danger { color:var(--color-error) !important; }
    .status-chip { display:inline-flex; align-items:center; padding:4px 8px; border-radius:999px; font-size:9px; font-weight:700; letter-spacing:.04em; white-space:nowrap; border:1px solid var(--color-border); background:var(--color-surface); color:var(--color-text-muted); }
    .status-open { color:var(--color-info); border-color:color-mix(in srgb,var(--color-info) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-info) 10%,transparent); }
    .status-overdue, .status-partiallyrefunded, .status-pending { color:var(--color-warning); border-color:color-mix(in srgb,var(--color-warning) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-warning) 10%,transparent); }
    .status-paid { color:var(--color-success); border-color:color-mix(in srgb,var(--color-success) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-success) 10%,transparent); }
    .status-void, .status-refunded { color:var(--color-text-muted); background:var(--color-surface); }
    .status-failed { color:var(--color-error); border-color:color-mix(in srgb,var(--color-error) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-error) 10%,transparent); }
    .note-block { margin-top:16px; padding:12px 14px; border-radius:12px; background:var(--color-surface); border:1px solid var(--color-border); }
    .note-block span { font-size:9px; font-weight:700; text-transform:uppercase; letter-spacing:.1em; color:var(--color-text-muted); }
    .note-block p { margin:5px 0 0; font-size:12px; line-height:1.6; }
    .notice-block { border-color:color-mix(in srgb,var(--color-warning) 38%,var(--color-border)); }
    .card-intro { margin:10px 0 16px; font-size:12px; line-height:1.65; color:var(--color-text-muted); }
    .sim-chip { display:inline-flex; align-items:center; gap:3px; padding:5px 8px; border-radius:999px; background:color-mix(in srgb,var(--color-info) 13%,transparent); color:var(--color-info); font-size:9px; font-weight:700; text-transform:uppercase; letter-spacing:.08em; white-space:nowrap; }
    .sim-chip mat-icon { font-size:13px; width:13px; height:13px; }
    .payment-form { display:flex; flex-direction:column; gap:13px; }
    .currency-prefix { color:var(--color-text-muted); font-size:12px; }
    .wide-button { width:100%; }
    .wide-button mat-spinner { display:inline-block; margin-right:6px; }
    .inline-error { margin:0; padding:9px 11px; border-radius:9px; background:color-mix(in srgb,var(--color-error) 10%,transparent); color:var(--color-error); font-size:11px; }
    .settled-card mat-card-content { display:flex; flex-direction:column; align-items:center; text-align:center; gap:5px; padding:30px 22px !important; }
    .settled-icon { width:54px; height:54px; border-radius:50%; display:grid; place-items:center; background:color-mix(in srgb,var(--color-success) 13%,transparent); color:var(--color-success); margin-bottom:5px; }
    .settled-icon mat-icon { font-size:28px; width:28px; height:28px; }
    .settled-card h2 { margin:0; font-size:19px; }
    .settled-card p { margin:0; max-width:34ch; color:var(--color-text-muted); font-size:12px; line-height:1.6; }
    .action-list { display:flex; flex-direction:column; gap:7px; margin-top:12px; }
    .action-list button { justify-content:flex-start; font-size:11px; }
    .danger-button { color:var(--color-error) !important; }
    .void-form { display:flex; flex-direction:column; gap:8px; padding-top:6px; }
    .void-actions { display:flex; justify-content:flex-end; gap:4px; }
    .history-card { margin-top:16px; }
    .history-count { font-size:11px; color:var(--color-text-muted); }
    .payment-table tbody tr:hover { background:var(--color-accent-soft); }
    .refund-row td { background:var(--color-surface); padding:14px 10px; }
    .refund-form { display:flex; align-items:center; gap:12px; flex-wrap:wrap; }
    .refund-copy { display:flex; flex-direction:column; gap:2px; min-width:150px; }
    .refund-copy strong { font-size:12px; }
    .refund-copy span { font-size:10px; color:var(--color-text-muted); }
    .refund-form mat-form-field { width:130px; }
    .refund-form .reason-field { flex:1 1 200px; width:auto; }
    .refund-actions { display:flex; align-items:center; gap:4px; margin-left:auto; }
    .empty-history { display:flex; align-items:center; gap:12px; padding:30px 4px; color:var(--color-text-muted); }
    .empty-history mat-icon { font-size:28px; width:28px; height:28px; color:var(--color-accent-text); }
    .empty-history div { display:flex; flex-direction:column; gap:2px; }
    .empty-history strong { color:var(--color-text); font-size:13px; }
    .empty-history span { font-size:11px; }
    @media (min-width:960px) { .detail-grid { grid-template-columns:1.45fr .55fr; } .detail-side { position:sticky; top:22px; } }
  `],
})
export class InvoiceDetailComponent implements OnInit {
  readonly facade = inject(BillingFacade);
  readonly methods = PAYMENT_METHODS;
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  paymentError = '';
  refundingId: string | null = null;
  voiding = false;

  readonly paymentForm = this.formBuilder.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    method: ['Card' as PaymentMethod, Validators.required],
    reference: [''],
  });

  readonly refundForm = this.formBuilder.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    reason: ['', Validators.required],
  });

  readonly voidForm = this.formBuilder.nonNullable.group({
    reason: ['', Validators.required],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.facade.loadInvoice('missing');
      return;
    }

    void this.facade.loadInvoice(id).then((invoice) => {
      if (invoice) this.syncPaymentAmount(invoice.outstandingAmount);
    });
  }

  recordPayment(): void {
    const invoice = this.facade.invoice();
    if (!invoice) return;

    const { amount, method, reference } = this.paymentForm.getRawValue();
    if (this.paymentForm.invalid || amount <= 0) {
      this.paymentError = 'Enter an amount greater than zero.';
      return;
    }

    if (amount > invoice.outstandingAmount) {
      this.paymentError = 'The amount cannot exceed the outstanding balance.';
      return;
    }

    if (method === 'BankTransfer' && reference.trim().length < 3) {
      this.paymentError = 'Add a transfer reference for bank payments.';
      return;
    }

    this.paymentError = '';
    void this.facade.recordPayment(amount, method, reference).then((recorded) => {
      if (!recorded) return;
      this.paymentForm.patchValue({ reference: '' });
      this.syncPaymentAmount(this.facade.invoice()?.outstandingAmount ?? 0);
    });
  }

  startRefund(payment: PaymentSummary): void {
    this.refundingId = payment.id;
    this.refundForm.reset({
      amount: this.availableToRefund(payment),
      reason: '',
    });
  }

  cancelRefund(): void {
    this.refundingId = null;
    this.refundForm.reset({ amount: 0, reason: '' });
  }

  refund(payment: PaymentSummary): void {
    const { amount, reason } = this.refundForm.getRawValue();
    if (this.refundForm.invalid || amount <= 0 || amount > this.availableToRefund(payment)) return;

    void this.facade.refundPayment(payment.id, amount, reason).then((refunded) => {
      if (refunded) this.cancelRefund();
    });
  }

  markPaid(): void {
    void this.facade.markPaid().then((updated) => {
      if (updated) this.syncPaymentAmount(this.facade.invoice()?.outstandingAmount ?? 0);
    });
  }

  markOverdue(): void {
    void this.facade.markOverdue();
  }

  startVoid(): void {
    this.voiding = true;
    this.voidForm.reset({ reason: '' });
  }

  cancelVoid(): void {
    this.voiding = false;
    this.voidForm.reset({ reason: '' });
  }

  voidInvoice(): void {
    if (this.voidForm.invalid) return;
    const reason = this.voidForm.controls.reason.value;
    void this.facade.voidInvoice(reason).then((voided) => {
      if (voided) this.cancelVoid();
    });
  }

  canAcceptPayment(status: string, outstanding: number): boolean {
    return (status === 'Open' || status === 'Overdue') && outstanding > 0;
  }

  canRefund(payment: PaymentSummary): boolean {
    return (
      (payment.status === 'Paid' || payment.status === 'PartiallyRefunded') &&
      this.availableToRefund(payment) > 0
    );
  }

  availableToRefund(payment: PaymentSummary): number {
    return Math.round((payment.amount - payment.refundedAmount) * 100) / 100;
  }

  methodLabel(method: string): string {
    return PAYMENT_METHOD_LABELS[method as PaymentMethod] ?? statusLabel(method);
  }

  label(status: string): string {
    return statusLabel(status);
  }

  private syncPaymentAmount(outstanding: number): void {
    this.paymentForm.patchValue({ amount: Math.max(0, outstanding) });
  }
}
