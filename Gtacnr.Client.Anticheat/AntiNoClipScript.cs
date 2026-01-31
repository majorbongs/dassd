using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Inventory;
using Gtacnr.Client.Jobs.Mechanic;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Sessions;
using Gtacnr.Client.Tutorials;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Anticheat;

public class AntiNoClipScript : Script
{
	private static readonly DetectionThresholdManager positionDetectionManager = new DetectionThresholdManager(5, TimeSpan.FromSeconds(60.0));

	private static readonly DetectionThresholdManager disabledCollisionDetectionManager = new DetectionThresholdManager(8, TimeSpan.FromSeconds(14.0));

	private bool attached;

	private bool detected;

	private bool parachuteJustEquipped;

	private Vector3? previousDetectedPosition;

	private static readonly Vector3[] teleportPositions = (Vector3[])(object)new Vector3[3]
	{
		new Vector3(-672.4422f, -912.625f, 38.6285f),
		new Vector3(-696.4726f, -862.397f, 30.3379f),
		new Vector3(1729.7126f, 6408.114f, 34.3481f)
	};

	public AntiNoClipScript()
	{
		ArmoryScript.WeaponEquipped += OnWeaponEquipped;
	}

	private void OnWeaponEquipped(object sender, ArmoryWeaponEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)e.WeaponHash == -72657034)
		{
			parachuteJustEquipped = true;
		}
	}

	private static bool IsPlayerInSafeState()
	{
		if (!SpectateScript.IsSpectating && !NoClipScript.IsNoClipActive && !ModeratorMenuScript.IsOnDuty && SpawnScript.HasSpawned && !((Entity)Game.PlayerPed).IsDead && !TutorialScript.IsInTutorial && !DeathScript.HasSpawnProtection && DeathScript.IsAlive == true && !Utils.IsFrozen && !Utils.IsInvisible && !Utils.IsTeleporting && !ShoppingScript.IsInPropPreview)
		{
			return SessionStatsScript.IsRunning;
		}
		return true;
	}

	private bool IsPositionAboveGround(Vector3 position)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		if (API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref num, true))
		{
			return position.Z > num + 4f;
		}
		return true;
	}

	private bool IsEntityFallingThroughMap(int entityId)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return IsEntityFallingThroughMap(API.GetEntityVelocity(entityId), API.GetEntityCoords(entityId, false));
	}

	private bool IsEntityFallingThroughMap(Vector3 velocity, Vector3 position)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (velocity.Z < -10f)
		{
			return position.Z < -10f;
		}
		return false;
	}

	private bool IsEntityCollisionLoaded(int entityId)
	{
		if (API.HasCollisionLoadedAroundEntity(entityId))
		{
			return !API.IsEntityWaitingForWorldCollision(entityId);
		}
		return false;
	}

	private bool IsPlayerAboveGround(int playerPed)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		if (API.IsPedFalling(playerPed) || API.IsPedRagdoll(playerPed))
		{
			return false;
		}
		if (API.IsPedClimbing(playerPed))
		{
			return false;
		}
		if (API.IsPedSwimming(playerPed) || API.IsPedSwimmingUnderWater(playerPed))
		{
			return false;
		}
		if (API.IsPedInParachuteFreeFall(playerPed))
		{
			return false;
		}
		if (API.GetPedParachuteState(playerPed) > 0)
		{
			return false;
		}
		if (parachuteJustEquipped)
		{
			parachuteJustEquipped = false;
			return false;
		}
		Vector3 entityVelocity = API.GetEntityVelocity(playerPed);
		Vector3 playerPosition = ((Entity)Game.PlayerPed).Position;
		if (entityVelocity.Z > 10f && playerPosition.Z > 100f)
		{
			return false;
		}
		if (IsEntityFallingThroughMap(entityVelocity, playerPosition))
		{
			return false;
		}
		if (((Vector3)(ref entityVelocity)).LengthSquared() < 4f)
		{
			if (Utils.GetIsTaskActiveEx(playerPed, TaskTypeIndex.CTaskUseScenario))
			{
				return false;
			}
			if (CuffedScript.IsInCustody)
			{
				return false;
			}
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		position.Z += 0.5f;
		Vector3 position2 = ((Entity)Game.PlayerPed).Position;
		position2.Z -= 0.5f;
		RaycastResult val = World.RaycastCapsule(position, position2, 2f, (IntersectOptions)19, (Entity)(object)Game.PlayerPed);
		if (((RaycastResult)(ref val)).HitPosition != Vector3.Zero)
		{
			return false;
		}
		Vector3 zero = Vector3.Zero;
		Vector3 zero2 = Vector3.Zero;
		API.GetModelDimensions((uint)API.GetEntityModel(playerPed), ref zero, ref zero2);
		if (((IEnumerable<Vector3>)(object)new Vector3[7]
		{
			API.GetOffsetFromEntityInWorldCoords(playerPed, zero.X, zero.Y, zero.Z + 0.5f),
			API.GetOffsetFromEntityInWorldCoords(playerPed, zero2.X, zero.Y, zero.Z + 0.5f),
			API.GetOffsetFromEntityInWorldCoords(playerPed, zero2.X, zero2.Y, zero.Z + 0.5f),
			API.GetOffsetFromEntityInWorldCoords(playerPed, zero.X, zero2.Y, zero.Z + 0.5f),
			API.GetOffsetFromEntityInWorldCoords(playerPed, 0f, zero.Y, zero.Z + 0.5f),
			API.GetOffsetFromEntityInWorldCoords(playerPed, 0f, zero2.Y, zero.Z + 0.5f),
			new Vector3(playerPosition.X, playerPosition.Y, playerPosition.Z + 0.5f)
		}).Where((Vector3 p) => !IsPositionAboveGround(p)).Any())
		{
			return false;
		}
		if (previousDetectedPosition.HasValue)
		{
			Vector3 value = previousDetectedPosition.Value;
			if (((Vector3)(ref value)).DistanceToSquared(playerPosition) < 1.5f)
			{
				return false;
			}
		}
		if (teleportPositions.Where((Vector3 p) => ((Vector3)(ref p)).DistanceToSquared2D(playerPosition) <= 15f.Square()).Any())
		{
			float num = 0f;
			if (API.GetGroundZFor_3dCoord(playerPosition.X, playerPosition.Y, playerPosition.Z, ref num, true))
			{
				Vector3 position3 = playerPosition;
				position3.Z = num + 3f;
				((Entity)Game.PlayerPed).Position = position3;
				((Entity)Game.PlayerPed).Velocity = Vector3.Zero;
				return false;
			}
		}
		previousDetectedPosition = playerPosition;
		BaseScript.TriggerServerEvent("gtacnr:ac:logMePublic", new object[1] { $"\ud83d\udd74 {{0}} could be using no clip: {playerPosition}" });
		if (entityVelocity.Z > 2.5f && playerPosition.Z > -10f)
		{
			return false;
		}
		return true;
	}

	private static bool IsVehicleAttachedToTowTruck(int vehicleId)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (API.IsEntityAttached(vehicleId))
		{
			return true;
		}
		Vector3 position = API.GetEntityCoords(vehicleId, false);
		return World.GetAllVehicles().Where(delegate(Vehicle v)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position2 = ((Entity)v).Position;
			return ((Vector3)(ref position2)).DistanceToSquared2D(position) <= 10f.Square() && TowingScript.IsTowTruck(Model.op_Implicit(((Entity)v).Model));
		}).Any();
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		await BaseScript.Delay(30000);
		if (!attached)
		{
			base.Update += Check;
			attached = true;
		}
	}

	private async Coroutine Check()
	{
		await Script.Wait(1000);
		if (IsPlayerInSafeState() || detected)
		{
			return;
		}
		int handle = ((PoolObject)Game.PlayerPed).Handle;
		int vehiclePedIsIn = API.GetVehiclePedIsIn(handle, false);
		bool flag = false;
		if (vehiclePedIsIn == 0)
		{
			flag = IsPlayerAboveGround(handle);
		}
		bool flag2 = false;
		if (vehiclePedIsIn == 0)
		{
			flag2 = API.GetEntityCollisionDisabled(handle) && IsEntityCollisionLoaded(handle) && !API.IsPedClimbing(handle) && !API.IsPedVaulting(handle) && !API.IsPedGettingIntoAVehicle(handle);
		}
		else if ((int)Game.PlayerPed.SeatIndex == -1 && !IsVehicleAttachedToTowTruck(vehiclePedIsIn))
		{
			flag2 = API.GetEntityCollisionDisabled(vehiclePedIsIn) && IsEntityCollisionLoaded(vehiclePedIsIn) && !IsEntityFallingThroughMap(vehiclePedIsIn);
		}
		if (flag)
		{
			positionDetectionManager.AddDetection();
			if (positionDetectionManager.IsThresholdExceeded)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4] { 30, 2, "no clip", "too many positions above ground" });
				detected = true;
				return;
			}
		}
		if (flag2)
		{
			disabledCollisionDetectionManager.AddDetection();
			BaseScript.TriggerServerEvent("gtacnr:ac:logMePublic", new object[1] { "\ud83d\udd74 {0} has disabled collision " + ((vehiclePedIsIn == 0) ? "(ped)" : "(vehicle)") + $" - {((Entity)Game.PlayerPed).Position}" });
			if (disabledCollisionDetectionManager.IsThresholdExceeded)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4] { 30, 2, "no clip", "disabled collision" });
				detected = true;
			}
		}
	}
}
