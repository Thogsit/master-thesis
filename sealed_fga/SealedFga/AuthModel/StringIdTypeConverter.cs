using System;
using System.ComponentModel;
using System.Globalization;

namespace SealedFga.AuthModel;

/// <summary>
///     Converts between <see cref="string" /> and a strongly-typed ID for use with SealedFGA entities.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID type.</typeparam>
/// <param name="parseFunc">A function to parse the ID from a <see cref="string" />.</param>
public class StringIdTypeConverter<TId>(Func<string, TId> parseFunc)
    : TypeConverter where TId : class, ISealedFgaTypeId<TId> {
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc />
    public override object? ConvertFrom(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object? value
    ) {
        return value switch {
            string strValue => parseFunc(strValue),
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
            _ => base.ConvertTo(context, culture, value, destinationType),
        };
    }
}
