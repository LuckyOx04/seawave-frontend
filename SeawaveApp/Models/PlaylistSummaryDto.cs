namespace SeawaveApp.Models;

public record PlaylistSummaryDto(int Id, string Name, int CreatorId, string CreatorName, int TrackCount);