namespace SealedFga.Database;

public record OpenFgaQueueEntry(
    long Id,
    string OperationType,
    string User,
    string Relation,
    string Object,
    int AttemptCount,
    string? LastError
);
