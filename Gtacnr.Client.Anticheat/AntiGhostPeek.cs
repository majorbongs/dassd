using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public sealed class AntiGhostPeek : Script
{
	private static Vector3 RotationToDirection(Vector3 rotation)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = new Vector3
		{
			X = (float)Math.PI / 180f * rotation.X,
			Y = (float)Math.PI / 180f * rotation.Y,
			Z = (float)Math.PI / 180f * rotation.Z
		};
		return new Vector3
		{
			X = (float)((0.0 - Math.Sin(val.Z)) * Math.Abs(Math.Cos(val.X))),
			Y = (float)(Math.Cos(val.Z) * Math.Abs(Math.Cos(val.X))),
			Z = (float)Math.Sin(val.X)
		};
	}

	private static void RayCastGamePlayWeapon(int weapon, float distance, out RaycastResult raycastResult, out Vector3 destination, IntersectOptions intersectOptions = (IntersectOptions)1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		Vector3 gameplayCamRot = API.GetGameplayCamRot(2);
		Vector3 entityCoords = API.GetEntityCoords(weapon, true);
		Vector3 gameplayCamCoord = API.GetGameplayCamCoord();
		Vector3 val = RotationToDirection(gameplayCamRot);
		destination = new Vector3(gameplayCamCoord.X + val.X * distance, gameplayCamCoord.Y + val.Y * distance, gameplayCamCoord.Z + val.Z * distance);
		raycastResult = World.Raycast(entityCoords, destination, intersectOptions, (Entity)null);
	}

	private static void RayCastGamePlayCamera(int weapon, float distance, out RaycastResult raycastResult, out Vector3 destination, IntersectOptions intersectOptions = (IntersectOptions)1)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		Vector3 gameplayCamRot = API.GetGameplayCamRot(2);
		API.GetEntityCoords(weapon, true);
		Vector3 gameplayCamCoord = API.GetGameplayCamCoord();
		Vector3 val = RotationToDirection(gameplayCamRot);
		destination = new Vector3(gameplayCamCoord.X + val.X * distance, gameplayCamCoord.Y + val.Y * distance, gameplayCamCoord.Z + val.Z * distance);
		raycastResult = World.Raycast(gameplayCamCoord, destination, intersectOptions, (Entity)null);
	}

	[Update]
	private async Coroutine CheckTask()
	{
		int sleep = 100;
		int num = API.PlayerId();
		int currentPedWeaponEntityIndex = API.GetCurrentPedWeaponEntityIndex(API.PlayerPedId());
		if (currentPedWeaponEntityIndex <= 0 || !Game.PlayerPed.IsAiming)
		{
			await BaseScript.Delay(500);
		}
		else
		{
			RayCastGamePlayWeapon(currentPedWeaponEntityIndex, 15f, out var raycastResult, out var _, (IntersectOptions)1);
			if (((RaycastResult)(ref raycastResult)).DitHit)
			{
				RayCastGamePlayCamera(currentPedWeaponEntityIndex, 1000f, out var raycastResult2, out var _, (IntersectOptions)1);
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				Vector3 hitPosition = ((RaycastResult)(ref raycastResult)).HitPosition;
				float num2 = ((Vector3)(ref hitPosition)).DistanceToSquared(position);
				hitPosition = ((RaycastResult)(ref raycastResult2)).HitPosition;
				float num3 = ((Vector3)(ref hitPosition)).DistanceToSquared(position);
				if (num2 < num3)
				{
					hitPosition = ((RaycastResult)(ref raycastResult)).HitPosition;
					if (((Vector3)(ref hitPosition)).DistanceToSquared(((RaycastResult)(ref raycastResult2)).HitPosition) > 2.5f)
					{
						sleep = 0;
						API.DisablePlayerFiring(num, true);
						API.DisableControlAction(0, 106, true);
					}
				}
			}
		}
		await BaseScript.Delay(sleep);
	}
}
