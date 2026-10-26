# Kinevor.Modules.PaymentsBilling

Domain and application layer for Kinevor invoices, simulated payments, refunds
and revenue reporting. The package contains the `Invoice` and `Payment`
aggregates, MediatR commands and queries, DTOs and the staff/member API
controllers.

## Install

```powershell
dotnet add package Kinevor.Modules.PaymentsBilling
```

The package is published to GitHub Packages. Configure the
`SoftwareDeveloper98` source and authenticate with a token that can read
packages before restoring.

## Register

```csharp
using System.Text.Json.Serialization;
using Kinevor.Modules.PaymentsBilling.Api;
using Kinevor.Modules.PaymentsBilling.DependencyInjection;

builder.Services.AddPaymentsBillingModule();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddApplicationPart(typeof(InvoicesController).Assembly);
```

`AddPaymentsBillingModule` registers the MediatR handlers and `TimeProvider`.
The host must also register `ICurrentUser`, `IBranchContext`, authorization
policies, authentication and the repository implementations. The companion
`Kinevor.Modules.PaymentsBilling.EntityFrameworkCore` package supplies EF Core
repositories and the unit of work.

## API surface

- Staff: `/api/v1/billing/stats`, `/revenue`, `/invoices` and `/payments`.
- Member: `/api/v1/me/invoices`, `/me/invoices/summary`,
  `/me/invoices/{id}/pay` and `/me/payments`.
- Policies: `StaffOrAdmin` for staff routes and `MemberOrAbove` for member
  routes.
- Responses use the `Kinevor.Platform` `ApiResponse<T>` envelope; enums are
  serialized as strings.

## Behaviour

- Invoice totals are derived from lines, including quantity, unit price and tax
  rate. Currency codes are validated against ISO 4217 minor units.
- Payments support `Card`, `Cash` and `BankTransfer`, reference-level
  idempotency, ownership checks and outstanding-balance validation.
- Refunds are recorded against the captured payment and enforce the remaining
  refundable amount.
- Invoice lifecycle transitions cover mark-paid, mark-overdue and void.
- Member queries bind the authenticated member id into every lookup.

The demo API, seed data and Swagger explorer live in the repository's
`backend/samples/Kinevor.Modules.PaymentsBilling.DemoApi` project.

## Licence

MIT.
