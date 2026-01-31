using System;

namespace Gtacnr.Model;

public class DailyRaceTrack : RaceTrack
{
	public long RewardAmount { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Id { get; set; } = string.Empty;

	public string AuthorId { get; set; } = string.Empty;

	public string AuthorUsername { get; set; } = string.Empty;

	public TimeSpan BestLapTime { get; set; }

	public string? BestLapTimeUserId { get; set; }

	public string? BestLapTimeUsername { get; set; }

	public int PlayersCount { get; set; }

	public int PlayersNeeded { get; set; } = 5;
}
