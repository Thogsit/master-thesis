using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SealedFga.Analysis;

public class DepInjectionAwareDiagnosticsReporter(CompilationAnalysisContext context) {
    public class LocationMapData {
        public required SyntaxTree OldSyntaxTree { get; set; }
        public required SourceLocationMapper LocationMapper { get; set; }
    }

    public readonly Dictionary<string, LocationMapData> FilePathToLocationMapData = new();

    public void AddFile(string filePath, LocationMapData locMapData)
        => FilePathToLocationMapData[filePath] = locMapData;

    public void ReportDiagnostic(DiagnosticDescriptor rule, Location loc, params object[] args) {
        Location locationToReport = loc;

        // If we have modified the syntax tree, we need to readjust the diagnostic's target location
        if (loc.SourceTree != null &&
            FilePathToLocationMapData.TryGetValue(loc.SourceTree.FilePath, out var locMapData)) {
            var originalLocation = locMapData.LocationMapper.MapToOriginal(loc);

            // Create a new location that references the original syntax tree
            locationToReport = Location.Create(
                locMapData.OldSyntaxTree,
                originalLocation.SourceSpan
            );
        }

        // Create and report the diagnostic with the corrected location
        var diagnostic = Diagnostic.Create(rule, locationToReport, args);
        context.ReportDiagnostic(diagnostic);
    }
}
