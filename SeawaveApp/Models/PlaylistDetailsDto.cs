using System.Collections.Generic;

namespace SeawaveApp.Models;

public record PlaylistDetailsDto(int Id, string Name, int CreatorId, string CreatorName, List<TrackDto> Tracks);