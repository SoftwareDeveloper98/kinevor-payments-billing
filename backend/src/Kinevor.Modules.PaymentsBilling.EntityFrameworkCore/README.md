# Kinevor.Modules.PaymentsBilling.EntityFrameworkCore

EF Core persistence for the Kinevor payments and billing module. This package
provides `PaymentsBillingDbContext`, entity configurations and the invoice and
payment repositories used by the application layer.

## Install

```powershell
dotnet add package Kinevor.Modules.PaymentsBilling.EntityFrameworkCore
```

The package is published to GitHub Packages. Configure the
`SoftwareDeveloper98` source and authenticate with a token that can read
packages before restoring.

## Register

```csharp
using Kinevor.Modules.PaymentsBilling.DependencyInjection;
using Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<PaymentsBillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsBilling")));

builder.Services.AddPaymentsBillingModule();
builder.Services.AddPaymentsBillingEntityFrameworkCore();
```

`AddPaymentsBillingEntityFrameworkCore` registers `IInvoiceRepository`,
`IPaymentRepository` and `IPaymentsBillingUnitOfWork` as scoped services. The
unit of work resolves the registered `PaymentsBillingDbContext`; configure that
context for SQLite, PostgreSQL or another EF Core provider.

## Schema

The context maps `Invoices`, `InvoiceLines`, `Payments` and `PaymentRefunds`.
Generate migrations in the consuming application:

```powershell
dotnet ef migrations add InitialBilling
dotnet ef database update
```

The standalone demo uses `EnsureCreated` for a disposable SQLite database. Use
migrations, not `EnsureCreated`, for a deployed host.

## Licence

MIT.
