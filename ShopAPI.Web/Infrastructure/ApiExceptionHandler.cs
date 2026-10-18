using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShopAPI.Core.Exceptions;

namespace ShopAPI.Web.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ShopException shop => (shop.StatusCode, "Request failed", shop.Message),
            DbUpdateConcurrencyException => (409, "Conflict", "The resource changed concurrently. Retry your request."),
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => (409, "Conflict", "A resource with this unique value already exists."),
            PostgresException { SqlState: "23505" } => (409, "Conflict", "A resource with this unique value already exists."),
            PostgresException { SqlState: "40001" or "40P01" } => (409, "Conflict", "Concurrent update detected. Retry the operation."),
            DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" } } => (409, "Conflict", "Concurrent update detected. Retry the operation."),
            _ => (500, "Internal Server Error", "An unexpected error occurred.")
        };
        if (status == 500) logger.LogError(exception, "Unhandled API exception");
        else logger.LogWarning("Request rejected: {Type}: {Message}", exception.GetType().Name, exception.Message);
        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
