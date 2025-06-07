using SealedFga.Models;

namespace SealedFga.Generators;

public static class OpenFgaRelationInterfacesGenerator {
    public static GeneratedFile Generate()
        => new(
            "OpenFgaRelationInterfaces.g.cs",
            """
            /// <summary>
            ///     Used to strongly type enums for representation of OpenFGA relations.
            /// </summary>
            /// <param name="val">The enum's value, i.e. the OpenFGA relation string, e.g. <c>"can_view"</c>.</param>
            /// <typeparam name="TObjId">The related object entity ID's type.</typeparam>
            /// <typeparam name="TEnum">The type of the implementing enum class, e.g. 'UserAttributes'</typeparam>
            public abstract class OpenFgaRelation(string val) {
                /// <summary>
                ///     The raw relation name.
                /// </summary>
                public string Value { get; set; } = val;

                /// <inheritdoc />
                public override string ToString() => Value;

                /// <inheritdoc />
                public string AsOpenFgaString() => Value;
            }

            public interface IOpenFgaRelationWithImplementingType<TObjId, TEnum> : IOpenFgaRelation<TObjId>
                where TObjId : IOpenFgaTypeIdWithoutAssociatedIdType
                where TEnum : IOpenFgaRelationWithImplementingType<TObjId, TEnum> {
                public string Value { get; set; }

                /// <summary>
                ///     Converts an OpenFGA relation string to the corresponding relation enum object.
                /// </summary>
                /// <param name="openFgaString">The raw OpenFga relation string.</param>
                /// <returns>The corresponding enum object.</returns>
                public static abstract TEnum FromOpenFgaString(string openFgaString);
            }

            /// <summary>
            ///     Used to strongly type enums for representation of OpenFGA relations.
            /// </summary>
            /// <typeparam name="TObjId">The related object entity ID's type.</typeparam>
            public interface IOpenFgaRelation<TObjId>
                where TObjId : IOpenFgaTypeIdWithoutAssociatedIdType {
                /// <summary>
                ///     Returns the relation in the OpenFGA string representation.
                /// </summary>
                /// <returns>The OpenFGA relation string, e.g. <c>"can_view"</c></returns>
                public string AsOpenFgaString();
            }
            """
        );
}
