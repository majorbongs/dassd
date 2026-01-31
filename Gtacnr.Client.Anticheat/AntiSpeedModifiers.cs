using System;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class AntiSpeedModifiers : Script
{
	private readonly DetectionThresholdManager speedModifiersDetectionManager = new DetectionThresholdManager(3, TimeSpan.FromMinutes(1.0));

	private bool detected;

	public static float RunningSpeedModifier = 1f;

	private bool IsPedInSafeState(int pedId)
	{
		if (!API.IsPedFalling(pedId))
		{
			return API.IsPedRagdoll(pedId);
		}
		return true;
	}

	[Update]
	private async Coroutine SpeedModifiersCheck()
	{
		await Script.Wait(2000);
		if (detected)
		{
			return;
		}
		int playerPed = API.PlayerPedId();
		if (API.IsPedInAnyVehicle(playerPed, true))
		{
			int num = API.GetVehiclePedIsIn(playerPed, false);
			if (num == 0)
			{
				num = API.GetVehiclePedIsEntering(playerPed);
			}
			if (API.DoesEntityExist(num) && !API.IsEntityAttachedToAnyVehicle(num))
			{
				float num2 = API.GetVehicleEstimatedMaxSpeed(num) + 40f;
				Vector3 entityVelocity = API.GetEntityVelocity(num);
				entityVelocity.Z = 0f;
				float num3 = ((Vector3)(ref entityVelocity)).LengthSquared();
				if (num3 > num2.Square())
				{
					SendBanEvent($"Vehicle speed {Math.Sqrt(num3)}/{num2}");
				}
			}
		}
		else if (!IsPedInSafeState(playerPed) && !World.GetAllVehicles().Any((Vehicle v) => ((Entity)v).IsTouching(Entity.FromHandle(playerPed))))
		{
			float num4 = 12f * RunningSpeedModifier;
			Vector3 entityVelocity2 = API.GetEntityVelocity(playerPed);
			entityVelocity2.Z = 0f;
			float num5 = ((Vector3)(ref entityVelocity2)).LengthSquared();
			if (num5 > num4.Square())
			{
				SendBanEvent($"On-foot speed {Math.Sqrt(num5)}/{num4}");
			}
		}
	}

	private void SendBanEvent(string internalInfo)
	{
		speedModifiersDetectionManager.AddDetection();
		if (speedModifiersDetectionManager.IsThresholdExceeded)
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4] { 30, 2, "speed modifiers", internalInfo });
			detected = true;
		}
	}
}
