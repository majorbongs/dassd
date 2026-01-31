using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Sessions;

public class SessionScript : Script
{
	public SessionScript()
	{
		base.Update += FirstTick;
	}

	private async Coroutine FirstTick()
	{
		do
		{
			await BaseScript.Delay(0);
		}
		while (!API.NetworkIsSessionStarted());
		BaseScript.TriggerServerEvent("gtacnr:hardcap:playerActivated", new object[0]);
		base.Update -= FirstTick;
	}
}
