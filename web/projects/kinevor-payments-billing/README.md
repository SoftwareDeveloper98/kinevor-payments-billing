# Kinevor Payments & Billing Web

Angular 21 screens for invoices, simulated payments, refunds and revenue reporting.

## GitHub Packages access

This package is private. Configure the scope and token before installing:

```ini
@softwaredeveloper98:registry=https://npm.pkg.github.com
//npm.pkg.github.com/:_authToken=${KINEVOR_PACKAGES_TOKEN}
```

## Install

```bash
npm install @softwaredeveloper98/kinevor-payments-billing-web
```

Peer dependencies: Angular 21, Angular Material 21, RxJS 7.8 and
`@softwaredeveloper98/kinevor-platform-web`.

## Register

```ts
import { provideBilling } from '@softwaredeveloper98/kinevor-payments-billing-web';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    provideBilling({ apiBaseUrl: 'https://api.example.com/api/v1' }),
  ],
});
```

Register `BILLING_ROUTES` under an authenticated shell. They expose `/billing`,
`/billing/invoices` and `/billing/invoices/:id`. The host must provide Angular
Material theming and the Kinevor theme tokens used by the components.

## Replace the transport

Provide a custom `BillingGateway` with `provideBilling({ gateway: MyGateway })`
to connect the screens to another transport.

## Demo

```bash
npm run install:local
npm start
```

The demo expects the standalone API at `http://localhost:5087`.
