using System;

namespace Gtacnr.Model;

public class RaceParticipantInfo
{
	public bool Joined;

	public DateTime LastInvite = DateTime.MinValue;

	public uint CheckpointsPassed = 1u;

	public long BetAmount;

	public bool ReceivedWantedLevel;

	public DateTime LapStartTime = DateTime.MinValue;

	public TimeSpan? BestLapTime;
}
