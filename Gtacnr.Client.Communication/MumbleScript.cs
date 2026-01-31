using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Communication;

public class MumbleScript : Script
{
	private bool isInitialized;

	private bool isConnectedToMumble;

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		int convarInt = API.GetConvarInt("profile_voiceEnable", 0);
		int convarInt2 = API.GetConvarInt("profile_voiceTalkEnabled", 0);
		isConnectedToMumble = true;
		if (convarInt != 1)
		{
			Utils.DisplayHelpText("Your voice chat is ~r~disabled~s~! You can enable it in the pause menu.");
			isConnectedToMumble = false;
		}
		else if (convarInt2 != 1)
		{
			Utils.DisplayHelpText("Your microphone is ~r~disabled~s~! You can enable it in the pause menu.");
		}
		isInitialized = true;
	}

	[EventHandler("gtacnr:muted")]
	private void OnMuted(bool isMuted)
	{
		if (isMuted)
		{
			API.MumbleSetActive(false);
		}
		else
		{
			API.MumbleSetActive(true);
		}
	}

	[Update]
	private async Coroutine DetectMumbleConnectionTask()
	{
		if (!isInitialized)
		{
			return;
		}
		if (!isConnectedToMumble)
		{
			BaseScript.TriggerServerEvent("gtacnr:mumbleDisconnected", new object[0]);
			while (!API.MumbleIsConnected())
			{
				await Script.Wait(1000);
			}
			isConnectedToMumble = true;
			Utils.DisplayHelpText("You are now connected to the ~p~voice server~s~.");
		}
		else
		{
			BaseScript.TriggerServerEvent("gtacnr:mumbleConnected", new object[0]);
			while (API.MumbleIsConnected())
			{
				await Script.Wait(1000);
			}
			isConnectedToMumble = false;
			Utils.DisplayHelpText("You have been disconnected from the ~p~voice server~s~.");
		}
	}
}
