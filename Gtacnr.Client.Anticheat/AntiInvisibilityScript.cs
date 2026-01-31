using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Tutorials;

namespace Gtacnr.Client.Anticheat;

public class AntiInvisibilityScript : Script
{
	private static bool IsPlayerInSafeState()
	{
		if (!SpectateScript.IsSpectating && !NoClipScript.IsNoClipActive && !ModeratorMenuScript.IsOnDuty && SpawnScript.HasSpawned && !((Entity)Game.PlayerPed).IsDead && !TutorialScript.IsInTutorial && DeathScript.IsAlive == true && !Utils.IsInvisible)
		{
			return ShoppingScript.IsInPropPreview;
		}
		return true;
	}

	[Update]
	private async Coroutine DetectionTask()
	{
		await Script.Wait(5000);
		if (!IsPlayerInSafeState())
		{
			int num = API.PlayerPedId();
			if (!API.IsEntityVisible(num))
			{
				API.SetEntityVisible(num, true, false);
			}
			if (API.GetEntityAlpha(num) < 255)
			{
				API.SetEntityAlpha(num, 255, 0);
			}
		}
	}
}
