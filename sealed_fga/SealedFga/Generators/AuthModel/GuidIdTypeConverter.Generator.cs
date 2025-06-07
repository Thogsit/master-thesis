using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators;

public static class GuidIdTypeConverterGenerator {
    public static GeneratedFile Generate()
        => new(
            "GuidIdTypeConverter.g.cs",
            """
            public class GuidIdTypeConverter<TId>(Func<Guid, TId> constrFunc, Func<string, TId> parseFunc)
                : TypeConverter where TId : class, IOpenFgaTypeId<TId>
            {
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                    => sourceType == typeof(string)
                       || sourceType == typeof(Guid)
                       || base.CanConvertFrom(context, sourceType);

                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
                    => destinationType == typeof(string)
                       || destinationType == typeof(Guid)
                       || base.CanConvertTo(context, destinationType);

                public override object? ConvertFrom(
                    ITypeDescriptorContext context,
                    System.Globalization.CultureInfo culture,
                    object? value
                )
                {
                    return value switch
                    {
                        string strValue => parseFunc(strValue),
                        Guid guidValue => constrFunc(guidValue),
                        _ => base.ConvertFrom(context, culture, value)
                    };
                }

                public override object? ConvertTo(
                    ITypeDescriptorContext context,
                    System.Globalization.CultureInfo culture,
                    object? value,
                    Type destinationType
                )
                {
                    return value switch
                    {
                        TId tValue when destinationType == typeof(string) => tValue.ToString(),
                        TId tValue when destinationType == typeof(Guid) => (Guid)(object)tValue,
                        _ => base.ConvertTo(context, culture, value, destinationType)
                    };
                }
            }
            """,
            new HashSet<string>([
                    "System",
                    "System.ComponentModel",
                ]
            ),
            "SealedFga.Util"
        );
}
