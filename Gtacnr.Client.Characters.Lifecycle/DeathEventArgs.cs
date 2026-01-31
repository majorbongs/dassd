using System;

namespace Gtacnr.Client.Characters.Lifecycle;

public class DeathEventArgs : EventArgs
{
	public int VictimId { get; set; }

	public int KillerId { get; set; }

	public int Cause { get; set; }

	public bool IsRevenge { get; set; }

	public bool IsRedzone { get; set; }

	public int Bounty { get; set; }

	public bool IsSelfDefense { get; set; }

	public bool IsHeadshot { get; set; }

	public ulong HitmanContractReward { get; set; }

	public byte VictimWantedLevel { get; set; }

	public DeathEventArgs(int victimId, int killerId, int cause, bool isRevenge, bool isRedzone, int bounty, bool isSelfDefense, bool isHeadshot, ulong hitmanContractReward, byte victimWantedLevel)
	{
		VictimId = victimId;
		KillerId = killerId;
		Cause = cause;
		IsRevenge = isRevenge;
		IsRedzone = isRedzone;
		Bounty = bounty;
		IsSelfDefense = isSelfDefense;
		IsHeadshot = isHeadshot;
		HitmanContractReward = hitmanContractReward;
		VictimWantedLevel = victimWantedLevel;
	}
}
