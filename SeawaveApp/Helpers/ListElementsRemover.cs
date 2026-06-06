using System;
using System.Collections.Generic;

namespace SeawaveApp.Helpers;

public static class ListElementsRemover
{
    public static void KeepOnlyIndex<T>(this List<T> list, int indexToKeep)
    {
        if (indexToKeep < 0 || indexToKeep >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(indexToKeep));
        }
        
        var itemsAfter = list.Count - 1 - indexToKeep;
        if (itemsAfter > 0)
        {
            list.RemoveRange(indexToKeep + 1, itemsAfter);
        }

        if (indexToKeep > 0)
        {
            list.RemoveRange(0,indexToKeep);
        }
    }
}