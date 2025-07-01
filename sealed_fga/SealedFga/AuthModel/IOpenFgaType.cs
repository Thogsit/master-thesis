namespace SealedFga.AuthModel;

/// <summary>
///     Interface for OpenFGA entities.
/// </summary>
public interface IOpenFgaType<TId> where TId : IOpenFgaTypeId<TId> {
    /// <summary>
    ///     The OpenFGA ID of this entity.
    /// </summary>
    public TId Id { get; set; }

    /// <summary>
    ///     Returns the OpenFGA ID as a string in the format "type:id".
    /// </summary>
    public string AsOpenFgaIdTupleString() => Id.AsOpenFgaIdTupleString();
}
