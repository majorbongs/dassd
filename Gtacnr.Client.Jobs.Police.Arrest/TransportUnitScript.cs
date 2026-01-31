using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Events.Holidays.AprilsFools;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class TransportUnitScript : Script
{
	private class TransportUnit
	{
		public Ped Driver { get; set; }

		public Ped Passenger { get; set; }

		public Vehicle Vehicle { get; set; }

		public DateTime CreationTime { get; set; }

		public TransportUnit()
		{
			CreationTime = DateTime.UtcNow;
		}
	}

	private enum DriveToSuspectResponse
	{
		GenericError,
		Success,
		Timeout,
		Died
	}

	private enum TakeSuspectIntoCarResponse
	{
		GenericError,
		Success,
		Timeout,
		Died,
		CantEnter,
		CantArrest
	}

	private static Random random = new Random();

	private static Player target;

	private static Model driverModel = new Model("s_f_y_cop_01");

	private static Model passengerModel = new Model("s_m_y_cop_01");

	private static Model vehicleModel = new Model("pscoutnew");

	private static readonly int DRIVE_TIMEOUT = 30000;

	private static readonly int ENTER_TIMEOUT = 30000;

	private static bool custodyHelpShown;

	private static HashSet<TransportUnit> activeTransportUnits = new HashSet<TransportUnit>();

	public static TransportUnitScript Instance { get; private set; }

	public TransportUnitScript()
	{
		Instance = this;
	}

	public static async void CallTransport()
	{
		if (CuffScript.TargetPlayer == (Player)null)
		{
			return;
		}
		target = CuffScript.TargetPlayer;
		int targetServerId = target.ServerId;
		PlayerState state = LatentPlayers.Get(targetServerId);
		if (!(await Instance.TriggerServerEventAsync<bool>("gtacnr:police:canTransport", new object[1] { targetServerId })))
		{
			Utils.DisplayHelpText("~r~A transport unit for this suspect is already on the way!");
			return;
		}
		_ = state?.IsCuffed;
		_ = state?.IsInCustody;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		Vector3 spawnPos = default(Vector3);
		float spawnHeading = 0f;
		bool nthClosestVehicleNodeFavourDirection = API.GetNthClosestVehicleNodeFavourDirection(position.X, position.Y, position.Z, position.X, position.Y, position.Z, 30 + random.Next(20), ref spawnPos, ref spawnHeading, 1, 1077936128, 0);
		string errMsg = "The ~b~transport unit ~s~couldn't arrest the ~o~suspect~s~.\nThe ~o~suspect ~s~has been teleported to jail.";
		string errMsg2 = "The ~b~transport unit ~s~couldn't arrest the ~o~suspect~s~.";
		string diedMsg = "The ~b~transport unit ~s~is ~r~down~s~! Unable to transport the ~o~suspect~s~.";
		if (!nthClosestVehicleNodeFavourDirection || ((Vector3)(ref spawnPos)).DistanceToSquared(position) > 122500f)
		{
			Utils.DisplayHelpText(errMsg);
			await BaseScript.Delay(1000);
			await ArrestPlayer(targetServerId);
			return;
		}
		if (AprilsFoolsScript.IsAprilsFools)
		{
			driverModel = Model.op_Implicit("a_m_m_fatlatin_01");
			passengerModel = Model.op_Implicit("a_m_m_eastsa_01");
			vehicleModel = Model.op_Implicit("burrito3");
		}
		Utils.DisplayHelpText("You requested a ~b~transport unit ~s~for the suspect!");
		TransportUnit transportUnit;
		try
		{
			DisposableModel driverDispModel = new DisposableModel(driverModel);
			DisposableModel passengerDispModel = new DisposableModel(passengerModel);
			DisposableModel vehicleDispModel = new DisposableModel(vehicleModel);
			await driverDispModel.Load();
			await passengerDispModel.Load();
			await vehicleDispModel.Load();
			transportUnit = await SpawnTransportUnit(spawnPos, spawnHeading);
			driverDispModel.Dispose();
			passengerDispModel.Dispose();
			vehicleDispModel.Dispose();
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
			Utils.DisplayHelpText(errMsg);
			await BaseScript.Delay(1000);
			await ArrestPlayer(targetServerId);
			return;
		}
		DriveToSuspectResponse driveToSuspectResponse = await TaskDriveToSuspect(transportUnit, targetServerId);
		state = LatentPlayers.Get(targetServerId);
		bool num = state?.IsCuffed ?? false;
		bool flag = state?.IsInCustody ?? false;
		if (!num || !flag)
		{
			Utils.DisplayHelpText("The ~b~transport unit ~s~can't transport a ~o~suspect~s~ that's not in custody.");
			DismissTransportUnit(transportUnit, targetServerId);
			return;
		}
		if (driveToSuspectResponse != DriveToSuspectResponse.Success)
		{
			switch (driveToSuspectResponse)
			{
			case DriveToSuspectResponse.GenericError:
			case DriveToSuspectResponse.Timeout:
				Utils.DisplayHelpText(errMsg);
				await BaseScript.Delay(1000);
				await ArrestPlayer(targetServerId);
				break;
			case DriveToSuspectResponse.Died:
				Utils.DisplayHelpText(diedMsg);
				break;
			}
			DismissTransportUnit(transportUnit, targetServerId);
			return;
		}
		Ped character = target.Character;
		if (!((Entity)(object)character == (Entity)null))
		{
			Vector3 position2 = ((Entity)character).Position;
			if (!(((Vector3)(ref position2)).DistanceToSquared(((Entity)transportUnit.Vehicle).Position) > 1600f))
			{
				if (((Entity)character).IsDead)
				{
					Utils.DisplayHelpText("The ~b~transport unit ~s~can't transport an injured ~o~suspect.");
					DismissTransportUnit(transportUnit, targetServerId);
					return;
				}
				switch (await TakeSuspectIntoCar(transportUnit, targetServerId))
				{
				case TakeSuspectIntoCarResponse.GenericError:
				case TakeSuspectIntoCarResponse.Timeout:
				case TakeSuspectIntoCarResponse.CantEnter:
					Utils.DisplayHelpText(errMsg);
					await BaseScript.Delay(1000);
					await ArrestPlayer(targetServerId);
					break;
				case TakeSuspectIntoCarResponse.Died:
					Utils.DisplayHelpText(diedMsg);
					break;
				case TakeSuspectIntoCarResponse.CantArrest:
					Utils.DisplayHelpText(errMsg2);
					break;
				}
				DismissTransportUnit(transportUnit, targetServerId);
				return;
			}
		}
		Utils.DisplayHelpText("The ~b~transport unit ~s~couldn't find the suspect.");
		DismissTransportUnit(transportUnit, targetServerId);
	}

	private static async Task<bool> ArrestPlayer(int targetServerId)
	{
		PlayerState? playerState = LatentPlayers.Get(targetServerId);
		bool flag = playerState?.IsCuffed ?? false;
		bool flag2 = playerState?.IsInCustody ?? false;
		if (!flag || !flag2)
		{
			Utils.DisplayHelpText("The ~b~transport unit ~s~can't transport a ~o~suspect~s~ that's not in custody.");
			return false;
		}
		bool flag3 = await Instance.TriggerServerEventAsync<bool>("gtacnr:police:arrest", new object[2] { targetServerId, false });
		if (!flag3)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
		return flag3;
	}

	private static async Task<TransportUnit> SpawnTransportUnit(Vector3 position, float heading)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		TransportUnit transportUnit = new TransportUnit();
		TransportUnit transportUnit2 = transportUnit;
		transportUnit2.Vehicle = await World.CreateVehicle(vehicleModel, position, heading);
		transportUnit.Vehicle.Mods.Livery = 0;
		Blip obj = ((Entity)transportUnit.Vehicle).AttachBlip();
		API.SetBlipDisplay(((PoolObject)obj).Handle, 8);
		obj.Sprite = (BlipSprite)56;
		obj.Scale = 1f;
		obj.Color = (BlipColor)3;
		obj.Name = "Transport Unit";
		((Entity)transportUnit.Vehicle).State.Set("gtacnr:isTransportUnit", (object)true, true);
		API.SetEntityAsMissionEntity(((PoolObject)transportUnit.Vehicle).Handle, true, true);
		transportUnit.Driver = new Ped(API.CreatePedInsideVehicle(((PoolObject)transportUnit.Vehicle).Handle, 6, (uint)driverModel.Hash, -1, true, true));
		transportUnit.Driver.AlwaysKeepTask = true;
		transportUnit.Driver.BlockPermanentEvents = true;
		API.SetDriverAbility(((PoolObject)transportUnit.Driver).Handle, 1f);
		API.SetPedRandomComponentVariation(((PoolObject)transportUnit.Driver).Handle, false);
		API.SetPedRandomProps(((PoolObject)transportUnit.Driver).Handle);
		API.SetEntityAsMissionEntity(((PoolObject)transportUnit.Driver).Handle, true, true);
		API.SetPedRelationshipGroupHash(((PoolObject)transportUnit.Driver).Handle, (uint)API.GetHashKey("emergency"));
		transportUnit.Passenger = new Ped(API.CreatePedInsideVehicle(((PoolObject)transportUnit.Vehicle).Handle, 6, (uint)passengerModel.Hash, 0, true, true));
		transportUnit.Passenger.AlwaysKeepTask = true;
		transportUnit.Passenger.BlockPermanentEvents = true;
		API.SetPedRandomComponentVariation(((PoolObject)transportUnit.Passenger).Handle, false);
		API.SetPedRandomProps(((PoolObject)transportUnit.Passenger).Handle);
		API.SetEntityAsMissionEntity(((PoolObject)transportUnit.Passenger).Handle, true, true);
		API.SetPedRelationshipGroupHash(((PoolObject)transportUnit.Passenger).Handle, (uint)API.GetHashKey("emergency"));
		List<Entity> entities = new List<Entity>
		{
			(Entity)(object)transportUnit.Vehicle,
			(Entity)(object)transportUnit.Driver,
			(Entity)(object)transportUnit.Passenger
		};
		await AntiEntitySpawnScript.RegisterEntities(entities);
		BaseScript.TriggerServerEvent("gtacnr:entities:tempEntitiesCreated", new object[1] { entities.Select((Entity e) => e.NetworkId).Json() });
		activeTransportUnits.Add(transportUnit);
		return transportUnit;
	}

	private static async void DismissTransportUnit(TransportUnit unit, int targetServerId)
	{
		BaseScript.TriggerServerEvent("gtacnr:police:cancelTransport", new object[1] { targetServerId });
		target.State.Set("gtacnr:police:inTransportUnitCustody", (object)false, true);
		if (((Entity)unit.Vehicle).AttachedBlip != (Blip)null && ((PoolObject)((Entity)unit.Vehicle).AttachedBlip).Exists())
		{
			((PoolObject)((Entity)unit.Vehicle).AttachedBlip).Delete();
		}
		if (!unit.Driver.IsInVehicle(unit.Vehicle))
		{
			unit.Driver.Task.EnterVehicle(unit.Vehicle, (VehicleSeat)(-1), -1, 2f, 0);
		}
		if (!unit.Passenger.IsInVehicle(unit.Vehicle))
		{
			unit.Passenger.Task.EnterVehicle(unit.Vehicle, (VehicleSeat)0, -1, 2f, 0);
		}
		while (!unit.Driver.IsInVehicle(unit.Vehicle))
		{
			await BaseScript.Delay(500);
		}
		while (!unit.Passenger.IsInVehicle(unit.Vehicle))
		{
			await BaseScript.Delay(500);
		}
		unit.Vehicle.IsSirenActive = false;
		if (((Entity)unit.Driver).IsAlive)
		{
			API.TaskVehicleDriveWander(((PoolObject)unit.Driver).Handle, ((PoolObject)unit.Vehicle).Handle, 15f, 427);
		}
		API.SetEntityAsMissionEntity(((PoolObject)unit.Vehicle).Handle, false, false);
		API.SetEntityAsMissionEntity(((PoolObject)unit.Driver).Handle, false, false);
		API.SetEntityAsMissionEntity(((PoolObject)unit.Passenger).Handle, false, false);
		await BaseScript.Delay(60000);
		DestroyTransportUnit(unit);
	}

	private static void DestroyTransportUnit(TransportUnit unit)
	{
		if (activeTransportUnits.Contains(unit))
		{
			activeTransportUnits.Remove(unit);
		}
		if ((Entity)(object)unit.Driver != (Entity)null && unit.Driver.Exists())
		{
			((PoolObject)unit.Driver).Delete();
		}
		if ((Entity)(object)unit.Passenger != (Entity)null && unit.Passenger.Exists())
		{
			((PoolObject)unit.Passenger).Delete();
		}
		if ((Entity)(object)unit.Vehicle != (Entity)null && unit.Vehicle.Exists())
		{
			((PoolObject)unit.Vehicle).Delete();
		}
		if (((Entity)unit.Vehicle).AttachedBlip != (Blip)null && ((PoolObject)((Entity)unit.Vehicle).AttachedBlip).Exists())
		{
			((PoolObject)((Entity)unit.Vehicle).AttachedBlip).Delete();
		}
	}

	private static async Task<DriveToSuspectResponse> TaskDriveToSuspect(TransportUnit transportUnit, int targetServerId)
	{
		try
		{
			Vector3 entityCoords = API.GetEntityCoords(API.GetPlayerPed(API.GetPlayerFromServerId(targetServerId)), false);
			int num = 828;
			transportUnit.Driver.Task.DriveTo(transportUnit.Vehicle, entityCoords, 15f, 12f, num);
			transportUnit.Vehicle.IsSirenActive = true;
			transportUnit.Vehicle.IsSirenSilent = true;
			transportUnit.Vehicle.LockStatus = (VehicleLockStatus)10;
			BaseScript.TriggerServerEvent("gtacnr:police:transport", new object[2]
			{
				targetServerId,
				API.VehToNet(((PoolObject)transportUnit.Vehicle).Handle)
			});
			DateTime t = DateTime.UtcNow;
			Vector3 position;
			Vector3 position2;
			do
			{
				await BaseScript.Delay(500);
				if (!((Entity)transportUnit.Driver).IsAlive || !((Entity)transportUnit.Passenger).IsAlive)
				{
					return DriveToSuspectResponse.Died;
				}
				if (Gtacnr.Utils.CheckTimePassed(t, DRIVE_TIMEOUT))
				{
					return DriveToSuspectResponse.Timeout;
				}
				position = ((Entity)transportUnit.Vehicle).Position;
				position2 = ((Entity)Game.PlayerPed).Position;
			}
			while (!(((Vector3)(ref position)).DistanceToSquared(position2) < 324f));
			return DriveToSuspectResponse.Success;
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
			return DriveToSuspectResponse.GenericError;
		}
	}

	private static async Task<TakeSuspectIntoCarResponse> TakeSuspectIntoCar(TransportUnit transportUnit, int targetServerId)
	{
		_ = 7;
		try
		{
			int playerFromServerId = API.GetPlayerFromServerId(targetServerId);
			Ped targetPed = new Ped(API.GetPlayerPed(playerFromServerId));
			transportUnit.Passenger.Task.LeaveVehicle((LeaveVehicleFlags)0);
			Tasks task = transportUnit.Passenger.Task;
			Vector3 val = default(Vector3);
			task.GoTo((Entity)(object)targetPed, val, 20000);
			DateTime t = DateTime.UtcNow;
			do
			{
				await BaseScript.Delay(500);
				if (!((Entity)transportUnit.Driver).IsAlive || !((Entity)transportUnit.Passenger).IsAlive)
				{
					return TakeSuspectIntoCarResponse.Died;
				}
				if (Gtacnr.Utils.CheckTimePassed(t, ENTER_TIMEOUT))
				{
					return TakeSuspectIntoCarResponse.Timeout;
				}
				_ = ((Entity)transportUnit.Vehicle).Position;
				_ = ((Entity)Game.PlayerPed).Position;
				val = ((Entity)transportUnit.Passenger).Position;
			}
			while (!(((Vector3)(ref val)).DistanceToSquared(((Entity)targetPed).Position) < 4f));
			transportUnit.Passenger.Task.ClearAll();
			await BaseScript.Delay(1000);
			transportUnit.Passenger.PlayAmbientSpeech("ARREST_PLAYER", (SpeechModifier)3);
			if (!custodyHelpShown)
			{
				Utils.DisplayHelpText("The ~o~suspect ~s~is now in the ~b~NPC~s~'s custody. It's safe to walk away, but if the NPC is ~r~killed~s~, the suspect can run away.");
				custodyHelpShown = true;
			}
			await BaseScript.Delay(500);
			if (await Instance.TriggerServerEventAsync<int>("gtacnr:police:forceEnterVehicle", new object[3]
			{
				targetServerId,
				((Entity)transportUnit.Vehicle).NetworkId,
				((Entity)transportUnit.Passenger).NetworkId
			}) != 1)
			{
				return TakeSuspectIntoCarResponse.CantEnter;
			}
			API.TaskOpenVehicleDoor(((PoolObject)transportUnit.Passenger).Handle, ((PoolObject)transportUnit.Vehicle).Handle, -1, 2, 1f);
			t = DateTime.UtcNow;
			while (true)
			{
				await BaseScript.Delay(500);
				if (targetPed.IsInVehicle(transportUnit.Vehicle))
				{
					break;
				}
				if (!((Entity)transportUnit.Driver).IsAlive || !((Entity)transportUnit.Passenger).IsAlive)
				{
					return TakeSuspectIntoCarResponse.Died;
				}
				if (Gtacnr.Utils.CheckTimePassed(t, ENTER_TIMEOUT))
				{
					return TakeSuspectIntoCarResponse.Timeout;
				}
			}
			transportUnit.Passenger.Task.EnterVehicle(transportUnit.Vehicle, (VehicleSeat)0, -1, 1f, 0);
			await BaseScript.Delay(7000);
			API.TaskVehicleDriveWander(((PoolObject)transportUnit.Driver).Handle, ((PoolObject)transportUnit.Vehicle).Handle, 12f, 427);
			((PoolObject)((Entity)transportUnit.Vehicle).AttachedBlip).Delete();
			transportUnit.Vehicle.IsSirenActive = false;
			await BaseScript.Delay(7000);
			if (!(await ArrestPlayer(targetServerId)))
			{
				return TakeSuspectIntoCarResponse.CantArrest;
			}
			return TakeSuspectIntoCarResponse.Success;
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
			return TakeSuspectIntoCarResponse.GenericError;
		}
	}

	[Update]
	private async Coroutine CleanupTask()
	{
		await Script.Wait(10000);
		if (activeTransportUnits.Count == 0)
		{
			return;
		}
		foreach (TransportUnit item in activeTransportUnits.ToList())
		{
			if (Gtacnr.Utils.CheckTimePassed(item.CreationTime, 120000.0))
			{
				DestroyTransportUnit(item);
			}
		}
	}
}
