using System.Collections.Generic;
using System.Linq;
using OpenFga.Language.Model;
using SealedFga.Models;
using SealedFga.Util;

namespace SealedFga.Generators;

public static class _TypeName_RelationsGenerator {
    private static GeneratedFile BuildRelationFile(
        string classNamespace,
        string className,
        string idClassName,
        List<string> relNames
    ) => new(
        $"{className}.g.cs",
        $$"""
          public class {{className}}(string val)
          : OpenFgaRelation(val), IOpenFgaRelationWithImplementingType<{{idClassName}}, {{className}}>
          {
              {{GetEnumFields(className, relNames)}}

              public static {{className}} FromOpenFgaString(string openFgaString) => new {{className}}(openFgaString);
          }
          """,
        new HashSet<string>([
                Settings.PackageNamespace,
            ]
        ),
        classNamespace
    );

    public static List<GeneratedFile> Generate(AuthorizationModel authModel, IdClassToGenerateData idClassToGenerate) {
        var relationFiles = new List<GeneratedFile>();
        var relationNames = authModel.TypeDefinitions.FirstOrDefault(td => td.Type == idClassToGenerate.TypeName);
        if (relationNames is null) return relationFiles;

        var relAttributesClassName = idClassToGenerate.ClassName + "Attributes";
        var relGroupsClassName = idClassToGenerate.ClassName + "Groups";
        var attrRelations = relationNames.Relations!.Keys.Where(rel => char.IsLower(rel[0])).ToList();
        var groupRelations = relationNames.Relations!.Keys.Where(rel => char.IsUpper(rel[0])).ToList();
        if (attrRelations.Count > 0) {
            relationFiles.Add(
                BuildRelationFile(
                    idClassToGenerate.ClassNamespace,
                    relAttributesClassName,
                    idClassToGenerate.ClassName,
                    attrRelations
                )
            );
        }

        if (groupRelations.Count > 0) {
            relationFiles.Add(
                BuildRelationFile(
                    idClassToGenerate.ClassNamespace,
                    relGroupsClassName,
                    idClassToGenerate.ClassName,
                    groupRelations
                )
            );
        }

        return relationFiles;
    }

    private static string GetEnumFields(string className, List<string> relNames)
        => GeneratorUtil.BuildLinesWithIndent(
            relNames.Select(rel => $"public static readonly {className} {rel} = new {className}(\"{rel}\");"),
            4
        );
}
