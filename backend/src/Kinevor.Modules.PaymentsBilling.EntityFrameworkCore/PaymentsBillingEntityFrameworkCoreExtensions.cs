using Kinevor.Modules.PaymentsBilling.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;

/// <summary>Registers payments and billing persistence services.</summary>
public static class PaymentsBillingEntityFrameworkCoreExtensions
{
    /// <summary>Adds repositories and the module unit of work.</summary>
    public static IServiceCollection AddPaymentsBillingEntityFrameworkCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentsBillingUnitOfWork>(provider =>
            provider.GetRequiredService<PaymentsBillingDbContext>());
        return services;
    }
}
