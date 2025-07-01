using System;
using System.ComponentModel;
using System.Globalization;

namespace SealedFga.AuthModel;

/// <summary>
///     Converts between <see cref="Guid" /> and a strongly-typed ID for use with OpenFGA entities.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID type.</typeparam>
/// <param name="constrFunc">A function to construct the ID from a <see cref="Guid" />.</param>
/// <param name="parseFunc">A function to parse the ID from a <see cref="string" />.</param>
public class GuidIdTypeConverter<TId>(Func<Guid, TId> constrFunc, Func<string, TId> parseFunc)
    : TypeConverter where TId : class, IOpenFgaTypeId<TId> {
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string)
           || sourceType == typeof(Guid)
           || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type destinationType)
        => destinationType == typeof(string)
           || destinationType == typeof(Guid)
           || base.CanConvertTo(context, destinationType);

    /// <inheritdoc />
    public override object? ConvertFrom(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object? value
    ) {
        return value switch {
            string strValue => parseFunc(strValue),
            Guid guidValue => constrFunc(guidValue),
            _ => base.ConvertFrom(context, culture, value),
        };
    }

    /// <inheritdoc />
    public override object ConvertTo(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object value,
        Type destinationType
    ) {
        return value switch {
            TId tValue when destinationType == typeof(string) => tValue.ToString(),
            TId tValue when destinationType == typeof(Guid) => (Guid) (object) tValue,
            _ => base.ConvertTo(context, culture, value, destinationType),
        };
    }
}
