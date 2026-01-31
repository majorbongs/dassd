using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Model;

namespace Gtacnr.Client.Crimes;

public class AidingScript : Script
{
	private List<int> playersAided = new List<int>();

	private Dictionary<int, DateTime> felonEntryTimes = new Dictionary<int, DateTime>();

	private static readonly TimeSpan harboringThreshold = TimeSpan.FromSeconds(5.0);

	[Update]
	private async Coroutine UpdateTick()
	{
		await Script.Wait(1000);
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() || (Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			return;
		}
		List<int> playerPassengersInVehicle = Utils.GetPlayerPassengersInVehicle(currentVehicle);
		foreach (int item in playerPassengersInVehicle)
		{
			if (playersAided.Contains(item))
			{
				continue;
			}
			PlayerState playerState = LatentPlayers.Get(item);
			if (playerState != null && playerState.WantedLevel > 1)
			{
				if (!felonEntryTimes.ContainsKey(item))
				{
					felonEntryTimes[item] = DateTime.UtcNow;
				}
				else if (Gtacnr.Utils.CheckTimePassed(felonEntryTimes[item], harboringThreshold))
				{
					playersAided.Add(item);
					BaseScript.TriggerServerEvent("gtacnr:crimes:aidPlayer", new object[1] { item });
				}
			}
		}
		foreach (int item2 in new List<int>(felonEntryTimes.Keys))
		{
			if (!playerPassengersInVehicle.Contains(item2))
			{
				felonEntryTimes.Remove(item2);
			}
		}
	}

	[EventHandler("gtacnr:died")]
	private void OnDead(int killerId, int cause)
	{
		playersAided.Clear();
		felonEntryTimes.Clear();
	}
}
