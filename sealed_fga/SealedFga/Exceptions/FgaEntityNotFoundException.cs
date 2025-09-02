using System;

namespace SealedFga.Exceptions;

/// <summary>
///     Exception thrown when an entity cannot be found in the database after authorization succeeds.
///     Results in HTTP 404 Not Found response.
/// </summary>
public class FgaEntityNotFoundException(Type entityType, object entityId)
    : Exception($"Entity of type '{entityType.Name}' with ID '{entityId}' was not found");
