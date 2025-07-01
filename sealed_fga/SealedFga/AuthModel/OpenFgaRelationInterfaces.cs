namespace SealedFga.AuthModel;

/// <summary>
///     Used to strongly type enums for representation of OpenFGA relations.
/// </summary>
/// <param name="val">The enum's value, i.e. the OpenFGA relation string, e.g. <c>"can_view"</c>.</param>
public abstract class OpenFgaRelation(string val) {
    /// <summary>
    ///     The raw relation name.
    /// </summary>
    public string Value { get; set; } = val;

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    ///     Returns the relation in the OpenFGA string representation.
    /// </summary>
    /// <returns>The OpenFGA relation string, e.g. <c>"can_view"</c></returns>
    public string AsOpenFgaString() => Value;
}

/// <summary>
///     Used to strongly type enums for representation of OpenFGA relations.
/// </summary>
/// <typeparam name="TObjId">The related object entity ID's type.</typeparam>
public interface IOpenFgaRelation<TObjId>
    where TObjId : IOpenFgaTypeIdWithoutAssociatedIdType {
    /// <summary>
    ///     The raw relation name.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    ///     Returns the relation in the OpenFGA string representation.
    /// </summary>
    /// <returns>The OpenFGA relation string, e.g. <c>"can_view"</c></returns>
    public string AsOpenFgaString();
}
