namespace OpenFga.Language.Errors;

public sealed class StartEnd(int start, int end)
{
    public readonly int Start = start;
    public readonly int End = end;

    public StartEnd WithOffset(int offset)
    {
        return new StartEnd(Start + offset, End + offset);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not StartEnd other) return false;
        return Start == other.Start && End == other.End;
    }

    public override int GetHashCode()
    {
        return (Start, End).GetHashCode();
    }
}