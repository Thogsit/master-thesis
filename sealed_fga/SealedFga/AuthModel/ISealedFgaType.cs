namespace SealedFga.AuthModel;

/// <summary>
///     Interface for SealedFGA entities.
/// </summary>
public interface ISealedFgaType<TId> where TId : ISealedFgaTypeId<TId> {
    /// <summary>
    ///     The SealedFGA ID of this entity.
    /// </summary>
    public TId Id { get; set; }

    /// <summary>
    ///     Returns the SealedFGA ID as a string in the format "type:id".
    /// </summary>
    public string AsOpenFgaIdTupleString() => Id.AsOpenFgaIdTupleString();
}
