//
// OpenFGA/.NET SDK for OpenFGA
//
// API version: 1.x
// Website: https://openfga.dev
// Documentation: https://openfga.dev/docs
// Support: https://openfga.dev/community
// License: [Apache-2.0](https://github.com/openfga/dotnet-sdk/blob/main/LICENSE)
//
// NOTE: This file was auto generated. DO NOT EDIT.
//


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// This class is based on suggestions from https://github.com/dotnet/runtime/issues/31081

/// <summary>
/// Helper class to serialize / deserialize enum member to/from JSON
/// </summary>
public class JsonStringEnumMemberConverter<EnumTemplate> : JsonConverter<EnumTemplate> where EnumTemplate : struct, Enum {

    private readonly Dictionary<EnumTemplate, string> _enumToString = new();
    private readonly Dictionary<string, EnumTemplate> _stringToEnum = new();

    /// <summary>
    /// Parsing and converting enum member	
	/// </summary>
    public JsonStringEnumMemberConverter() {
        var type = typeof(EnumTemplate);
        var values = (EnumTemplate[])Enum.GetValues(type);

        foreach (var value in values) {
            var enumMember = type.GetMember(value.ToString())[0];
            var attr = enumMember.GetCustomAttributes(typeof(EnumMemberAttribute), false)
              .Cast<EnumMemberAttribute>()
              .FirstOrDefault();

            _stringToEnum.Add(value.ToString(), value);

            if (attr?.Value != null && value.ToString() != attr?.Value) {
                _stringToEnum.Add(attr.Value, value);
                _enumToString.Add(value, attr.Value);
            }
            else {
                _enumToString.Add(value, value.ToString());
            }
        }
    }

    /// <summary>
    /// Overwrite write function to provide enum value to string
	/// </summary>
    public override void Write(Utf8JsonWriter writer, EnumTemplate value, JsonSerializerOptions options) {
        writer.WriteStringValue(_enumToString[value]);
    }

    /// <summary>
    /// Overwrite read function to convert string back to enum value
	/// </summary>
    public override EnumTemplate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var stringValue = reader.GetString();

        return ((_stringToEnum.TryGetValue(stringValue, out var enumValue)) ? enumValue : default);
    }

}