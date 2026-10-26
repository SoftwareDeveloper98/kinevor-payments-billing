import {
  EnvironmentProviders,
  Provider,
  Type,
  makeEnvironmentProviders,
} from '@angular/core';
import { BillingGateway, BILLING_API_BASE_URL, BILLING_GATEWAY } from './billing.model';
import { BillingFacade } from './billing.facade';
import { HttpBillingGateway } from './http-billing.gateway';

export interface BillingOptions {
  readonly apiBaseUrl?: string;
  readonly gateway?: Type<BillingGateway>;
}

export function provideBilling(options: BillingOptions = {}): EnvironmentProviders {
  const providers: (Provider | EnvironmentProviders)[] = [
    { provide: BILLING_GATEWAY, useClass: options.gateway ?? HttpBillingGateway },
    BillingFacade,
  ];

  if (options.apiBaseUrl) {
    providers.push({ provide: BILLING_API_BASE_URL, useValue: options.apiBaseUrl });
  }

  return makeEnvironmentProviders(providers);
}
