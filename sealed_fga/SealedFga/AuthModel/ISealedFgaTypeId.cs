using SealedFga.Util;

namespace SealedFga.AuthModel;

/// <summary>
///     Interface for type IDs for SealedFGA object entities.
/// </summary>
public interface ISealedFgaTypeId<out TId> : ISealedFgaTypeIdWithoutAssociatedIdType where TId : ISealedFgaTypeId<TId> {
    /// <summary>
    ///     Returns the current object as an SealedFGA ID tuple string, e.g. "company:&lt;company_id&gt;".
    /// </summary>
    /// <returns>ID in OpenFGA tuple string representation.</returns>
    public string AsOpenFgaIdTupleString();
}
