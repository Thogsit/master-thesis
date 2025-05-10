using System;
using System.Runtime.Serialization;

namespace OpenFga.Language.Util;

public class EnumUtil
{
    public static T FromString<T>(string value) where T : Enum
    {
        var type = typeof(T);
        foreach (var field in type.GetFields())
        {
            if (Attribute.GetCustomAttribute(
                    field,
                    typeof(EnumMemberAttribute)
                ) is EnumMemberAttribute attribute
                && attribute.Value == value)
            {
                return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException($"Unknown value: {value}");
    }
}