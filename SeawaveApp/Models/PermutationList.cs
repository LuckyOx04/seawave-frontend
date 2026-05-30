using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SeawaveApp.Models;

public class PermutationList(List<UnifiedTrack> masterList, List<int> indexMap)
    : ReadOnlyCollection<UnifiedTrack>(indexMap.Select(index => masterList[index]).ToList());