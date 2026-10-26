import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { BillingFacade } from './billing.facade';
import { INVOICE_STATUSES, InvoiceStatus, statusLabel } from './billing.model';

@Component({
  selector: 'kinevor-invoice-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  template: `
    <div class="page-container">
      <header class="page-head">
        <div>
          <span class="eyebrow">Invoice ledger</span>
          <h1>Invoices</h1>
          <p>Find any invoice, track its balance and move straight into collection.</p>
        </div>
        <div class="page-actions">
          <a mat-stroked-button routerLink="/billing">
            <mat-icon aria-hidden="true">dashboard</mat-icon>
            Dashboard
          </a>
          <button mat-flat-button color="primary" type="button" (click)="facade.loadInvoices()">
            <mat-icon aria-hidden="true">refresh</mat-icon>
            Refresh
          </button>
        </div>
      </header>

      @if (facade.stats(); as stats) {
        <section class="ledger-stats" aria-label="Invoice totals">
          <div><span>Total</span><strong>{{ stats.totalInvoiceCount }}</strong><small>invoices</small></div>
          <div><span>Open</span><strong>{{ stats.openInvoiceCount }}</strong><small>awaiting payment</small></div>
          <div><span>Overdue</span><strong class="danger">{{ stats.overdueInvoiceCount }}</strong><small>past due</small></div>
          <div><span>Outstanding</span><strong>{{ stats.outstandingAmount | currency: 'USD' : 'symbol' : '1.2-2' }}</strong><small>{{ stats.currency }}</small></div>
        </section>
      }

      <mat-card class="gym-card ledger-card">
        <mat-card-content>
          <form class="filter-bar" [formGroup]="filters" (ngSubmit)="applyFilters()">
            <mat-form-field appearance="outline" subscriptSizing="dynamic" class="search-field">
              <mat-label>Search invoices</mat-label>
              <mat-icon matPrefix aria-hidden="true">search</mat-icon>
              <input matInput formControlName="search" placeholder="Invoice number or member" autocomplete="off" />
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic" class="status-field">
              <mat-label>Status</mat-label>
              <mat-select formControlName="status">
                <mat-option value="">All statuses</mat-option>
                @for (status of statuses; track status) {
                  <mat-option [value]="status">{{ label(status) }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <button mat-flat-button color="primary" type="submit">Apply</button>
            @if (hasFilters()) {
              <button mat-button type="button" (click)="clearFilters()">Clear</button>
            }
          </form>

          @if (facade.error(); as error) {
            <p class="notice notice-error" role="alert"><mat-icon aria-hidden="true">error</mat-icon>{{ error }}</p>
          }

          @if (facade.loading()) {
            <div class="table-loading" role="status">
              <mat-spinner diameter="44" />
              <span>Loading invoices…</span>
            </div>
          } @else if (facade.invoices().length > 0) {
            <div class="table-wrap">
              <table class="billing-table">
                <thead>
                  <tr>
                    <th scope="col">Invoice</th>
                    <th scope="col">Member</th>
                    <th scope="col">Issued</th>
                    <th scope="col">Due</th>
                    <th scope="col" class="align-end">Total</th>
                    <th scope="col" class="align-end">Paid</th>
                    <th scope="col" class="align-end">Outstanding</th>
                    <th scope="col">Status</th>
                    <th scope="col"><span class="sr-only">Open</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (invoice of facade.invoices(); track invoice.id) {
                    <tr>
                      <td>
                        <a class="invoice-link mono" [routerLink]="['/billing/invoices', invoice.id]">{{ invoice.number }}</a>
                        <small>{{ invoice.lineCount }} line{{ invoice.lineCount === 1 ? '' : 's' }}</small>
                      </td>
                      <td>
                        <span class="member-name">{{ invoice.memberName }}</span>
                        @if (invoice.memberEmail) {
                          <small>{{ invoice.memberEmail }}</small>
                        }
                      </td>
                      <td>{{ invoice.issuedAt | date: 'mediumDate' }}</td>
                      <td>{{ invoice.dueAt | date: 'mediumDate' }}</td>
                      <td class="align-end amount">{{ invoice.total | currency: 'USD' : 'symbol' : '1.2-2' }}</td>
                      <td class="align-end amount muted">{{ invoice.amountPaid | currency: 'USD' : 'symbol' : '1.2-2' }}</td>
                      <td class="align-end amount" [class.danger]="invoice.outstandingAmount > 0">
                        {{ invoice.outstandingAmount | currency: 'USD' : 'symbol' : '1.2-2' }}
                      </td>
                      <td>
                        <span class="status-chip" [class]="'status-' + invoice.status.toLowerCase()">
                          {{ label(invoice.status) }}
                        </span>
                      </td>
                      <td class="align-end">
                        <a mat-icon-button [routerLink]="['/billing/invoices', invoice.id]" [attr.aria-label]="'Open ' + invoice.number">
                          <mat-icon aria-hidden="true">chevron_right</mat-icon>
                        </a>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <footer class="pager">
              <span>Showing {{ rangeStart() }}–{{ rangeEnd() }} of {{ facade.total() }} invoices</span>
              <div>
                <button mat-icon-button type="button" [disabled]="!canPrev()" (click)="setPage(currentPage() - 1)" aria-label="Previous page">
                  <mat-icon aria-hidden="true">chevron_left</mat-icon>
                </button>
                <span class="page-count">Page {{ currentPage() }} of {{ totalPages() }}</span>
                <button mat-icon-button type="button" [disabled]="!canNext()" (click)="setPage(currentPage() + 1)" aria-label="Next page">
                  <mat-icon aria-hidden="true">chevron_right</mat-icon>
                </button>
              </div>
            </footer>
          } @else {
            <div class="empty-state">
              <mat-icon aria-hidden="true">receipt_long</mat-icon>
              <h2>No matching invoices</h2>
              <p>Try a different search term or clear the status filter.</p>
              <button mat-stroked-button type="button" (click)="clearFilters()">Reset filters</button>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .page-head { display:flex; align-items:flex-end; justify-content:space-between; gap:20px; flex-wrap:wrap; margin-bottom:18px; }
    .eyebrow { display:inline-flex; align-items:center; gap:7px; font-size:11px; font-weight:700; letter-spacing:.16em; text-transform:uppercase; color:var(--color-accent-text); }
    h1 { margin:6px 0 4px; font-size:clamp(34px,4vw,48px); line-height:1; letter-spacing:-.045em; }
    .page-head p { margin:0; color:var(--color-text-muted); font-size:13px; }
    .page-actions { display:flex; gap:8px; flex-wrap:wrap; }
    .ledger-stats { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:1px; overflow:hidden; border:1px solid var(--color-border); border-radius:18px; background:var(--color-border); margin-bottom:18px; }
    .ledger-stats div { display:flex; flex-direction:column; gap:2px; padding:15px 18px; background:var(--color-surface-raised); }
    .ledger-stats span { font-size:10px; font-weight:700; text-transform:uppercase; letter-spacing:.09em; color:var(--color-text-muted); }
    .ledger-stats strong { font-size:25px; line-height:1.1; letter-spacing:-.03em; font-variant-numeric:tabular-nums; }
    .ledger-stats small { font-size:10px; color:var(--color-text-muted); }
    .danger { color:var(--color-error) !important; }
    .ledger-card mat-card-content { padding:22px !important; }
    .filter-bar { display:flex; align-items:center; gap:10px; flex-wrap:wrap; margin-bottom:16px; }
    .search-field { flex:1 1 280px; min-width:220px; }
    .status-field { flex:0 1 190px; min-width:160px; }
    .table-wrap { overflow-x:auto; }
    .billing-table { width:100%; border-collapse:collapse; font-size:12px; min-width:940px; }
    .billing-table th { padding:10px 12px; text-align:start; font-size:10px; font-weight:700; text-transform:uppercase; letter-spacing:.08em; color:var(--color-text-muted); border-bottom:1px solid var(--color-border); white-space:nowrap; }
    .billing-table td { padding:12px; border-bottom:1px solid var(--color-border); vertical-align:middle; }
    .billing-table tbody tr:last-child td { border-bottom:none; }
    .billing-table tbody tr:hover { background:var(--color-accent-soft); }
    .billing-table td small { display:block; margin-top:2px; color:var(--color-text-muted); font-size:10px; }
    .billing-table .member-name { font-weight:600; }
    .align-end { text-align:end !important; }
    .amount { font-variant-numeric:tabular-nums; font-weight:600; white-space:nowrap; }
    .muted { color:var(--color-text-muted); font-weight:500; }
    .mono { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:11px; }
    .invoice-link { color:var(--color-text); font-weight:700; }
    .invoice-link:hover { color:var(--color-accent-text); }
    .status-chip { display:inline-flex; align-items:center; padding:4px 8px; border-radius:999px; font-size:9px; font-weight:700; letter-spacing:.04em; white-space:nowrap; border:1px solid var(--color-border); background:var(--color-surface); color:var(--color-text-muted); }
    .status-open { color:var(--color-info); border-color:color-mix(in srgb,var(--color-info) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-info) 10%,transparent); }
    .status-overdue { color:var(--color-warning); border-color:color-mix(in srgb,var(--color-warning) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-warning) 10%,transparent); }
    .status-paid { color:var(--color-success); border-color:color-mix(in srgb,var(--color-success) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-success) 10%,transparent); }
    .status-void { color:var(--color-text-muted); background:var(--color-surface); }
    .pager { display:flex; align-items:center; justify-content:space-between; gap:12px; flex-wrap:wrap; padding-top:14px; margin-top:2px; border-top:1px solid var(--color-border); font-size:11px; color:var(--color-text-muted); }
    .pager > div { display:flex; align-items:center; gap:4px; }
    .page-count { min-width:100px; text-align:center; font-weight:600; color:var(--color-text); }
    .empty-state { display:flex; flex-direction:column; align-items:center; text-align:center; gap:8px; padding:54px 16px; }
    .empty-state mat-icon { font-size:44px; width:44px; height:44px; color:var(--color-accent-text); }
    .empty-state h2 { margin:0; font-size:18px; }
    .empty-state p { margin:0 0 8px; color:var(--color-text-muted); font-size:12px; }
    @media (min-width:760px) { .ledger-stats { grid-template-columns:repeat(4,minmax(0,1fr)); } }
  `],
})
export class InvoiceListComponent implements OnInit {
  readonly facade = inject(BillingFacade);
  readonly statuses = INVOICE_STATUSES;
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly filters = this.formBuilder.nonNullable.group({
    search: '',
    status: '' as InvoiceStatus | '',
  });

  ngOnInit(): void {
    const requestedStatus = this.route.snapshot.queryParamMap.get('status');
    const status = INVOICE_STATUSES.find((item) => item === requestedStatus);
    if (status) {
      this.filters.controls.status.setValue(status);
      void this.facade.updateQuery({ status, page: 1 });
    } else {
      void this.facade.loadInvoices();
    }

    if (!this.facade.stats()) {
      void this.facade.loadDashboard();
    }

    this.filters.controls.search.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.applyFilters());
  }

  applyFilters(): void {
    const { search, status } = this.filters.getRawValue();
    void this.facade.updateQuery({
      search: search.trim() || undefined,
      status: status || undefined,
      page: 1,
    });
  }

  clearFilters(): void {
    this.filters.reset({ search: '', status: '' });
    void this.facade.updateQuery({ search: undefined, status: undefined, page: 1 });
  }

  hasFilters(): boolean {
    const { search, status } = this.filters.getRawValue();
    return search.trim().length > 0 || status !== '';
  }

  setPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    void this.facade.updateQuery({ page });
  }

  currentPage(): number {
    return this.facade.query().page ?? 1;
  }

  totalPages(): number {
    const pageSize = this.facade.query().pageSize ?? 20;
    return Math.max(1, Math.ceil(this.facade.total() / pageSize));
  }

  rangeStart(): number {
    if (this.facade.total() === 0) return 0;
    return (this.currentPage() - 1) * (this.facade.query().pageSize ?? 20) + 1;
  }

  rangeEnd(): number {
    return Math.min(this.currentPage() * (this.facade.query().pageSize ?? 20), this.facade.total());
  }

  canPrev(): boolean {
    return this.currentPage() > 1;
  }

  canNext(): boolean {
    return this.currentPage() < this.totalPages();
  }

  label(status: string): string {
    return statusLabel(status);
  }
}
