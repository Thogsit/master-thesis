namespace SealedFga.Sample.Fga.Payloads;

public abstract record BaseFgaQueuePayload {
    public required string RawUser { get; init; }
    public required string RawRelation { get; init; }
    public required string RawObject { get; init; }
}
