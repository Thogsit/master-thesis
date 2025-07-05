namespace SealedFga.AuthModel;

/// <summary>
///     Abstract base class for type IDs for SealedFGA object entities.
///     Has no associated ID type for easier usage in generic type parameters.
/// </summary>
public interface ISealedFgaTypeIdWithoutAssociatedIdType {
    /// <summary>
    ///     Returns the ID without its type in its OpenFGA string representation.
    /// </summary>
    /// <returns>The ID as a string.</returns>
    public string ToString();
}
