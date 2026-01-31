using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Characters.Lifecycle;

namespace Gtacnr.Client.Anticheat;

public class AntiTeleportScript : Script
{
	private readonly DetectionThresholdManager teleportDetectionManager = new DetectionThresholdManager(3, TimeSpan.FromMinutes(15.0));

	private static Vector3? previousLocation;

	private int currentFrameCount = API.GetFrameCount();

	private const int CHECK_DELAY = 500;

	public static void JustTeleported()
	{
		previousLocation = null;
	}

	private static bool IsPlayerInSafeState()
	{
		if (!SpectateScript.IsSpectating && !NoClipScript.IsNoClipActive && !ModeratorMenuScript.IsOnDuty)
		{
			return !SpawnScript.HasSpawned;
		}
		return true;
	}

	[Update]
	private async Coroutine Check()
	{
		int previousFrameCount = currentFrameCount;
		await BaseScript.Delay(500);
		currentFrameCount = API.GetFrameCount();
		float num = (float)(currentFrameCount - previousFrameCount) / 0.5f;
		if (IsPlayerInSafeState())
		{
			previousLocation = null;
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		Vector3 val = default(Vector3);
		if (position == val)
		{
			return;
		}
		if (!previousLocation.HasValue)
		{
			previousLocation = position;
		}
		val = previousLocation.Value;
		float num2 = ((Vector3)(ref val)).DistanceToSquared2D(position);
		VehicleSeat seatIndex = Game.PlayerPed.SeatIndex;
		if (num2 > 40000f && ((int)seatIndex == -1 || (int)seatIndex == -3))
		{
			teleportDetectionManager.AddDetection();
			string text = (((int)seatIndex == -3) ? "on-foot" : Game.GetGXTEntry(API.GetDisplayNameFromVehicleModel(Model.op_Implicit(((Entity)Game.PlayerPed.CurrentVehicle).Model))));
			string text2 = Gtacnr.Utils.GenerateMapLink(previousLocation.Value, "From", position, "To");
			BaseScript.TriggerServerEvent("gtacnr:ac:logMePublic", new object[1] { $"\ud83d\udd74 {{0}} could be teleporting ({Math.Sqrt(num2):0.00}m, {text}, {num:0.00}FPS, {seatIndex}). View on [map]({text2})." });
			if (teleportDetectionManager.IsThresholdExceeded)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
				{
					30,
					2,
					"teleporting",
					$"Distance: {Math.Sqrt(num2):0.00}m ({text}), {num:0.00}FPS, {seatIndex}. View on [map]({text2})."
				});
			}
		}
		previousLocation = position;
	}
}
