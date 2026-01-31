using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Sessions;
using Gtacnr.Client.Tutorials;

namespace Gtacnr.Client.Anticheat;

public class AntiFreeCam : Script
{
	private readonly DetectionThresholdManager freeCamDetectionManager = new DetectionThresholdManager(3, TimeSpan.FromMinutes(1.0));

	private const float NORMAL_RANGE = 17f;

	private const float VEHICLE_RANGE = 35f;

	private const float CINEMATIC_RANGE = 175f;

	private const float HUGE_VEH_RANGE = 250f;

	private readonly int[] buggedVehicleHashes = new string[3] { "towtruck", "towtruck2", "streamer216x" }.Select((string s) => API.GetHashKey(s)).ToArray();

	private const int TASK_DELAY = 10000;

	private int currentFrameCount = API.GetFrameCount();

	private bool IsPlayerInSafeState()
	{
		if (!SpectateScript.IsSpectating && !NoClipScript.IsNoClipActive && !ModeratorMenuScript.IsOnDuty && !TutorialScript.IsInTutorial && !DealershipScript.IsInDealership && SpawnScript.HasSpawned)
		{
			return SessionStatsScript.IsRunning;
		}
		return true;
	}

	[Update]
	private async Coroutine CheckTask()
	{
		int previousFrameCount = currentFrameCount;
		await Script.Wait(10000);
		currentFrameCount = API.GetFrameCount();
		float fps = (float)(currentFrameCount - previousFrameCount) / 10f;
		if (!IsPlayerInSafeState() && !(await FreeCamDetection(fps)))
		{
			SpectatorModeDetection();
		}
	}

	private async Task<bool> FreeCamDetection(float fps)
	{
		if (API.IsPlayerSwitchInProgress())
		{
			return false;
		}
		if (Utils.IsTeleporting)
		{
			return false;
		}
		if (DeathScript.IsAlive != true)
		{
			return false;
		}
		if (fps < 15f)
		{
			return false;
		}
		int playerPed = API.PlayerPedId();
		bool flag = API.IsCinematicCamRendering() || API.IsCinematicIdleCamRendering();
		bool flag2 = API.IsPedInAnyVehicle(playerPed, true) || API.IsPedGettingIntoAVehicle(playerPed);
		bool flag3 = false;
		if (flag2)
		{
			int num = API.GetVehiclePedIsUsing(playerPed);
			if (num == 0)
			{
				num = API.GetVehiclePedIsEntering(playerPed);
			}
			if (num != 0)
			{
				if (API.GetEntityModel(num).In(buggedVehicleHashes))
				{
					API.TaskLeaveVehicle(playerPed, num, 0);
					return false;
				}
				VehicleClass val = (VehicleClass)API.GetVehicleClass(num);
				if ((int)val == 16 || (int)val == 15 || (int)val == 14)
				{
					flag3 = true;
				}
			}
		}
		float maxRange = (flag3 ? 250f : (flag ? 175f : (flag2 ? 35f : 17f)));
		Vector3 entityCoords = API.GetEntityCoords(playerPed, false);
		Vector3 finalRenderedCamCoord = API.GetFinalRenderedCamCoord();
		float num2 = Vector3.Distance(entityCoords, finalRenderedCamCoord);
		if (num2 <= maxRange)
		{
			return false;
		}
		await BaseScript.Delay(50);
		entityCoords = API.GetEntityCoords(playerPed, false);
		finalRenderedCamCoord = API.GetFinalRenderedCamCoord();
		num2 = Vector3.Distance(entityCoords, finalRenderedCamCoord);
		if (num2 <= maxRange)
		{
			return false;
		}
		freeCamDetectionManager.AddDetection();
		if (freeCamDetectionManager.IsThresholdExceeded)
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
			{
				30,
				2,
				"free cam",
				$"Distance: {num2}, max range: {maxRange}, character pos: {entityCoords}, camera pos: {finalRenderedCamCoord}"
			});
		}
		return true;
	}

	private bool SpectatorModeDetection()
	{
		if (!API.NetworkIsInSpectatorMode())
		{
			return false;
		}
		BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4] { 30, 2, "free cam", "spectator mode" });
		return true;
	}
}
