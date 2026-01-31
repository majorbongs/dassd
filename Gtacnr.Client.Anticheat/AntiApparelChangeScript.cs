using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Editor;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Premium;

namespace Gtacnr.Client.Anticheat;

public class AntiApparelChangeScript : Script
{
	private static readonly DetectionThresholdManager detectionManager = new DetectionThresholdManager(3, TimeSpan.FromSeconds(60.0));

	private static bool detected = false;

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		base.Update += Check;
	}

	private async Coroutine Check()
	{
		await BaseScript.Delay(5000);
		if (detected || CharacterCreationScript.IsInCreator || DeathScript.IsAlive != true || !CustomScript.DataLoaded)
		{
			return;
		}
		Ped playerPed = Game.PlayerPed;
		if (!Clothes.CurrentApparel.GetAppliedData(playerPed).DoesMatch(playerPed, out string error))
		{
			detectionManager.AddDetection();
			if (detectionManager.IsThresholdExceeded)
			{
				detected = true;
				BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4]
				{
					30,
					2,
					"apparel change",
					error ?? ""
				});
			}
		}
	}
}
