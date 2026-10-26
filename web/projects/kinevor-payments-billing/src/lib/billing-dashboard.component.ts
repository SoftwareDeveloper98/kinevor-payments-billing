import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BillingFacade } from './billing.facade';
import { RevenueBucket, statusLabel } from './billing.model';

@Component({
  selector: 'kinevor-billing-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <div class="page-container">
      <section class="hero">
        <div class="hero-copy">
          <span class="eyebrow"><span class="live-dot" aria-hidden="true"></span>Billing command centre</span>
          <h1>Every dollar,<br /><span>accounted for.</span></h1>
          <p>
            Invoices, simulated payments, refunds and revenue in one operational view.
            Built for the front desk and the finance team.
          </p>
          <div class="hero-actions">
            <a mat-flat-button color="primary" routerLink="/billing/invoices">
              <mat-icon aria-hidden="true">receipt_long</mat-icon>
              Open invoice ledger
            </a>
            <a mat-stroked-button routerLink="/billing/invoices" [queryParams]="{ status: 'Overdue' }">
              <mat-icon aria-hidden="true">schedule</mat-icon>
              Review overdue
            </a>
          </div>
        </div>

        <aside class="hero-panel" aria-label="Outstanding balance summary">
          <div class="hero-panel-head">
            <span>Outstanding balance</span>
            <span class="currency-tag">{{ stats()?.currency ?? 'USD' }}</span>
          </div>
          <strong class="hero-amount">
            {{ stats()?.outstandingAmount ?? 0 | currency: 'USD' : 'symbol' : '1.2-2' }}
          </strong>
          <div class="meter" aria-hidden="true">
            <span [style.width.%]="100 - collectionRate()"></span>
          </div>
          <div class="hero-panel-grid">
            <div>
              <span>{{ stats()?.openInvoiceCount ?? 0 }}</span>
              <small>Open invoices</small>
            </div>
            <div>
              <span class="danger">{{ stats()?.overdueInvoiceCount ?? 0 }}</span>
              <small>Overdue</small>
            </div>
            <div>
              <span>{{ collectionRate() }}%</span>
              <small>Collected</small>
            </div>
          </div>
          <p class="hero-note">
            <mat-icon aria-hidden="true">info</mat-icon>
            {{ overdueShare() }}% of the outstanding balance is past due.
          </p>
        </aside>
      </section>

      @if (facade.error(); as error) {
        <p class="notice notice-error" role="alert"><mat-icon aria-hidden="true">error</mat-icon>{{ error }}</p>
      }

      @if (facade.loading() && !stats()) {
        <div class="table-loading" role="status">
          <mat-spinner diameter="48" />
          <span>Loading billing pulse…</span>
        </div>
      }

      @if (stats(); as stats) {
        <section class="stats-grid" aria-label="Billing totals">
          <article class="metric-card">
            <span class="metric-icon metric-icon-accent" aria-hidden="true"><mat-icon>account_balance_wallet</mat-icon></span>
            <div>
              <span class="metric-label">Outstanding</span>
              <strong>{{ stats.outstandingAmount | currency: 'USD' : 'symbol' : '1.2-2' }}</strong>
              <small>{{ stats.openInvoiceCount + stats.overdueInvoiceCount }} unpaid invoices</small>
            </div>
          </article>
          <article class="metric-card">
            <span class="metric-icon metric-icon-warning" aria-hidden="true"><mat-icon>warning_amber</mat-icon></span>
            <div>
              <span class="metric-label">Overdue</span>
              <strong>{{ stats.overdueAmount | currency: 'USD' : 'symbol' : '1.2-2' }}</strong>
              <small>{{ stats.overdueInvoiceCount }} invoices need attention</small>
            </div>
          </article>
          <article class="metric-card">
            <span class="metric-icon metric-icon-success" aria-hidden="true"><mat-icon>trending_up</mat-icon></span>
            <div>
              <span class="metric-label">Collected this month</span>
              <strong>{{ stats.collectedThisMonth | currency: 'USD' : 'symbol' : '1.2-2' }}</strong>
              <small>{{ revenue()?.paymentCount ?? 0 }} captured payments</small>
            </div>
          </article>
          <article class="metric-card">
            <span class="metric-icon metric-icon-info" aria-hidden="true"><mat-icon>savings</mat-icon></span>
            <div>
              <span class="metric-label">Net this month</span>
              <strong>{{ stats.netThisMonth | currency: 'USD' : 'symbol' : '1.2-2' }}</strong>
              <small>{{ stats.refundedThisMonth | currency: 'USD' : 'symbol' : '1.2-2' }} refunded</small>
            </div>
          </article>
        </section>

        <div class="insight-grid">
          <mat-card class="gym-card revenue-card">
            <mat-card-content>
              <div class="section-head">
                <div>
                  <span class="eyebrow">Revenue</span>
                  <h2>Six-month cash flow</h2>
                </div>
                <span class="net-chip">
                  Net {{ revenue()?.netRevenue ?? 0 | currency: 'USD' : 'symbol' : '1.0-0' }}
                </span>
              </div>

              @if (revenue(); as revenue) {
                <div class="chart" role="img" aria-label="Gross collected revenue by month">
                  @for (bucket of revenue.buckets; track bucket.periodStart) {
                    <div class="chart-col">
                      <span class="chart-value">{{ bucket.grossCollected | currency: 'USD' : 'symbol' : '1.0-0' }}</span>
                      <div class="chart-rail">
                        <span
                          class="chart-bar"
                          [style.height.%]="barHeight(bucket)"
                          [class.chart-bar-soft]="bucket.grossCollected === 0"
                        ></span>
                        @if (bucket.refunded > 0) {
                          <span class="chart-refund" [style.height.%]="refundHeight(bucket)"></span>
                        }
                      </div>
                      <span class="chart-label">{{ bucket.label }}</span>
                    </div>
                  }
                </div>
                <mat-divider />
                <div class="revenue-foot">
                  <div><span>Gross</span><strong>{{ revenue.grossCollected | currency: 'USD' : 'symbol' : '1.2-2' }}</strong></div>
                  <div><span>Refunded</span><strong class="danger">{{ revenue.refunded | currency: 'USD' : 'symbol' : '1.2-2' }}</strong></div>
                  <div><span>Net revenue</span><strong>{{ revenue.netRevenue | currency: 'USD' : 'symbol' : '1.2-2' }}</strong></div>
                </div>
              } @else {
                <p class="muted-note">Revenue data is not available yet.</p>
              }
            </mat-card-content>
          </mat-card>

          <mat-card class="gym-card health-card">
            <mat-card-content>
              <span class="eyebrow">Collection health</span>
              <div class="health-ring" [style.--progress.%]="collectionRate()">
                <div>
                  <strong>{{ collectionRate() }}%</strong>
                  <span>paid</span>
                </div>
              </div>
              <div class="health-list">
                <div>
                  <span><i class="dot dot-ok"></i>Paid invoices</span>
                  <strong>{{ stats.paidInvoiceCount }}</strong>
                </div>
                <div>
                  <span><i class="dot dot-warn"></i>Open invoices</span>
                  <strong>{{ stats.openInvoiceCount }}</strong>
                </div>
                <div>
                  <span><i class="dot dot-danger"></i>Overdue invoices</span>
                  <strong>{{ stats.overdueInvoiceCount }}</strong>
                </div>
                <div>
                  <span><i class="dot dot-muted"></i>Total invoices</span>
                  <strong>{{ stats.totalInvoiceCount }}</strong>
                </div>
              </div>
              <div class="health-foot">
                <mat-icon aria-hidden="true">verified</mat-icon>
                All amounts in {{ stats.currency }}
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <mat-card class="gym-card recent-card">
          <mat-card-content>
            <div class="section-head">
              <div>
                <span class="eyebrow">Latest activity</span>
                <h2>Recent invoices</h2>
              </div>
              <a mat-button color="primary" routerLink="/billing/invoices">
                View all
                <mat-icon aria-hidden="true">arrow_forward</mat-icon>
              </a>
            </div>

            @if (facade.recentInvoices().length > 0) {
              <div class="table-wrap">
                <table class="billing-table">
                  <thead>
                    <tr>
                      <th scope="col">Invoice</th>
                      <th scope="col">Member</th>
                      <th scope="col">Issued</th>
                      <th scope="col">Due</th>
                      <th scope="col" class="align-end">Total</th>
                      <th scope="col" class="align-end">Outstanding</th>
                      <th scope="col">Status</th>
                      <th scope="col"><span class="sr-only">Open</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (invoice of facade.recentInvoices(); track invoice.id) {
                      <tr>
                        <td><a class="invoice-link mono" [routerLink]="['/billing/invoices', invoice.id]">{{ invoice.number }}</a></td>
                        <td>{{ invoice.memberName }}</td>
                        <td>{{ invoice.issuedAt | date: 'mediumDate' }}</td>
                        <td>{{ invoice.dueAt | date: 'mediumDate' }}</td>
                        <td class="align-end amount">{{ invoice.total | currency: 'USD' : 'symbol' : '1.2-2' }}</td>
                        <td class="align-end amount">{{ invoice.outstandingAmount | currency: 'USD' : 'symbol' : '1.2-2' }}</td>
                        <td>
                          <span class="status-chip" [class]="'status-' + invoice.status.toLowerCase()">
                            {{ status(invoice.status) }}
                          </span>
                        </td>
                        <td class="align-end">
                          <a mat-icon-button [routerLink]="['/billing/invoices', invoice.id]" aria-label="Open invoice">
                            <mat-icon aria-hidden="true">chevron_right</mat-icon>
                          </a>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            } @else {
              <div class="empty-inline">
                <mat-icon aria-hidden="true">receipt_long</mat-icon>
                <span>No invoices yet. Seed the demo or create the first invoice.</span>
              </div>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .hero { position:relative; overflow:hidden; display:grid; grid-template-columns:1fr; gap:24px; padding:32px; border-radius:28px; background:radial-gradient(circle at 82% 8%, color-mix(in srgb, var(--color-accent-fill) 20%, transparent), transparent 44%), linear-gradient(135deg, var(--color-surface-raised), var(--color-surface)); border:1px solid var(--color-border); margin-bottom:18px; }
    .hero::after { content:'receipt_long'; font-family:'Material Icons'; position:absolute; right:-24px; bottom:-70px; font-size:260px; color:color-mix(in srgb, var(--color-text) 4%, transparent); pointer-events:none; }
    .hero-copy { position:relative; z-index:1; display:flex; flex-direction:column; gap:14px; align-items:flex-start; }
    .eyebrow { display:inline-flex; align-items:center; gap:8px; font-size:11px; font-weight:700; letter-spacing:.16em; text-transform:uppercase; color:var(--color-accent-text); }
    .live-dot { width:8px; height:8px; border-radius:50%; background:var(--color-accent-fill); animation:live 2.2s ease-out infinite; }
    @keyframes live { 0%,100% { box-shadow:0 0 0 0 color-mix(in srgb, var(--color-accent-fill) 65%, transparent); } 50% { box-shadow:0 0 0 6px transparent; } }
    .hero h1 { margin:0; font-size:clamp(34px,4.5vw,56px); line-height:1.03; letter-spacing:-.045em; font-weight:800; }
    .hero h1 span { color:var(--color-accent-text); }
    .hero-copy > p { margin:0; max-width:50ch; color:var(--color-text-muted); line-height:1.7; }
    .hero-actions { display:flex; gap:10px; flex-wrap:wrap; margin-top:6px; }
    .hero-panel { position:relative; z-index:1; padding:22px; border-radius:20px; background:color-mix(in srgb, var(--color-ink) 74%, transparent); border:1px solid var(--color-border); backdrop-filter:blur(10px); display:flex; flex-direction:column; gap:12px; align-self:end; }
    .hero-panel-head { display:flex; align-items:center; justify-content:space-between; gap:8px; font-size:12px; font-weight:600; color:var(--color-text-muted); }
    .currency-tag { font-size:10px; font-weight:800; letter-spacing:.12em; color:var(--color-accent-text); background:var(--color-accent-soft); padding:4px 8px; border-radius:999px; }
    .hero-amount { font-size:clamp(32px,4vw,44px); letter-spacing:-.04em; line-height:1; font-variant-numeric:tabular-nums; }
    .meter { height:7px; border-radius:999px; background:var(--color-surface-raised); overflow:hidden; }
    .meter span { display:block; height:100%; border-radius:inherit; background:linear-gradient(90deg,var(--color-accent-pressed),var(--color-accent-fill)); }
    .hero-panel-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:10px; }
    .hero-panel-grid div { display:flex; flex-direction:column; gap:2px; padding:10px; border-radius:12px; background:color-mix(in srgb, var(--color-surface) 80%, transparent); }
    .hero-panel-grid span { font-size:20px; font-weight:800; font-variant-numeric:tabular-nums; }
    .hero-panel-grid small { font-size:10px; color:var(--color-text-muted); text-transform:uppercase; letter-spacing:.06em; }
    .hero-note { display:flex; align-items:center; gap:6px; margin:0; font-size:11px; color:var(--color-text-muted); }
    .hero-note mat-icon { font-size:14px; width:14px; height:14px; }
    .danger { color:var(--color-error) !important; }
    .stats-grid { display:grid; grid-template-columns:1fr; gap:12px; margin-bottom:18px; }
    .metric-card { display:flex; gap:14px; padding:18px; border-radius:18px; background:var(--color-surface-raised); border:1px solid var(--color-border); }
    .metric-card > div { display:flex; flex-direction:column; gap:3px; min-width:0; }
    .metric-icon { width:44px; height:44px; border-radius:13px; display:inline-flex; align-items:center; justify-content:center; flex:none; }
    .metric-icon mat-icon { font-size:22px; width:22px; height:22px; }
    .metric-icon-accent { background:var(--color-accent-soft); color:var(--color-accent-text); }
    .metric-icon-warning { background:color-mix(in srgb,var(--color-warning) 14%,transparent); color:var(--color-warning); }
    .metric-icon-success { background:color-mix(in srgb,var(--color-success) 14%,transparent); color:var(--color-success); }
    .metric-icon-info { background:color-mix(in srgb,var(--color-info) 14%,transparent); color:var(--color-info); }
    .metric-label { font-size:10px; font-weight:700; text-transform:uppercase; letter-spacing:.09em; color:var(--color-text-muted); }
    .metric-card strong { font-size:24px; letter-spacing:-.03em; line-height:1.15; font-variant-numeric:tabular-nums; }
    .metric-card small { font-size:11px; color:var(--color-text-muted); }
    .insight-grid { display:grid; grid-template-columns:1fr; gap:16px; margin-bottom:18px; align-items:stretch; }
    .revenue-card mat-card-content, .health-card mat-card-content, .recent-card mat-card-content { padding:24px !important; }
    .section-head { display:flex; align-items:flex-start; justify-content:space-between; gap:12px; flex-wrap:wrap; }
    .section-head h2 { margin:5px 0 0; font-size:20px; letter-spacing:-.03em; }
    .net-chip { font-size:11px; font-weight:700; padding:7px 11px; border-radius:999px; background:color-mix(in srgb,var(--color-success) 13%,transparent); color:var(--color-success); white-space:nowrap; }
    .chart { display:grid; grid-template-columns:repeat(6,1fr); gap:10px; align-items:end; height:236px; padding:22px 4px 0; }
    .chart-col { display:flex; flex-direction:column; align-items:center; gap:6px; height:100%; min-width:0; }
    .chart-value { font-size:10px; color:var(--color-text-muted); font-variant-numeric:tabular-nums; white-space:nowrap; }
    .chart-rail { flex:1; width:100%; max-width:46px; display:flex; align-items:flex-end; justify-content:center; gap:2px; position:relative; border-radius:10px; background:linear-gradient(to top, color-mix(in srgb,var(--color-surface) 60%,transparent), transparent); }
    .chart-bar { width:72%; min-height:3px; border-radius:8px 8px 3px 3px; background:linear-gradient(to top,var(--color-accent-pressed),var(--color-accent-fill)); transition:height .35s ease; }
    .chart-bar-soft { background:var(--color-border); }
    .chart-refund { width:14%; min-height:2px; border-radius:6px 6px 2px 2px; background:color-mix(in srgb,var(--color-warning) 70%,transparent); }
    .chart-label { font-size:10px; font-weight:600; color:var(--color-text-muted); white-space:nowrap; }
    .revenue-foot { display:flex; gap:20px; flex-wrap:wrap; padding-top:14px; }
    .revenue-foot div { display:flex; flex-direction:column; gap:2px; }
    .revenue-foot span { font-size:10px; text-transform:uppercase; letter-spacing:.07em; color:var(--color-text-muted); }
    .revenue-foot strong { font-size:14px; font-variant-numeric:tabular-nums; }
    .health-card mat-card-content { display:flex; flex-direction:column; gap:16px; height:100%; }
    .health-ring { width:148px; height:148px; border-radius:50%; margin:4px auto 2px; background:conic-gradient(var(--color-accent-fill) calc(var(--progress) * 1%), var(--color-surface) 0); display:grid; place-items:center; position:relative; }
    .health-ring::after { content:''; position:absolute; inset:11px; border-radius:50%; background:var(--color-surface-raised); }
    .health-ring > div { position:relative; z-index:1; display:flex; flex-direction:column; align-items:center; }
    .health-ring strong { font-size:30px; letter-spacing:-.04em; }
    .health-ring span { font-size:10px; text-transform:uppercase; letter-spacing:.1em; color:var(--color-text-muted); }
    .health-list { display:flex; flex-direction:column; }
    .health-list div { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:8px 0; border-bottom:1px solid var(--color-border); font-size:12px; }
    .health-list div:last-child { border-bottom:none; }
    .health-list span { display:inline-flex; align-items:center; gap:7px; color:var(--color-text-muted); }
    .health-list strong { font-variant-numeric:tabular-nums; }
    .dot { width:7px; height:7px; border-radius:50%; display:inline-block; }
    .dot-ok { background:var(--color-success); }
    .dot-warn { background:var(--color-warning); }
    .dot-danger { background:var(--color-error); }
    .dot-muted { background:var(--color-text-muted); }
    .health-foot { display:flex; align-items:center; gap:6px; margin-top:auto; padding-top:12px; border-top:1px solid var(--color-border); font-size:11px; color:var(--color-text-muted); }
    .health-foot mat-icon { font-size:15px; width:15px; height:15px; color:var(--color-success); }
    .recent-card mat-card-content { padding-bottom:12px !important; }
    .table-wrap { overflow-x:auto; margin-top:16px; }
    .billing-table { width:100%; border-collapse:collapse; font-size:12px; min-width:780px; }
    .billing-table th { padding:10px 12px; text-align:start; font-size:10px; font-weight:700; text-transform:uppercase; letter-spacing:.08em; color:var(--color-text-muted); border-bottom:1px solid var(--color-border); white-space:nowrap; }
    .billing-table td { padding:12px; border-bottom:1px solid var(--color-border); vertical-align:middle; }
    .billing-table tr:last-child td { border-bottom:none; }
    .billing-table tbody tr:hover { background:var(--color-accent-soft); }
    .align-end { text-align:end !important; }
    .amount { font-variant-numeric:tabular-nums; font-weight:600; white-space:nowrap; }
    .mono { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:11px; }
    .invoice-link { color:var(--color-text); font-weight:700; }
    .invoice-link:hover { color:var(--color-accent-text); }
    .status-chip { display:inline-flex; align-items:center; padding:4px 8px; border-radius:999px; font-size:9px; font-weight:700; letter-spacing:.04em; white-space:nowrap; border:1px solid var(--color-border); background:var(--color-surface); color:var(--color-text-muted); }
    .status-open { color:var(--color-info); border-color:color-mix(in srgb,var(--color-info) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-info) 10%,transparent); }
    .status-overdue { color:var(--color-warning); border-color:color-mix(in srgb,var(--color-warning) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-warning) 10%,transparent); }
    .status-paid { color:var(--color-success); border-color:color-mix(in srgb,var(--color-success) 35%,var(--color-border)); background:color-mix(in srgb,var(--color-success) 10%,transparent); }
    .status-void { color:var(--color-text-muted); background:var(--color-surface); }
    .empty-inline { display:flex; align-items:center; gap:10px; padding:28px 4px; color:var(--color-text-muted); font-size:13px; }
    .empty-inline mat-icon { color:var(--color-accent-text); }
    @media (min-width:640px) { .stats-grid { grid-template-columns:repeat(2,minmax(0,1fr)); } }
    @media (min-width:900px) { .hero { grid-template-columns:1.25fr .75fr; padding:42px; } .insight-grid { grid-template-columns:1.5fr .5fr; } .stats-grid { grid-template-columns:repeat(4,minmax(0,1fr)); } }
    @media (prefers-reduced-motion:reduce) { .live-dot { animation:none; } .chart-bar { transition:none; } }
  `],
})
export class BillingDashboardComponent implements OnInit {
  readonly facade = inject(BillingFacade);
  readonly stats = this.facade.stats;
  readonly revenue = this.facade.revenue;
  readonly collectionRate = this.facade.collectionRate;
  readonly overdueShare = this.facade.overdueShare;
  readonly revenuePeak = this.facade.revenuePeak;

  ngOnInit(): void {
    void this.facade.loadDashboard();
  }

  barHeight(bucket: RevenueBucket): number {
    if (bucket.grossCollected <= 0) return 3;
    return Math.max(6, Math.round((bucket.grossCollected / this.revenuePeak()) * 100));
  }

  refundHeight(bucket: RevenueBucket): number {
    if (bucket.refunded <= 0) return 0;
    return Math.max(2, Math.round((bucket.refunded / this.revenuePeak()) * 100));
  }

  status(status: string): string {
    return statusLabel(status);
  }
}
