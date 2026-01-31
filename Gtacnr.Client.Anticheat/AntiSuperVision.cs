using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class AntiSuperVision : Script
{
	private async Coroutine SuperVisionCheck()
	{
		await Script.Wait(10000);
		if (API.GetUsingseethrough())
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[3] { 30, 2, "thermal vision" });
		}
		else if (API.GetUsingnightvision())
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[3] { 30, 2, "night vision" });
		}
	}
}
