using System.Runtime.CompilerServices;
using SealedFga.AuthModel;

namespace SealedFga;

public static class SealedFga {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireCheck<TId, TRel>(ISealedFgaType<TId> entity, params TRel[] relations)
        where TId : ISealedFgaTypeId<TId>
        where TRel : ISealedFgaRelation<TId> {}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireCheck<TId, TRel>(TId entityId, params TRel[] relations)
        where TId : ISealedFgaTypeId<TId>
        where TRel : ISealedFgaRelation<TId> {}
}
