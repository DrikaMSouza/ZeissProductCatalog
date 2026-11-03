using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Exceptions;

namespace ProductCatalog.Api.ErrorHandling;

/// <summary>
/// Translates exceptions into RFC 9457 problem details, so controllers stay free of try/catch blocks.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            DomainValidationException domain => CreateProblem(StatusCodes.Status400BadRequest, "Invalid request", domain.Message),
            NotFoundException notFound => CreateProblem(StatusCodes.Status404NotFound, "Resource not found", notFound.Message),
            InsufficientStockException stock => CreateInsufficientStockProblem(stock),
            ConflictException conflict => CreateProblem(StatusCodes.Status409Conflict, "Conflict", conflict.Message),
            ProductIdExhaustedException exhausted => CreateProblem(StatusCodes.Status503ServiceUnavailable, "Product ids exhausted", exhausted.Message),
            _ => CreateProblem(StatusCodes.Status500InternalServerError, "An unexpected error occurred", null),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request {Method} {Path} rejected: {Reason}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails CreateProblem(int status, string title, string? detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };
    }

    private static ProblemDetails CreateInsufficientStockProblem(InsufficientStockException exception)
    {
        var problem = CreateProblem(StatusCodes.Status409Conflict, "Insufficient stock", exception.Message);
        problem.Extensions["availableStock"] = exception.AvailableStock;
        problem.Extensions["requestedQuantity"] = exception.RequestedQuantity;
        return problem;
    }

    private static string ToCamelCase(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
