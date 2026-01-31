using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Vehicles;

public class ActiveVehicleScript : Script
{
	private static ActiveVehicleScript instance;

	private static Vehicle activeVehicle;

	private static int activeVehicleNetId;

	private static string activeVehicleStoredId;

	private static Blip _activeVehicleAttachedBlip;

	private static Blip activeVehicleStaticBlip;

	private static float lastStoredFuel;

	private static Blip activeVehicleAttachedBlip
	{
		get
		{
			return _activeVehicleAttachedBlip;
		}
		set
		{
			if (value != (Blip)null)
			{
				Blip obj = activeVehicleStaticBlip;
				if (obj != null)
				{
					((PoolObject)obj).Delete();
				}
				activeVehicleStaticBlip = null;
			}
			_activeVehicleAttachedBlip = value;
		}
	}

	public static Vehicle ActiveVehicle => activeVehicle;

	public static int ActiveVehicleNetId => activeVehicleNetId;

	public static string ActiveVehicleStoredId => activeVehicleStoredId;

	public static VehicleHealthData ActiveVehicleHealthData { get; private set; }

	public ActiveVehicleScript()
	{
		instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	public static async Task<bool> SetActiveVehicle(StoredVehicle storedVehicle, bool clientOnly = false)
	{
		_ = 1;
		try
		{
			if (storedVehicle == null)
			{
				DeletePersonalVehicleBlip();
				activeVehicle = null;
				activeVehicleNetId = 0;
				activeVehicleStoredId = null;
				if (clientOnly)
				{
					return true;
				}
				Vehicle previousVeh = activeVehicle;
				bool result = await instance.TriggerServerEventAsync<bool>("gtacnr:vehicles:resetActive", new object[0]);
				if ((Entity)(object)previousVeh != (Entity)null && previousVeh.Exists())
				{
					((PoolObject)previousVeh).Delete();
				}
				return result;
			}
			if (!clientOnly)
			{
				Vehicle previousVeh = activeVehicle;
				bool num = await instance.TriggerServerEventAsync<bool>("gtacnr:vehicles:setActive", new object[2] { storedVehicle.Id, storedVehicle.NetworkId });
				if ((Entity)(object)previousVeh != (Entity)null && previousVeh.Exists())
				{
					((PoolObject)previousVeh).Delete();
				}
				if (!num)
				{
					return false;
				}
			}
			if (activeVehicleNetId != 0)
			{
				DeletePersonalVehicleBlip();
			}
			if (!API.NetworkDoesEntityExistWithNetworkId(storedVehicle.NetworkId))
			{
				return false;
			}
			Vehicle val = new Vehicle(API.NetworkGetEntityFromNetworkId(storedVehicle.NetworkId));
			if ((Entity)(object)val == (Entity)null)
			{
				return false;
			}
			activeVehicle = val;
			activeVehicleNetId = ((Entity)val).NetworkId;
			activeVehicleStoredId = storedVehicle.Id;
			lastStoredFuel = storedVehicle.HealthData.Fuel;
			return true;
		}
		catch (Exception arg)
		{
			Debug.WriteLine($"[SetActiveVehicle] {arg}");
			return false;
		}
	}

	public static async Task<bool> ResetActiveVehicle(bool clientOnly = false)
	{
		return await SetActiveVehicle(null, clientOnly);
	}

	private async Task<bool> MarkActiveVehicleAsDestroyed()
	{
		DeletePersonalVehicleBlip();
		activeVehicle = null;
		activeVehicleNetId = 0;
		activeVehicleStoredId = null;
		return await TriggerServerEventAsync<bool>("gtacnr:vehicles:markActiveAsDestroyed", new object[0]);
	}

	private static void CreatePersonalVehicleBlip()
	{
		if (!((Entity)(object)activeVehicle == (Entity)null) && !(((Entity)activeVehicle).AttachedBlip != (Blip)null))
		{
			((Entity)activeVehicle).AttachBlip();
			((Entity)activeVehicle).AttachedBlip.Sprite = (BlipSprite)225;
			((Entity)activeVehicle).AttachedBlip.Color = (BlipColor)4;
			((Entity)activeVehicle).AttachedBlip.Scale = 0.8f;
			((Entity)activeVehicle).AttachedBlip.IsShortRange = true;
			((Entity)activeVehicle).AttachedBlip.Name = "Personal Vehicle";
			activeVehicleAttachedBlip = ((Entity)activeVehicle).AttachedBlip;
		}
	}

	private static void DeletePersonalVehicleBlip()
	{
		Blip obj = activeVehicleStaticBlip;
		if (obj != null)
		{
			((PoolObject)obj).Delete();
		}
		activeVehicleStaticBlip = null;
		if (!((Entity)(object)activeVehicle == (Entity)null) && !(((Entity)activeVehicle).AttachedBlip == (Blip)null) && ((PoolObject)((Entity)activeVehicle).AttachedBlip).Exists())
		{
			((Entity)activeVehicle).AttachedBlip.Alpha = 0;
			((PoolObject)((Entity)activeVehicle).AttachedBlip).Delete();
		}
	}

	[Update]
	private async Coroutine PersonalVehicleTask()
	{
		await Script.Wait(500);
		try
		{
			if ((Entity)(object)activeVehicle == (Entity)null || !activeVehicle.Exists())
			{
				if (activeVehicleAttachedBlip != (Blip)null)
				{
					activeVehicleAttachedBlip.Alpha = 0;
					((PoolObject)activeVehicleAttachedBlip).Delete();
					activeVehicleAttachedBlip = null;
				}
				if (activeVehicleNetId <= 0)
				{
					return;
				}
				VehicleState vehicleState = LatentVehicleStateScript.Get(activeVehicleNetId);
				if (vehicleState != null)
				{
					if (activeVehicleStaticBlip == (Blip)null)
					{
						activeVehicleStaticBlip = World.CreateBlip(vehicleState.Position);
						activeVehicleStaticBlip.Sprite = (BlipSprite)225;
						activeVehicleStaticBlip.Color = (BlipColor)4;
						activeVehicleStaticBlip.Scale = 0.8f;
						activeVehicleStaticBlip.Name = "Personal Vehicle";
					}
					else
					{
						activeVehicleStaticBlip.Position = vehicleState.Position;
					}
				}
				if (API.NetworkDoesEntityExistWithNetworkId(activeVehicleNetId))
				{
					Entity obj = Entity.FromNetworkId(activeVehicleNetId);
					activeVehicle = (Vehicle)(object)((obj is Vehicle) ? obj : null);
					if ((Entity)(object)activeVehicle != (Entity)null)
					{
						Print($"Recovered active vehicle info ({((PoolObject)activeVehicle).Handle}).");
					}
					else
					{
						Print($"~y~Warning: ~s~Unable to recover active vehicle info (net ID: {activeVehicleNetId}).");
					}
				}
			}
			else if (((Entity)activeVehicle).IsDead)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.ACTIVE_VEHICLE_DESTROYED, MainMenuScript.OpenMenuControlString));
				if (await MarkActiveVehicleAsDestroyed())
				{
					VehiclesMenuScript.InvalidateCache();
				}
			}
			else
			{
				ActiveVehicleHealthData = Utils.GetVehicleHealthData(activeVehicle);
				VehicleState vehicleState2 = LatentVehicleStateScript.Get(activeVehicleNetId);
				if (vehicleState2 != null)
				{
					lastStoredFuel = vehicleState2.Fuel;
				}
				ActiveVehicleHealthData.Fuel = lastStoredFuel;
				if ((Entity)(object)activeVehicle.Driver != (Entity)(object)Game.PlayerPed)
				{
					CreatePersonalVehicleBlip();
				}
				else
				{
					DeletePersonalVehicleBlip();
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:vehicles:activeVehicleImpounded")]
	private async void OnActiveVehicleImpounded()
	{
		await ResetActiveVehicle(clientOnly: true);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_IMPOUNDED));
	}

	[EventHandler("gtacnr:vehicles:activeVehicleRemoved")]
	private async void OnActiveVehicleRemoved()
	{
		await TriggerServerEventAsync<int>("gtacnr:vehicles:storeActive", new object[1] { ActiveVehicleHealthData.Json() });
		await ResetActiveVehicle(clientOnly: true);
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		Job? jobData = Gtacnr.Data.Jobs.GetJobData(e.PreviousJobId);
		Job jobData2 = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId);
		if ((jobData != null && jobData.SeparateVehicles) || (jobData2 != null && jobData2.SeparateVehicles))
		{
			ResetActiveVehicle(clientOnly: true);
		}
	}
}
