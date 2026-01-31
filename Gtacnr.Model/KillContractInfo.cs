using System;
using Gtacnr.Client.API;

namespace Gtacnr.Model;

public class KillContractInfo : DispatchInfoBase
{
	public ulong Reward { get; set; }

	public DateTime ExpirationDate { get; set; }

	public int Placer { get; set; }

	public string PlacerString { get; set; } = "~c~Unknown~s~";

	public override string GetMenuItemDescription()
	{
		return "Reward: ~g~" + Reward.ToCurrencyString() + "~s~\nExpires: ~b~" + Utils.CalculateTimeIn(ExpirationDate) + "~s~\nPlaced by: " + ((LatentPlayers.Get(Placer) != null) ? LatentPlayers.Get(Placer).ColorNameAndId : PlacerString) + "~s~";
	}
}
