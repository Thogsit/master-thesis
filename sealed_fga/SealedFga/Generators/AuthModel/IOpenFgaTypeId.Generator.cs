using SealedFga.Models;

namespace SealedFga.Generators.AuthModel;

public static class OpenFgaTypeIdInterfaceGenerator {
    public static GeneratedFile Generate()
        => new(
            "IOpenFgaTypeId.g.cs",
            """
            /// <summary>
            ///     Abstract base class for type IDs for OpenFGA object entities.
            /// </summary>
            public interface IOpenFgaTypeId<out TId> : IOpenFgaTypeIdWithoutAssociatedIdType where TId : IOpenFgaTypeId<TId> {
                /// <summary>
                ///     Returns the current object as an OpenFGA ID tuple string, e.g. "company:&lt;company_id&gt;".
                /// </summary>
                /// <returns>ID in OpenFGA tuple string representation.</returns>
                public string AsOpenFgaIdTupleString()
                    => $"{TId.OpenFgaTypeName}:{ToString()}";

                /// <summary>
                ///     Parses a raw ID string into the ID type.
                /// </summary>
                /// <param name="val">The raw ID string.</param>
                /// <returns>The ID in its strongly typed representation.</returns>
                public static abstract TId Parse(string val);
            }
            """
        );
}
