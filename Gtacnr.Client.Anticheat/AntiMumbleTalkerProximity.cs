using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class AntiMumbleTalkerProximity : Script
{
	private static bool detected;

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(1000);
		float num = API.MumbleGetTalkerProximity();
		if (num > 31f)
		{
			API.MumbleSetTalkerProximity(30f);
			if (!detected)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4]
				{
					30,
					2,
					"triggering",
					$"trying to set talker proximity to {num}"
				});
			}
			detected = true;
		}
	}
}
