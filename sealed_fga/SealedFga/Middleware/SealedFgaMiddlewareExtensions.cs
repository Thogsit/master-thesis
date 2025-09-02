using Microsoft.AspNetCore.Builder;

namespace SealedFga.Middleware;

/// <summary>
///     Extension methods for registering SealedFGA middleware.
/// </summary>
public static class SealedFgaMiddlewareExtensions {
    /// <summary>
    ///     Adds the SealedFGA exception handler middleware to the application pipeline.
    ///     This middleware will catch SealedFGA-specific exceptions and return appropriate HTTP responses.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    ///     <code>
    ///     var app = builder.Build();
    ///     app.UseSealedFgaExceptionHandler();
    ///     </code>
    /// </example>
    public static IApplicationBuilder UseSealedFgaExceptionHandler(this IApplicationBuilder builder) =>
        builder.UseMiddleware<SealedFgaExceptionHandlerMiddleware>();
}
