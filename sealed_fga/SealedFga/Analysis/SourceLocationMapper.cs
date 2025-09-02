using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SealedFga.Analysis;

public class SourceLocationMapper {
    private readonly List<LocationMapping> _mappings = [];

    public void AddMapping(int originalStart, int originalLength, int newLength) {
        var cumulativeOffset = GetCumulativeOffset(originalStart);

        var mapping = new LocationMapping {
            OriginalStart = originalStart,
            OriginalEnd = originalStart + originalLength,
            TransformedStart = originalStart + cumulativeOffset,
            TransformedEnd = originalStart + cumulativeOffset + newLength,
            OffsetDelta = newLength - originalLength,
        };

        _mappings.Add(mapping);
        _mappings.Sort((a, b) => a.OriginalStart.CompareTo(b.OriginalStart));
    }

    private int GetCumulativeOffset(int position) {
        var offset = 0;
        foreach (var mapping in _mappings) {
            if (mapping.OriginalStart < position) {
                offset += mapping.OffsetDelta;
            }
        }

        return offset;
    }

    public Location MapToOriginal(Location transformedLocation) {
        var position = transformedLocation.SourceSpan.Start;
        var originalPosition = position;

        foreach (var mapping in _mappings.OrderBy(m => m.TransformedStart)) {
            if (position >= mapping.TransformedStart) {
                if (position < mapping.TransformedEnd) {
                    originalPosition = mapping.OriginalStart + (position - mapping.TransformedStart);
                    break;
                }

                originalPosition -= mapping.OffsetDelta;
            }
        }

        return Location.Create(
            transformedLocation.SourceTree!,
            new TextSpan(originalPosition, transformedLocation.SourceSpan.Length)
        );
    }

    public struct LocationMapping {
        public int OriginalStart;
        public int OriginalEnd;
        public int TransformedStart;
        public int TransformedEnd;
        public int OffsetDelta;
    }
}
