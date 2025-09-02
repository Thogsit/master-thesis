using System;
using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.AuthModel;

public static class TypeNameIdGenerator {
    public const string ParseMethodName = "Parse";
    public const string OpenFgaIdTupleStringMethodName = "AsOpenFgaIdTupleString";
    public const string OpenFgaTypeNamePropertyName = "OpenFgaTypeName";

    public static GeneratedFile Generate(IdClassToGenerateData idClassToGenerate)
        => new(
            $"{idClassToGenerate.ClassName}.partial.g.cs",
            $$"""

              [TypeConverter(typeof(IdTypeConverter))]
              [JsonConverter(typeof(IdJsonConverter))]
              public partial class {{idClassToGenerate.ClassName}} : ISealedFgaTypeId<{{idClassToGenerate.ClassName}}>, IEquatable<{{idClassToGenerate.ClassName}}>
              {
                  /// <summary>
                  ///     The ID's value.
                  /// </summary>
                  public {{idClassToGenerate.UnderlyingType}} Value { get; set; }

                  /// <summary>
                  ///     Creates a new instance of {{idClassToGenerate.ClassName}} from the ID's "raw" type.
                  /// </summary>
                  /// <param name="val">The raw ID's value.</param>
                  public {{idClassToGenerate.ClassName}}({{idClassToGenerate.UnderlyingType}} val)
                  {
                      Value = val;
                  }

                  /// <inheritdoc />
                  public static string {{OpenFgaTypeNamePropertyName}} => "{{idClassToGenerate.TypeName}}";

                  /// <summary>
                  ///     Creates a new instance of <see cref="{{idClassToGenerate.ClassName}}"/> with a generated value.
                  /// </summary>
                  public static {{idClassToGenerate.ClassName}} New()
                  {
                      return new {{idClassToGenerate.ClassName}}({{GetNewFunction(idClassToGenerate)}});
                  }

                  /// <inheritdoc />
                  public static {{idClassToGenerate.ClassName}} {{ParseMethodName}}(string val)
                  {
                      return new {{idClassToGenerate.ClassName}}({{GetParserFunction(idClassToGenerate, "val")}});
                  }

                  /// <inheritdoc />
                  public string {{OpenFgaIdTupleStringMethodName}}()
                      => $"{OpenFgaTypeName}:{ToString()}";

                  /// <inheritdoc />
                  public override string ToString()
                  {
                      return Value.ToString();
                  }

                  /// <inheritdoc />
                  public override bool Equals(object? obj)
                  {
                      if (ReferenceEquals(null, obj)) return false;
                      if (ReferenceEquals(this, obj)) return true;
                      if (obj.GetType() != this.GetType()) return false;
                      return Equals(({{idClassToGenerate.ClassName}}) obj);
                  }

                  /// <inheritdoc />
                  public bool Equals({{idClassToGenerate.ClassName}}? other)
                  {
                      if (ReferenceEquals(null, other)) return false;
                      if (ReferenceEquals(this, other)) return true;
                      return Value.Equals(other.Value);
                  }

                  /// <inheritdoc />
                  public override int GetHashCode()
                  {
                      return Value.GetHashCode();
                  }

                  public static bool operator ==({{idClassToGenerate.ClassName}}? left, {{idClassToGenerate.ClassName}}? right)
                  {
                      if (ReferenceEquals(left, null))
                      {
                          return ReferenceEquals(right, null);
                      }

                      return left.Equals(right);
                  }

                  public static bool operator !=({{idClassToGenerate.ClassName}}? left, {{idClassToGenerate.ClassName}}? right)
                  {
                      return !(left == right);
                  }


                  /// <summary>
                  ///     Converts a <see cref="{{idClassToGenerate.ClassName}}" /> to a <see cref="{{idClassToGenerate.UnderlyingType}}" /> and vice versa for DB storage.
                  /// </summary>
                  public class EfCoreValueConverter() : ValueConverter<{{idClassToGenerate.ClassName}}, {{idClassToGenerate.UnderlyingType}}>(
                      id => id.Value,
                      val => new {{idClassToGenerate.ClassName}}(val));

                  /// <summary>
                  ///    Converts a <see cref="{{idClassToGenerate.ClassName}}" /> to its JSON representation and vice versa.
                  /// </summary>
                  public class IdJsonConverter() : {{GetJsonConverter(idClassToGenerate)}};

                  /// <summary>
                  ///    Converts a <see cref="{{idClassToGenerate.ClassName}}" /> to another compatible type and vice versa.
                  /// </summary>
                  public class IdTypeConverter() : {{GetTypeConverter(idClassToGenerate)}};
              }
              """,
            new HashSet<string>([
                    ..GetTypeDependentUsings(idClassToGenerate),
                    "Microsoft.EntityFrameworkCore.Storage.ValueConversion",
                    Settings.AuthModelNamespace,
                    Settings.UtilNamespace,
                    "System.ComponentModel",
                    "System.Text.Json.Serialization",
                ]
            ),
            idClassToGenerate.ClassNamespace
        );

    private static string GetTypeConverter(IdClassToGenerateData idClassToGenerate)
        => idClassToGenerate.Type switch {
            SealedFgaTypeIdType.Guid
                => $"GuidIdTypeConverter<{idClassToGenerate.ClassName}>(g => new {idClassToGenerate.ClassName}(g), s => {idClassToGenerate.ClassName}.Parse(s))",
            SealedFgaTypeIdType.String
                => $"StringIdTypeConverter<{idClassToGenerate.ClassName}>(s => {idClassToGenerate.ClassName}.Parse(s))",
            _ => throw new ArgumentOutOfRangeException(),
        };

    private static string GetJsonConverter(IdClassToGenerateData idClassToGenerate) {
        var strTypeConverter =
            $"JsonSimpleStringConverter<{idClassToGenerate.ClassName}>(s => {idClassToGenerate.ClassName}.Parse(s))";
#pragma warning disable CS8524
        return idClassToGenerate.Type switch
#pragma warning restore CS8524
        {
            SealedFgaTypeIdType.Guid => strTypeConverter,
            SealedFgaTypeIdType.String => strTypeConverter,
        };
    }

    private static HashSet<string> GetTypeDependentUsings(IdClassToGenerateData idClassToGenerate) {
        var usings = new HashSet<string>();
        if (idClassToGenerate.Type == SealedFgaTypeIdType.Guid) usings.Add("System");

        return usings;
    }

    private static string GetParserFunction(IdClassToGenerateData idClassToGenerate, string attrName) {
#pragma warning disable CS8524
        return idClassToGenerate.Type switch
#pragma warning restore CS8524
        {
            SealedFgaTypeIdType.Guid => $"Guid.Parse({attrName})",
            SealedFgaTypeIdType.String => attrName,
        };
    }

    private static string GetNewFunction(IdClassToGenerateData idClassToGenerate) {
#pragma warning disable CS8524
        return idClassToGenerate.Type switch
#pragma warning restore CS8524
        {
            SealedFgaTypeIdType.Guid => "Guid.NewGuid()",
            SealedFgaTypeIdType.String => "\"\"",
        };
    }
}
