using System;
using CitizenFX.Core;

namespace Gtacnr.Client.Characters.Lifecycle;

public class DeathEventScript : Script
{
	public static event EventHandler<DeathEventArgs> PlayerDeath;

	[EventHandler("gtacnr:playerDeath")]
	private void OnPlayerDeath(int victimId, int killerId, int cause, bool isRevenge, bool isRedzone, int bounty, bool isSelfDefense, bool isHeadshot, ulong hitmanContractReward, byte victimWantedLevel)
	{
		DeathEventArgs e = new DeathEventArgs(victimId, killerId, cause, isRevenge, isRedzone, bounty, isSelfDefense, isHeadshot, hitmanContractReward, victimWantedLevel);
		DeathEventScript.PlayerDeath?.Invoke(this, e);
	}
}
