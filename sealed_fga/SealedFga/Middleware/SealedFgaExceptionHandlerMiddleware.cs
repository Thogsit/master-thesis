using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SealedFga.Exceptions;

namespace SealedFga.Middleware;

/// <summary>
///     Middleware for handling SealedFGA-specific exceptions.
///     Maps custom exceptions to appropriate HTTP responses.
/// </summary>
public class SealedFgaExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<SealedFgaExceptionHandlerMiddleware> logger
) {
    /// <summary>
    ///     Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context) {
        try {
            await next(context);
        } catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    ///     Handles exceptions and returns appropriate HTTP responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception) {
        switch (exception) {
            case FgaAuthorizationException authEx:
                await HandleFgaAuthorizationException(context, authEx);
                break;

            case FgaEntityNotFoundException notFoundEx:
                await HandleFgaEntityNotFoundException(context, notFoundEx);
                break;

            default:
                throw exception; // Re-throw if we can't handle it
        }
    }

    /// <summary>
    ///     Handles <see cref="FgaAuthorizationException" /> and returns HTTP 403 Forbidden.
    /// </summary>
    private async Task HandleFgaAuthorizationException(
        HttpContext context,
        FgaAuthorizationException exception
    ) {
        context.Response.StatusCode = (int) HttpStatusCode.Forbidden;
        context.Response.ContentType = "text/plain";

        await context.Response.WriteAsync(exception.Message);
    }

    /// <summary>
    ///     Handles <see cref="FgaEntityNotFoundException" /> and returns HTTP 404 Not Found.
    /// </summary>
    private async Task HandleFgaEntityNotFoundException(
        HttpContext context,
        FgaEntityNotFoundException exception
    ) {
        context.Response.StatusCode = (int) HttpStatusCode.NotFound;
        context.Response.ContentType = "text/plain";

        await context.Response.WriteAsync(exception.Message);
    }
}
