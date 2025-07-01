using System;
using System.Collections.Generic;

namespace SealedFga.Util;

public static class IdUtil {
    private static readonly Dictionary<Type, string> IdTypeToNameMap = new();
    private static readonly Dictionary<Type, Func<string, object>> IdTypeToParseMethodMap = new();

    public static void RegisterIdType(Type idType, string typeName) => IdTypeToNameMap[idType] = typeName;

    public static string GetNameByIdType(Type idType) => IdTypeToNameMap[idType];

    public static void RegisterIdTypeParseMethod(Type idType, Func<string, object> parseMethod)
        => IdTypeToParseMethodMap[idType] = parseMethod;

    public static TObjId ParseId<TObjId>(string id) where TObjId : notnull {
        if (!IdTypeToParseMethodMap.TryGetValue(typeof(TObjId), out var parseMethod)) {
            throw new InvalidOperationException($"No parse method registered for ID type {typeof(TObjId).Name}");
        }

        return (TObjId) parseMethod(id);
    }

    public static object ParseId(Type idType, string id) {
        if (!IdTypeToParseMethodMap.TryGetValue(idType, out var parseMethod)) {
            throw new InvalidOperationException($"No parse method registered for ID type {idType.Name}");
        }

        return parseMethod(id);
    }
}
