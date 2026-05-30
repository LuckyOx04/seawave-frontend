using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawaveApp.Models;

public class PermutationList(List<UnifiedTrack> masterList, List<int> indexMap) : IReadOnlyList<UnifiedTrack>
{
    public IEnumerator<UnifiedTrack> GetEnumerator()
    {
        return indexMap.Select(index => masterList[index]).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => indexMap.Count;

    public UnifiedTrack this[int index] => masterList[indexMap[index]];
}