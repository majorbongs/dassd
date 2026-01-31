using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Characters.Lifecycle;

namespace Gtacnr.Client.Anticheat;

public class PedWhitelistScript : Script
{
	private List<int> pedModelsWhitelist = new List<int>
	{
		API.GetHashKey("mp_m_freemode_01"),
		API.GetHashKey("mp_f_freemode_01"),
		API.GetHashKey("player_zero")
	};

	[Update]
	private async Coroutine WhitelistTask()
	{
		await Script.Wait(5000);
		if (!((Entity)(object)Game.PlayerPed == (Entity)null) && SpawnScript.HasSpawned && (int)StaffLevelScript.StaffLevel <= 0)
		{
			Model model = ((Entity)Game.PlayerPed).Model;
			if (!pedModelsWhitelist.Contains(model.Hash))
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
				{
					5,
					2,
					"changing ped model",
					$"0x{model.Hash:X}"
				});
			}
		}
	}
}
