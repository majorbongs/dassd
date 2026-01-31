using CitizenFX.Core;

namespace Gtacnr.Client.Anticheat;

public class HeartbeatScript : Script
{
	[EventHandler("gtacnr:heartbeat")]
	private void OnHeartbeat(int token)
	{
		BaseScript.TriggerServerEvent("gtacnr:heartbeat:response", new object[2] { token, true });
	}
}
