using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OpenFga.Language;
using OpenFga.Language.Model;
using SealedFga.Attributes;
using SealedFga.Generators;
using SealedFga.Generators.AuthModel;
using SealedFga.Generators.Fga;
using SealedFga.Models;

namespace SealedFga;

[Generator]
public class SealedFgaSourceGenerator : IIncrementalGenerator {
    private static readonly DiagnosticDescriptor InvalidModelFgaFileRule = new(
        "SFGA001",
        "Invalid model.fga file",
        "The model.fga file could not be parsed correctly",
        "Security",
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor UnknownOpenFgaTypeNameRule = new(
        "SFGA002",
        "Unknown OpenFGA type name",
        "The OpenFgaTypeId attribute references an unknown OpenFGA type name: {0}. Please make sure the type name exists in the 'model.fga' file.",
        "Security",
        DiagnosticSeverity.Error,
        true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Filters for changes to the model.fga file
        var modelFgaProvider = context.AdditionalTextsProvider
                                      .Where(f => Path.GetFileName(f.Path) == "model.fga")
                                      .Select((f, _) => {
                                               var fileContent = f.GetText()?.ToString();
                                               AuthorizationModel? authModel = null;
                                               if (fileContent is not null) {
                                                   var authModelResult =
                                                       OpenFgaFromDslTransformer.ParseDsl(fileContent);
                                                   if (authModelResult.IsFailure) {
                                                       // TODO: Error handling?
                                                   } else {
                                                       authModel = authModelResult.AuthorizationModel;
                                                   }
                                               }

                                               return new ModelFgaIncrementalChange {
                                                   DiagnosticLocation =
                                                       Location.Create(f.Path, new TextSpan(), new LinePositionSpan()),
                                                   AuthorizationModel = authModel,
                                               };
                                           }
                                       );

        // Filters for classes with the OpenFgaTypeIdAttribute
        var fgaTypeIdProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(SealedFgaTypeIdAttribute).FullName!,
            static (synNode, _) => synNode is ClassDeclarationSyntax,
            static (context, _) => {
                var classDeclaration = (ClassDeclarationSyntax) context.TargetNode;
                var attribute = context.Attributes[0];
                return IdClassToGenerateData.From(
                    attribute,
                    classDeclaration,
                    context.TargetSymbol
                );
            }
        ).Collect();

        // Combine both so that we are triggered when either the model.fga file changes or a class with the OpenFgaTypeIdAttribute is added
        var fgaRelatedChangesProvider = modelFgaProvider.Combine(fgaTypeIdProvider);

        // Register incremental model.fga based code gen
        context.RegisterSourceOutput(fgaRelatedChangesProvider, GenerateCodeOnFgaRelatedChange);

        // Register non-incremental code gen
        context.RegisterPostInitializationOutput(GenerateNonIncrementalSourceFiles);
    }

    private static void GenerateNonIncrementalSourceFiles(IncrementalGeneratorPostInitializationContext context) {
        var generatedFiles = new List<GeneratedFile>([
                SealedFgaExtensionsGenerator.Generate(),
                SealedFgaSaveChangesInterceptorGenerator.Generate(),
            ]
        );

        foreach (var genFile in generatedFiles) {
            context.AddSource(
                genFile.FileName,
                genFile.BuildFullFileContent()
            );
        }
    }

    private static void GenerateCodeOnFgaRelatedChange(
        SourceProductionContext context,
        (ModelFgaIncrementalChange modelFileChange, ImmutableArray<IdClassToGenerateData> idClasses)
            fgaRelatedChanges
    ) {
        if (fgaRelatedChanges.modelFileChange.AuthorizationModel is null) {
            context.ReportDiagnostic(Diagnostic.Create(
                    InvalidModelFgaFileRule,
                    fgaRelatedChanges.modelFileChange.DiagnosticLocation
                )
            );
            return;
        }

        foreach (var idClassToGenerate in fgaRelatedChanges.idClasses) {
            AddGeneratedFilesForFgaType(context,
                fgaRelatedChanges.modelFileChange.AuthorizationModel,
                idClassToGenerate
            );
        }

        var generatedSealedFgaFile = SealedFgaInitGenerator.Generate(fgaRelatedChanges.idClasses);
        context.AddSource(generatedSealedFgaFile.FileName, generatedSealedFgaFile.BuildFullFileContent());
    }

    private static void AddGeneratedFilesForFgaType(
        SourceProductionContext context,
        AuthorizationModel authModel,
        IdClassToGenerateData idClassToGenerate
    ) {
        // Check if the OpenFGA type name is valid
        if (authModel.TypeDefinitions.All(td => td.Type != idClassToGenerate.TypeName)) {
            context.ReportDiagnostic(Diagnostic.Create(
                    UnknownOpenFgaTypeNameRule,
                    idClassToGenerate.Location,
                    idClassToGenerate.TypeName
                )
            );
            return;
        }

        // Generate the partial class for the OpenFGA ID type
        var generatedIdFile = TypeNameIdGenerator.Generate(idClassToGenerate);
        context.AddSource(generatedIdFile.FileName, generatedIdFile.BuildFullFileContent());

        // Generate the relation types for the OpenFGA ID type
        var generatedRelationFiles = TypeNameRelationsGenerator.Generate(authModel, idClassToGenerate);
        foreach (var generatedRelationsFile in generatedRelationFiles) {
            context.AddSource(generatedRelationsFile.FileName, generatedRelationsFile.BuildFullFileContent());
        }
    }
}
