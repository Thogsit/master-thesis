using SealedFga.Util;

namespace SealedFga.AuthModel;

/// <summary>
///     Interface for type IDs for OpenFGA object entities.
/// </summary>
public interface IOpenFgaTypeId<out TId> : IOpenFgaTypeIdWithoutAssociatedIdType where TId : IOpenFgaTypeId<TId> {
    /// <summary>
    ///     Returns the current object as an OpenFGA ID tuple string, e.g. "company:&lt;company_id&gt;".
    /// </summary>
    /// <returns>ID in OpenFGA tuple string representation.</returns>
    public string AsOpenFgaIdTupleString()
        => $"{IdUtil.GetNameByIdType(GetType())}:{ToString()}";
}
