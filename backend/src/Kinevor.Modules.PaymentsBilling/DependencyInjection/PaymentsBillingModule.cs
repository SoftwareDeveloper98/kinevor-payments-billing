using Kinevor.Modules.PaymentsBilling.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Kinevor.Modules.PaymentsBilling.DependencyInjection;

/// <summary>Registers the payments and billing application services.</summary>
public static class PaymentsBillingModule
{
    /// <summary>Adds MediatR handlers and the system clock.</summary>
    public static IServiceCollection AddPaymentsBillingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<GetInvoicesHandler>());
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
