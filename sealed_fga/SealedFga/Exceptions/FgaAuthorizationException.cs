using System;

namespace SealedFga.Exceptions;

/// <summary>
///     Exception thrown when OpenFGA authorization check fails.
///     Results in HTTP 403 Forbidden response.
/// </summary>
public class FgaAuthorizationException(string objectId) : Exception($"Access denied to object '{objectId}'");
