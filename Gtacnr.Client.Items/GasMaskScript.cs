using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Items;

public class GasMaskScript : Script
{
	private static Dictionary<Sex, List<int>> gasMaskIndices = Gtacnr.Utils.LoadJson<Dictionary<Sex, List<int>>>("data/gasMasks.json");

	[Update]
	private async Coroutine GasMaskTask()
	{
		await BaseScript.Delay(500);
		if (SpawnScript.HasSpawned)
		{
			bool num = IsPlayerWearingGasMask();
			AntiEntityProofs.SetPlayerProofs(num, num);
		}
	}

	public static bool IsPlayerWearingGasMask()
	{
		if (ModeratorMenuScript.IsOnDuty)
		{
			return true;
		}
		if (!SpawnScript.HasSpawned || (Entity)(object)Game.PlayerPed == (Entity)null || !Utils.IsFreemodePed(Game.PlayerPed))
		{
			return false;
		}
		if (Game.PlayerPed.IsSwimming || Game.PlayerPed.IsSwimmingUnderWater)
		{
			return false;
		}
		Sex freemodePedSex = Utils.GetFreemodePedSex(Game.PlayerPed);
		int pedDrawableVariation = API.GetPedDrawableVariation(((PoolObject)Game.PlayerPed).Handle, 1);
		return gasMaskIndices[freemodePedSex].Contains(pedDrawableVariation);
	}
}
