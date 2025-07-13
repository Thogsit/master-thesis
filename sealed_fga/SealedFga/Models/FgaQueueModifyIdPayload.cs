namespace SealedFga.Models;

public record FgaQueueModifyIdPayload(
    string RawOldId,
    string RawNewId,
    string TypeName
);
