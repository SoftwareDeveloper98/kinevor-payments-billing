using Kinevor.Platform.Api;
using Kinevor.Platform.Application.Exceptions;

namespace Kinevor.Modules.PaymentsBilling.DemoApi.Middleware;

/// <summary>Maps module exceptions to the shared API envelope with the right status code.</summary>
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    /// <summary>Invokes the next middleware and translates known exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, message) = exception switch
            {
                DomainException => (StatusCodes.Status400BadRequest, exception.Message),
                NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message));
        }
    }
}
