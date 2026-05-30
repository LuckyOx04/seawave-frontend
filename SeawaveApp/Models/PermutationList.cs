using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawaveApp.Models;

public class PermutationList(List<UnifiedTrack> mainList, List<int> indexMap) : IList<UnifiedTrack>
{
    public IEnumerator<UnifiedTrack> GetEnumerator()
    {
        return indexMap.Select(index => mainList[index]).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(UnifiedTrack item) => throw new NotSupportedException();

    public void Clear() => throw new NotSupportedException();

    public bool Contains(UnifiedTrack item) => mainList.Contains(item);

    public void CopyTo(UnifiedTrack[] array, int arrayIndex)
    {
        for (var i = arrayIndex; i < indexMap.Count; i++)
        {
            array[i] = mainList[indexMap[i]];        
        }
    }

    public bool Remove(UnifiedTrack item) => throw new NotSupportedException();

    public int Count => indexMap.Count;
    
    public bool IsReadOnly => true;

    public int IndexOf(UnifiedTrack item) => indexMap.IndexOf(mainList.IndexOf(item));

    public void Insert(int index, UnifiedTrack item) => throw new NotSupportedException();

    public void RemoveAt(int index) => throw new NotSupportedException();

    public UnifiedTrack this[int index]
    {
        get => mainList[indexMap[index]];
        set => throw new NotSupportedException();
    }
}