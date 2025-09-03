namespace SealedFga.AuthModel;

/// <summary>
///     Interface for SealedFGA entities.
/// </summary>
public interface ISealedFgaType<TId> where TId : ISealedFgaTypeId<TId> {
    /// <summary>
    ///     The SealedFGA ID of this entity.
    /// </summary>
    public TId Id { get; set; }
}
