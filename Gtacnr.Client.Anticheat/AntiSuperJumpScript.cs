using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class AntiSuperJumpScript : Script
{
	private readonly DetectionThresholdManager detectionManager = new DetectionThresholdManager(3, TimeSpan.FromSeconds(30.0));

	[Update]
	private async Coroutine BeastJumpCheck()
	{
		await Script.Wait(1000);
		if (API.IsPedDoingBeastJump(((PoolObject)Game.PlayerPed).Handle))
		{
			detectionManager.AddDetection();
			if (detectionManager.IsThresholdExceeded)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4] { 30, 2, "super jump", "beast" });
			}
		}
	}

	private bool JumpCheck(int pedId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (API.IsPedJumping(pedId) && !API.IsPedFalling(pedId) && !API.IsPedRagdoll(pedId))
		{
			return API.GetEntityVelocity(pedId).Z > 0f;
		}
		return false;
	}

	[Update]
	private async Coroutine SuperJumpCheck()
	{
		await Script.Wait(50);
		int pedId = API.PlayerPedId();
		if (!JumpCheck(pedId))
		{
			return;
		}
		DateTime startTime = DateTime.UtcNow;
		do
		{
			await Script.Wait(50);
		}
		while (JumpCheck(pedId));
		TimeSpan timeSpan = DateTime.UtcNow - startTime;
		if (!(timeSpan < TimeSpan.FromMilliseconds(550.0)))
		{
			detectionManager.AddDetection();
			if (detectionManager.IsThresholdExceeded)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4]
				{
					30,
					2,
					"super jump",
					$"time ({timeSpan.TotalMilliseconds})"
				});
			}
		}
	}
}
