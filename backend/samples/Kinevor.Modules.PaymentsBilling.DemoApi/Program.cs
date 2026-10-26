using System.Text.Json.Serialization;
using Kinevor.Modules.PaymentsBilling.DemoApi.Authentication;
using Kinevor.Modules.PaymentsBilling.DemoApi.Authorization;
using Kinevor.Modules.PaymentsBilling.DemoApi.Data;
using Kinevor.Modules.PaymentsBilling.DemoApi.Middleware;
using Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;
using Kinevor.Modules.PaymentsBilling.DependencyInjection;
using Kinevor.Platform.Api;
using Kinevor.Platform.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("PaymentsBilling")
    ?? "Data Source=payments-billing-demo.db";

builder.Services.AddDbContext<PaymentsBillingDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddPaymentsBillingModule();
builder.Services.AddPaymentsBillingEntityFrameworkCore();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IBranchContext, HttpBranchContext>();

builder.Services
    .AddAuthentication(DemoAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
        DemoAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options => options.AddKinevorPolicies());

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddApplicationPart(typeof(Kinevor.Modules.PaymentsBilling.Api.InvoicesController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Kinevor Payments & Billing Module API",
        Version = "v1",
        Description = "Standalone invoices, simulated payments, refunds, member balances and revenue reporting.",
    });
    options.AddSecurityDefinition("DemoRole", new OpenApiSecurityScheme
    {
        Name = "X-Demo-Role",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Demo role: Admin, Staff or Member. Members also send X-Demo-Member.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "DemoRole",
            },
        }] = Array.Empty<string>(),
    });
});

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins("http://localhost:4206")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsBillingDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoDataSeeder.SeedAsync(db);
}

app.UseCors("web");
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.Run();

/// <summary>Entry point marker used by integration tests.</summary>
public partial class Program { }
