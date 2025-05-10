using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SealedFga.Util;

/// <summary>
///     A JSON converter that converts a string to a <b>T</b> object and vice versa.
/// </summary>
/// <param name="parseFunc">A function that builds an object of type <b>T</b> from a string.</param>
/// <typeparam name="T">The type to convert to/from.</typeparam>
public class JsonSimpleStringConverter<T>(Func<string, T> parseFunc) : JsonConverter<T> where T : notnull
{
    /// <summary>
    ///     Reads and converts the JSON to a <b>T</b> object.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A <b>T</b> object.</returns>
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options) =>
        parseFunc(reader.GetString()!);

    /// <summary>
    ///     Writes a <b>T</b> object as JSON.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}