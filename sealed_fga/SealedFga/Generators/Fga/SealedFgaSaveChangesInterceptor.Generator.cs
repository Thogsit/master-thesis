using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.Fga;

public static class SealedFgaSaveChangesInterceptorGenerator {
    public static GeneratedFile Generate()
        => new(
            "SealedFgaSaveChangesInterceptor.g.cs",
            """
            public class SealedFgaSaveChangesInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
            {
                private static readonly ThreadLocal<bool> IsProcessing = new();

                /// <summary>
                ///     Wrapper around the <see cref="ProcessSealedFgaChanges(DbContext?)" /> method
                ///     that ensures that it is not called recursively.
                ///     This can e.g. happen due to the TickerQ usage for SealedFGA change tracking.
                /// </summary>
                /// <param name="context"></param>
                private void RecursionSafeProcessSealedFgaChanges(DbContext? context) {
                    if (IsProcessing.Value) {
                        return;
                    }

                    try {
                        IsProcessing.Value = true;
                        var processor = new SealedFgaSaveChangesProcessor(serviceProvider);
                        processor.ProcessSealedFgaChanges(context);
                    } finally {
                        IsProcessing.Value = false;
                    }
                }

                /// <inheritdoc />
                public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                    DbContextEventData eventData,
                    InterceptionResult<int> result,
                    CancellationToken cancellationToken = new()
                ) {
                    RecursionSafeProcessSealedFgaChanges(eventData.Context);
                    return base.SavingChangesAsync(eventData, result, cancellationToken);
                }

                /// <inheritdoc />
                public override InterceptionResult<int> SavingChanges(
                    DbContextEventData eventData,
                    InterceptionResult<int> result
                ) {
                    RecursionSafeProcessSealedFgaChanges(eventData.Context);
                    return base.SavingChanges(eventData, result);
                }
            }
            """,
            new HashSet<string>([
                    "System",
                    "System.Threading",
                    "System.Threading.Tasks",
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.EntityFrameworkCore.Diagnostics",
                ]
            ),
            Settings.FgaNamespace
        );
}
