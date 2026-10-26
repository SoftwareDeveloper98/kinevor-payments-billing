import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'demo-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
  ],
  template: `
    <header class="demo-topbar">
      <a class="demo-brand" routerLink="/billing">
        <span class="demo-logo" aria-hidden="true">K</span>
        <span class="demo-brand-name">Kinevor<span>Billing</span></span>
      </a>

      <nav class="demo-nav" aria-label="Demo sections">
        <a
          mat-button
          routerLink="/billing"
          routerLinkActive="active"
          [routerLinkActiveOptions]="{ exact: true }"
        >
          <mat-icon aria-hidden="true">space_dashboard</mat-icon>
          Overview
        </a>
        <a mat-button routerLink="/billing/invoices" routerLinkActive="active">
          <mat-icon aria-hidden="true">receipt_long</mat-icon>
          Invoices
        </a>
      </nav>

      <div class="demo-meta">
        <span class="api-pill"><i aria-hidden="true"></i>API 5087</span>
        <span class="demo-actor">Front desk · demo tenant</span>
        <span class="demo-badge">Demo</span>
      </div>
    </header>

    <main>
      <router-outlet />
    </main>
  `,
  styles: [`
    :host { display:block; min-height:100vh; }
    .demo-topbar { display:flex; align-items:center; gap:22px; padding:0 28px; height:66px; background:color-mix(in srgb,var(--color-surface-raised) 91%,transparent); border-bottom:1px solid var(--color-border); position:sticky; top:0; z-index:20; backdrop-filter:blur(16px); }
    .demo-brand { display:flex; align-items:center; gap:10px; text-decoration:none; color:var(--color-text); font-weight:800; font-size:16px; letter-spacing:-.3px; }
    .demo-brand-name span { color:var(--color-accent-text); margin-inline-start:4px; }
    .demo-logo { width:34px; height:34px; border-radius:10px; background:var(--gradient-brandWarm); color:var(--color-on-accent); display:flex; align-items:center; justify-content:center; font-weight:900; box-shadow:0 6px 18px color-mix(in srgb,var(--color-accent-fill) 20%,transparent); }
    .demo-nav { display:flex; gap:4px; margin-inline-start:auto; }
    .demo-nav a { color:var(--color-text-muted); font-size:12px; }
    .demo-nav a.active { color:var(--color-accent-text); background:var(--color-accent-soft); }
    .demo-nav mat-icon { margin-inline-end:6px; font-size:19px; width:19px; height:19px; }
    .demo-meta { display:flex; align-items:center; gap:10px; }
    .api-pill { display:inline-flex; align-items:center; gap:6px; padding:4px 9px; border-radius:999px; background:var(--color-surface); border:1px solid var(--color-border); font-size:9px; font-weight:700; letter-spacing:.08em; color:var(--color-text-muted); white-space:nowrap; }
    .api-pill i { width:6px; height:6px; border-radius:50%; background:var(--color-success); box-shadow:0 0 0 3px color-mix(in srgb,var(--color-success) 14%,transparent); }
    .demo-actor { font-size:10px; color:var(--color-text-muted); white-space:nowrap; }
    .demo-badge { font-size:9px; font-weight:700; text-transform:uppercase; letter-spacing:.1em; color:var(--color-accent-text); border:1px solid color-mix(in srgb,var(--color-accent-text) 55%,transparent); border-radius:999px; padding:4px 9px; }
    main { min-height:calc(100vh - 66px); }
    @media (max-width:1000px) { .demo-actor { display:none; } }
    @media (max-width:760px) { .demo-topbar { padding:0 14px; gap:10px; } .demo-nav a { font-size:0; min-width:0; padding:8px; } .demo-nav mat-icon { margin:0; } .demo-meta { margin-inline-start:auto; } .api-pill { display:none; } }
    @media (max-width:460px) { .demo-brand-name { display:none; } .demo-badge { display:none; } }
  `],
})
export class AppComponent {}
