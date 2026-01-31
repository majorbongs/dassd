using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Crimes.Robberies.Jewelry;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using MenuAPI;
using Rock.Collections;

namespace Gtacnr.Client.Estates.Garages;

public class GarageScript : Script
{
	private class GarageKnockHandler : KnockHandler
	{
		protected override void OnAnswer()
		{
			PlayerState playerState = LatentPlayers.Get(knockInfo.PlayerId);
			BaseScript.TriggerServerEvent("gtacnr:garages:answer", new object[2] { knockInfo.PlayerId, knockInfo.PropertyId });
			Utils.DisplayHelpText("You answered " + playerState.ColorNameAndId + ".");
		}
	}

	private static OrderedHashSet<Garage> _ownedGarages = new OrderedHashSet<Garage>();

	private static Garage closestGarage;

	private static Tuple<string, int> currentGarage;

	private static GarageScript instance;

	private static List<Blip> blips = new List<Blip>();

	private static int propertyBlipsMode = 1;

	private static bool isEnteringOrExiting;

	private static bool canOpenMainMenu;

	private static bool canEnterWithVehicle;

	private static bool canOpenManageMenu;

	private static bool canManage;

	private static bool canExit;

	private static List<StoredVehicle> storedVehicles;

	private static Dictionary<int, Vehicle> parkedVehicles;

	private static Tuple<StoredVehicle, MenuItem> selectedVehicle;

	private KnockHandler knockHandler = new GarageKnockHandler();

	private bool instructionsShown;

	private bool antiFallTaskAttached;

	public static IEnumerable<Garage> OwnedGarages => _ownedGarages;

	public static Garage ClosestGarage => closestGarage;

	public static bool IsInGarage
	{
		get
		{
			int ownerServerId;
			return GetCurrentGarage(out ownerServerId) != null;
		}
	}

	public static bool DoesPlayerOwnGarage(Garage garage)
	{
		return _ownedGarages.Contains(garage);
	}

	public static int OwnedGaragesCount()
	{
		return _ownedGarages.Count();
	}

	public static Garage GetCurrentGarage(out int ownerServerId)
	{
		ownerServerId = 0;
		if (currentGarage == null)
		{
			return null;
		}
		ownerServerId = currentGarage.Item2;
		return Gtacnr.Data.Garages.GetGarageById(currentGarage.Item1);
	}

	private static void SetCurrentGarage(Garage garage, int ownerServerId)
	{
		currentGarage = Tuple.Create(garage.Id, ownerServerId);
	}

	private static void ResetCurrentGarage()
	{
		currentGarage = null;
	}

	public GarageScript()
	{
		instance = this;
		DeathScript.Respawning += OnRespawningOrArrested;
	}

	protected override async void OnStarted()
	{
		propertyBlipsMode = Preferences.PropertyBlipsMode.Get();
		await LoadOwnedGarages();
		CreateBlips();
	}

	public static void OnBuyGarage(Garage garage)
	{
		if (!_ownedGarages.Contains(garage))
		{
			_ownedGarages.Add(garage);
		}
	}

	public static async void EnterGarage(Garage garage, int ownerPlayerId, bool skipAuth = false)
	{
		if (GetCurrentGarage(out var _) != null || isEnteringOrExiting)
		{
			return;
		}
		try
		{
			if (CuffedScript.IsCuffed || SurrenderScript.IsSurrendered || Game.PlayerPed.IsBeingStunned || CuffedScript.IsBeingCuffedOrUncuffed || JewelryRobberyStateScript.IsPlayerRobbing)
			{
				Utils.PlayErrorSound();
				return;
			}
			isEnteringOrExiting = true;
			MenuController.CloseAllMenus();
			if (skipAuth)
			{
				Enter();
				return;
			}
			BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
			Utils.RemoveAllAttachedProps();
			EnterExitPropertyResponse enterExitPropertyResponse = (EnterExitPropertyResponse)(await instance.TriggerServerEventAsync<int>("gtacnr:garages:enter", new object[2] { garage.Id, ownerPlayerId }));
			switch (enterExitPropertyResponse)
			{
			case EnterExitPropertyResponse.Success:
				Enter();
				break;
			case EnterExitPropertyResponse.MissingPermission:
				Utils.DisplayHelpText("~r~You're not authorized to enter this garage.");
				break;
			case EnterExitPropertyResponse.OwnerOffline:
				Utils.DisplayHelpText("~r~The owner of this garage is offline.");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC8-{(int)enterExitPropertyResponse}"));
				break;
			}
		}
		catch (Exception exception)
		{
			instance.Print(exception);
		}
		finally
		{
			isEnteringOrExiting = false;
		}
		async void Enter()
		{
			if (ownerPlayerId == API.GetPlayerServerId(API.PlayerId()))
			{
				LoadGarageVehicles(garage);
			}
			API.DisplayRadar(false);
			await Utils.TeleportToCoords(garage.Interior.OnFootPosition, garage.Interior.OnFootHeading, Utils.TeleportFlags.VisualEffects);
			SetCurrentGarage(garage, ownerPlayerId);
			canManage = ownerPlayerId == API.GetPlayerServerId(API.PlayerId());
			instance.AttachAntiFallTask();
		}
	}

	public static async void ExitGarage()
	{
		int ownerServerId;
		Garage garage = GetCurrentGarage(out ownerServerId);
		if (garage == null || isEnteringOrExiting)
		{
			return;
		}
		isEnteringOrExiting = true;
		try
		{
			BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
			Utils.RemoveAllAttachedProps();
			EnterExitPropertyResponse enterExitPropertyResponse = (EnterExitPropertyResponse)(await instance.TriggerServerEventAsync<int>("gtacnr:garages:exit", new object[0]));
			if (enterExitPropertyResponse == EnterExitPropertyResponse.Success)
			{
				instance.DetachAntiFallTask();
				ResetCurrentGarage();
				canManage = false;
				await Utils.TeleportToCoords(garage.OnFootPosition, garage.OnFootHeading, Utils.TeleportFlags.VisualEffects);
				API.DisplayRadar(true);
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC9-{(int)enterExitPropertyResponse}"));
			}
		}
		catch (Exception exception)
		{
			instance.Print(exception);
		}
		isEnteringOrExiting = false;
	}

	public static async void EnterGarageWithVehicle(Garage garage, int ownerPlayerId)
	{
		if (GetCurrentGarage(out var _) != null || isEnteringOrExiting)
		{
			return;
		}
		isEnteringOrExiting = true;
		try
		{
			Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)vehicle == (Entity)null)
			{
				isEnteringOrExiting = false;
				return;
			}
			if (!API.IsEntityAMissionEntity(((PoolObject)vehicle).Handle))
			{
				API.SetEntityAsMissionEntity(((PoolObject)vehicle).Handle, true, true);
			}
			MenuController.CloseAllMenus();
			EnterExitPropertyWithVehicleData data = new EnterExitPropertyWithVehicleData
			{
				GarageId = garage.Id,
				GarageOwnerId = ownerPlayerId,
				Passengers = (from p in vehicle.Passengers
					where p.IsPlayer
					select ((IEnumerable<Player>)((BaseScript)instance).Players).FirstOrDefault((Player val) => (Entity)(object)val.Character == (Entity)(object)p).ServerId).ToList()
			};
			VehicleClass classType = vehicle.ClassType;
			if (!new HashSet<VehicleClass>
			{
				(VehicleClass)0,
				(VehicleClass)3,
				(VehicleClass)13,
				(VehicleClass)8,
				(VehicleClass)4,
				(VehicleClass)9,
				(VehicleClass)2,
				(VehicleClass)1,
				(VehicleClass)6,
				(VehicleClass)5,
				(VehicleClass)7,
				(VehicleClass)12
			}.Contains(classType))
			{
				Utils.DisplayHelpText("~r~You cannot store this vehicle in a garage.");
				isEnteringOrExiting = false;
				return;
			}
			await Utils.FadeOut(500);
			EnterExitPropertyWithVehicleData response = (await instance.TriggerServerEventAsync<string>("gtacnr:garages:enterWithVehicle", new object[1] { data.Json() })).Unjson<EnterExitPropertyWithVehicleData>();
			if (response.ResponseCode == EnterExitPropertyResponse.Success)
			{
				LoadGarageVehicles(garage);
				API.DisplayRadar(false);
				GarageParking garageParking = garage.Interior.ParkingSpaces.ElementAtOrDefault(response.ParkIndex);
				await Utils.TeleportToCoords(garageParking.Position, garageParking.Heading, Utils.TeleportFlags.TeleportVehicle | Utils.TeleportFlags.PlaceOnGround | Utils.TeleportFlags.VisualEffects, 1000);
				SetCurrentGarage(garage, ownerPlayerId);
				canManage = ownerPlayerId == API.GetPlayerServerId(API.PlayerId());
				instance.AttachAntiFallTask();
				vehicle = Game.PlayerPed.CurrentVehicle;
				if ((Entity)(object)vehicle != (Entity)null)
				{
					((Entity)vehicle).IsInvincible = true;
					Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)0);
					while ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
					{
						await BaseScript.Delay(0);
					}
					if (response.StoredVehicle.IsStolen)
					{
						Utils.DisplayHelpText("This ~r~stolen vehicle ~s~has been parked in your ~y~garage~s~.");
					}
					else if (response.StoredVehicle.GarageId == garage.Id)
					{
						Utils.DisplayHelpText("This ~b~personal vehicle ~s~has been returned to your ~y~garage~s~.");
					}
					else
					{
						Utils.DisplayHelpText("This ~b~personal vehicle ~s~has been moved to this ~y~garage~s~.");
					}
					vehicle.IsEngineRunning = false;
					if (ActiveVehicleScript.ActiveVehicleNetId == ((Entity)vehicle).NetworkId)
					{
						await ActiveVehicleScript.ResetActiveVehicle(clientOnly: true);
					}
				}
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.InvalidJob)
			{
				Utils.DisplayHelpText("~r~You cannot park vehicles in a garage on your current job.");
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.MissingPermission)
			{
				Utils.DisplayHelpText("~r~You're not authorized to enter this garage.");
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.VehicleNotOwned)
			{
				Utils.DisplayHelpText("~r~You cannot park someone else's personal vehicle in your garage.");
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.OwnerOffline)
			{
				Utils.DisplayHelpText("~r~The owner of this garage is offline.");
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.NoParkingSpace)
			{
				Utils.DisplayHelpText("~r~Your garage is full.");
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC8-{(int)response.ResponseCode}"));
			}
		}
		catch (Exception exception)
		{
			instance.Print(exception);
		}
		finally
		{
			Finally();
		}
		isEnteringOrExiting = false;
		static async void Finally()
		{
			if (API.IsScreenFadedOut())
			{
				await Utils.FadeIn(500);
			}
		}
	}

	public static async void ExitGarageWithVehicle()
	{
		int ownerServerId;
		Garage garage = GetCurrentGarage(out ownerServerId);
		if (garage == null || isEnteringOrExiting)
		{
			return;
		}
		isEnteringOrExiting = true;
		try
		{
			Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)vehicle == (Entity)null)
			{
				isEnteringOrExiting = false;
				return;
			}
			MenuController.CloseAllMenus();
			EnterExitPropertyWithVehicleData data = new EnterExitPropertyWithVehicleData
			{
				GarageId = garage.Id,
				Passengers = (from p in vehicle.Passengers
					where p.IsPlayer
					select ((IEnumerable<Player>)((BaseScript)instance).Players).FirstOrDefault((Player val) => (Entity)(object)val.Character == (Entity)(object)p).ServerId).ToList()
			};
			((Entity)vehicle).IsInvincible = false;
			await Utils.FadeOut(500);
			EnterExitPropertyWithVehicleData response = (await instance.TriggerServerEventAsync<string>("gtacnr:garages:exitWithVehicle", new object[1] { data.Json() })).Unjson<EnterExitPropertyWithVehicleData>();
			if (response.ResponseCode == EnterExitPropertyResponse.Success)
			{
				API.DisplayRadar(true);
				instance.DetachAntiFallTask();
				await Utils.TeleportToCoords(garage.VehiclePosition, garage.VehicleHeading, Utils.TeleportFlags.TeleportVehicle | Utils.TeleportFlags.PlaceOnGround | Utils.TeleportFlags.VisualEffects, 1000);
				if ((Entity)(object)vehicle != (Entity)null)
				{
					((Entity)vehicle).IsPositionFrozen = false;
					await ActiveVehicleScript.SetActiveVehicle(response.StoredVehicle);
				}
				ResetCurrentGarage();
				canManage = false;
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.InvalidJob)
			{
				Utils.DisplayHelpText("~r~You cannot take vehicles out of a garage on your current job.");
			}
			else if (response.ResponseCode == EnterExitPropertyResponse.InMaintenance)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_IN_MAINTENANCE));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC8-{(int)response.ResponseCode}"));
			}
			if (response.ResponseCode != EnterExitPropertyResponse.Success && (Entity)(object)vehicle != (Entity)null)
			{
				((Entity)vehicle).IsInvincible = true;
			}
		}
		catch (Exception exception)
		{
			instance.Print(exception);
		}
		finally
		{
			Finally();
		}
		isEnteringOrExiting = false;
		static async void Finally()
		{
			if (API.IsScreenFadedOut())
			{
				await Utils.FadeIn(500);
			}
		}
	}

	private static async void LoadGarageVehicles(Garage garage)
	{
		List<StoredVehicle> list = (await instance.TriggerServerEventAsync<string>("gtacnr:garages:getVehiclesToCreate", new object[1] { garage.Id })).Unjson<List<StoredVehicle>>();
		storedVehicles = new List<StoredVehicle>();
		parkedVehicles = new Dictionary<int, Vehicle>();
		DateTime startT = DateTime.UtcNow;
		foreach (StoredVehicle storedVehicle in list)
		{
			await BaseScript.Delay(100);
			try
			{
				GarageParking garageParking = garage.Interior.ParkingSpaces[storedVehicle.GarageParkIndex];
				Vehicle val = await Utils.CreateStoredVehicle(storedVehicle, garageParking.Position, garageParking.Heading);
				if ((Entity)(object)val != (Entity)null)
				{
					((Entity)val).IsInvincible = true;
					parkedVehicles.Add(storedVehicle.GarageParkIndex, val);
					storedVehicles.Add(storedVehicle);
				}
			}
			catch (Exception exception)
			{
				instance.Print("Unable to create stored vehicle " + storedVehicle.Id + " because an exception has occurred:");
				instance.Print(exception);
			}
			if (Gtacnr.Utils.CheckTimePassed(startT, TimeSpan.FromSeconds(15.0)) && GetCurrentGarage(out var _) != garage)
			{
				AntiEntitySpawnScript.RegisterEntities(parkedVehicles.Values.Cast<Entity>().ToList());
				return;
			}
		}
		if (storedVehicles.Count > 0)
		{
			BaseScript.TriggerServerEvent("gtacnr:vehicles:registerCreatedVehicles", new object[1] { storedVehicles.Json() });
			AntiEntitySpawnScript.RegisterEntities(parkedVehicles.Values.Cast<Entity>().ToList());
		}
	}

	[EventHandler("gtacnr:garages:passengerStartEnterWithVehicle")]
	private async void OnPassengerStartEnterWithVehicle(string garageId, int garageOwnerId)
	{
		await Utils.FadeOut(500);
		SetCurrentGarage(Gtacnr.Data.Garages.GetGarageById(garageId), garageOwnerId);
		API.DisplayRadar(false);
		canManage = false;
	}

	[EventHandler("gtacnr:garages:passengerEndEnterWithVehicle")]
	private async void OnPassengerEndEnterWithVehicle()
	{
		await BaseScript.Delay(2000);
		int ownerServerId;
		Garage garage = GetCurrentGarage(out ownerServerId);
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
		{
			await Utils.TeleportToCoords(garage.Interior.OnFootPosition, garage.Interior.OnFootHeading);
		}
		await Utils.FadeIn(500);
		AttachAntiFallTask();
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)0);
		}
	}

	[EventHandler("gtacnr:garages:passengerStartExitWithVehicle")]
	private async void OnPassengerStartExitWithVehicle()
	{
		DetachAntiFallTask();
		await Utils.FadeOut(500);
		canManage = false;
	}

	[EventHandler("gtacnr:garages:passengerEndExitWithVehicle")]
	private async void OnPassengerEndExitWithVehicle()
	{
		await BaseScript.Delay(2000);
		int ownerServerId;
		Garage garage = GetCurrentGarage(out ownerServerId);
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
		{
			await Utils.TeleportToCoords(garage.OnFootPosition, garage.OnFootHeading);
		}
		ResetCurrentGarage();
		API.DisplayRadar(true);
		await Utils.FadeIn(500);
	}

	[EventHandler("gtacnr:garages:gotKnockedAt")]
	private async void OnGarageGotKnockedAt(int playerId, string garageId)
	{
		Garage garage = Gtacnr.Data.Garages.GetGarageById(garageId);
		if (garage != null && DoesPlayerOwnGarage(garage))
		{
			while (knockHandler.IsKnockActive())
			{
				await BaseScript.Delay(0);
			}
			Utils.DisplayHelpText(LatentPlayers.Get(playerId).ColorNameAndId + " ~s~is knocking at ~y~" + garage.Name + "~s~.");
			KnockInfo newKnock = new KnockInfo
			{
				PlayerId = playerId,
				PropertyId = garageId
			};
			await knockHandler.Knock(newKnock);
		}
	}

	[EventHandler("gtacnr:garages:answered")]
	private void OnGotAnsweredToEnterGarage(int ownerId, string garageId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Garage garageById = Gtacnr.Data.Garages.GetGarageById(garageId);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(garageById.OnFootPosition) < 100f)
		{
			EnterGarage(garageById, ownerId);
		}
	}

	[EventHandler("gtacnr:propertyBlipsToggled")]
	private void OnPropertyBlipsToggled(int mode)
	{
		propertyBlipsMode = mode;
		RefreshBlips();
	}

	[EventHandler("gtacnr:police:onArrested")]
	private void OnArrested()
	{
		OnRespawningOrArrested(this, EventArgs.Empty);
	}

	private void OnRespawningOrArrested(object sender, EventArgs e)
	{
		if (GetCurrentGarage(out var _) != null)
		{
			ResetCurrentGarage();
			API.DisplayRadar(true);
			DetachAntiFallTask();
		}
	}

	public static void RefreshBlips()
	{
		instance.DeleteBlips();
		instance.CreateBlips();
	}

	private void CreateBlips()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		foreach (Garage allGarage in Gtacnr.Data.Garages.AllGarages)
		{
			bool flag = _ownedGarages.Contains(allGarage);
			switch (propertyBlipsMode)
			{
			case 0:
				return;
			case 2:
				if (!flag)
				{
					continue;
				}
				break;
			}
			Blip val = World.CreateBlip(allGarage.OnFootPosition);
			if (flag)
			{
				val.Sprite = (BlipSprite)357;
				val.Scale = 1f;
				Utils.SetBlipName(val, "Owned Garage", "owned_garage");
			}
			else
			{
				val.Sprite = (BlipSprite)369;
				val.Scale = 0.6f;
				Utils.SetBlipName(val, "Garage", "garage");
			}
			val.IsShortRange = true;
			blips.Add(val);
		}
	}

	private void DeleteBlips()
	{
		foreach (Blip blip in blips)
		{
			((PoolObject)blip).Delete();
		}
		blips.Clear();
	}

	private async Task LoadOwnedGarages()
	{
		while (!SpawnScript.HasSpawned)
		{
			await BaseScript.Delay(0);
		}
		foreach (string item in (await TriggerServerEventAsync<string>("gtacnr:garages:getAllOwned", new object[0])).Unjson<List<string>>())
		{
			Garage garageById = Gtacnr.Data.Garages.GetGarageById(item);
			if (garageById != null)
			{
				_ownedGarages.Add(garageById);
			}
		}
	}

	private void EnableInstructionalButtons(string label)
	{
		if (!instructionsShown)
		{
			instructionsShown = true;
			Utils.AddInstructionalButton("garage", new InstructionalButton(label, 2, (Control)51));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionsShown)
		{
			instructionsShown = false;
			Utils.RemoveInstructionalButton("garage");
		}
	}

	[Update]
	private async Coroutine FindClosestGarageTask()
	{
		closestGarage = null;
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		float num = 62500f;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		foreach (Garage allGarage in Gtacnr.Data.Garages.AllGarages)
		{
			float num2 = ((Vector3)(ref position)).DistanceToSquared(allGarage.OnFootPosition);
			if (num2 < num)
			{
				num = num2;
				closestGarage = allGarage;
			}
		}
		await Script.Wait(5000);
	}

	[Update]
	private async Coroutine EnableControlsTask()
	{
		if (currentGarage != null)
		{
			int ownerServerId;
			Garage garage = GetCurrentGarage(out ownerServerId);
			Vector3 val = garage.Interior.OnFootPosition;
			if (((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 1f)
			{
				EnableInstructionalButtons("Exit");
				canExit = true;
				return;
			}
			if (canManage)
			{
				val = garage.Interior.ManagePosition;
				if (((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 0.64000005f)
				{
					EnableInstructionalButtons("Manage");
					canOpenManageMenu = true;
					return;
				}
			}
			canOpenManageMenu = false;
			canExit = false;
		}
		else if (closestGarage != null)
		{
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed && DoesPlayerOwnGarage(closestGarage))
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref position)).DistanceToSquared(closestGarage.VehiclePosition) <= 9f)
				{
					EnableInstructionalButtons("Enter");
					canEnterWithVehicle = true;
					return;
				}
			}
			else
			{
				Vector3 position2 = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref position2)).DistanceToSquared(closestGarage.OnFootPosition) <= 1f)
				{
					EnableInstructionalButtons("Garage");
					canOpenMainMenu = true;
					return;
				}
			}
			canOpenMainMenu = false;
			canEnterWithVehicle = false;
		}
		DisableInstructionalButtons();
		await Script.Wait(100);
	}

	[Update]
	private async Coroutine DrawGarageTask()
	{
		if (currentGarage != null)
		{
			int ownerServerId;
			Garage garage = GetCurrentGarage(out ownerServerId);
			API.SetPlayerBlipPositionThisFrame(garage.OnFootPosition.X, garage.OnFootPosition.Y);
			Vector3 onFootPosition = garage.Interior.OnFootPosition;
			Vector3 size = default(Vector3);
			((Vector3)(ref size))._002Ector(1f, 1f, 0.75f);
			Color col = Color.FromUint(3137339520u);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(onFootPosition.X, onFootPosition.Y, onFootPosition.Z, ref z, false))
			{
				onFootPosition.Z = z;
			}
			DrawGarageMarker(onFootPosition, size, col);
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if (canManage)
			{
				onFootPosition = garage.Interior.ManagePosition;
				((Vector3)(ref size))._002Ector(0.5f, 0.5f, 0.4f);
				col = Color.FromUint(4125163648u);
				if (API.GetGroundZFor_3dCoord(onFootPosition.X, onFootPosition.Y, onFootPosition.Z, ref z, false))
				{
					onFootPosition.Z = z;
				}
				DrawGarageMarker(onFootPosition, size, col);
				if (!isEnteringOrExiting)
				{
					if ((Entity)(object)currentVehicle != (Entity)null && (Entity)(object)currentVehicle.Driver == (Entity)(object)Game.PlayerPed)
					{
						if (!currentVehicle.IsEngineRunning)
						{
							currentVehicle.IsEngineRunning = true;
						}
						if (Game.IsControlJustPressed(2, (Control)71) || Game.IsControlJustPressed(2, (Control)72))
						{
							((Entity)currentVehicle).IsPositionFrozen = true;
							ExitGarageWithVehicle();
						}
					}
					currentVehicle = Game.PlayerPed.LastVehicle;
					if ((Entity)(object)currentVehicle != (Entity)null && (Entity)(object)currentVehicle != (Entity)(object)Game.PlayerPed.CurrentVehicle && currentVehicle.IsEngineRunning)
					{
						currentVehicle.IsEngineRunning = false;
					}
				}
			}
			else if ((Entity)(object)currentVehicle != (Entity)null && (Entity)(object)currentVehicle.Driver == (Entity)(object)Game.PlayerPed && currentVehicle.IsEngineRunning)
			{
				currentVehicle.IsEngineRunning = false;
			}
			if (Game.IsControlJustPressed(2, (Control)51))
			{
				if (canExit)
				{
					MenuController.CloseAllMenus();
					ExitGarage();
				}
				else if (canManage && canOpenManageMenu)
				{
					ManageGarage(garage);
				}
			}
		}
		else
		{
			if (closestGarage == null)
			{
				return;
			}
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed && DoesPlayerOwnGarage(closestGarage))
			{
				Vector3 vehiclePosition = closestGarage.VehiclePosition;
				Vector3 size2 = default(Vector3);
				((Vector3)(ref size2))._002Ector(3f, 3f, 0.75f);
				Color col2 = Color.FromUint(3137339520u);
				float z2 = 0f;
				if (API.GetGroundZFor_3dCoord(vehiclePosition.X, vehiclePosition.Y, vehiclePosition.Z, ref z2, false))
				{
					vehiclePosition.Z = z2;
				}
				DrawGarageMarker(vehiclePosition, size2, col2);
				if (canEnterWithVehicle && Game.IsControlJustPressed(2, (Control)51))
				{
					EnterGarageWithVehicle(closestGarage, Game.Player.ServerId);
				}
			}
			else
			{
				Vector3 onFootPosition2 = closestGarage.OnFootPosition;
				Vector3 size3 = default(Vector3);
				((Vector3)(ref size3))._002Ector(1f, 1f, 0.75f);
				Color col3 = Color.FromUint(3137339520u);
				float z3 = 0f;
				if (API.GetGroundZFor_3dCoord(onFootPosition2.X, onFootPosition2.Y, onFootPosition2.Z, ref z3, false))
				{
					onFootPosition2.Z = z3;
				}
				DrawGarageMarker(onFootPosition2, size3, col3);
				if (canOpenMainMenu && Game.IsControlJustPressed(2, (Control)51))
				{
					GarageMenuScript.OpenMenu();
				}
			}
		}
		static void DrawGarageMarker(Vector3 pos, Vector3 val, Color color)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			API.DrawMarker(1, pos.X, pos.Y, pos.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	private async Coroutine AntiFallTask()
	{
		await Script.Wait(1000);
		if (currentGarage != null && antiFallTaskAttached)
		{
			int ownerServerId;
			GarageInterior interior = GetCurrentGarage(out ownerServerId).Interior;
			Vector3 onFootPosition = interior.OnFootPosition;
			if (((Vector3)(ref onFootPosition)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position) > 2500f)
			{
				await Utils.TeleportToCoords(interior.OnFootPosition, interior.OnFootHeading, Utils.TeleportFlags.VisualEffects);
				Utils.DisplayHelpText("~r~You fell out of the property interior, so you were teleported back in.");
			}
		}
	}

	private void AttachAntiFallTask()
	{
		if (!antiFallTaskAttached)
		{
			antiFallTaskAttached = true;
			base.Update += AntiFallTask;
		}
	}

	private void DetachAntiFallTask()
	{
		if (antiFallTaskAttached)
		{
			antiFallTaskAttached = false;
			base.Update -= AntiFallTask;
		}
	}

	private static void ManageGarage(Garage garage)
	{
		Menu menu = new Menu("Garage", "Manage your vehicles");
		menu.OnItemSelect += ManageGarageOnItemSelect;
		Dictionary<int, StoredVehicle> dictionary = storedVehicles.ToDictionary((StoredVehicle v) => v.GarageParkIndex, (StoredVehicle v) => v);
		foreach (int item in Enumerable.Range(0, garage.Interior.ParkingSpaces.Count))
		{
			MenuItem menuItem;
			if (dictionary.TryGetValue(item, out var value))
			{
				menuItem = value.ToMenuItem();
				menuItem.ItemData = value;
			}
			else
			{
				menuItem = new MenuItem($"Empty #{item + 1}");
				menuItem.ItemData = item;
			}
			menu.AddMenuItem(menuItem);
		}
		menu.OpenMenu();
	}

	private static async void ManageGarageOnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is StoredVehicle storedVehicle)
		{
			if (selectedVehicle == null)
			{
				selectedVehicle = new Tuple<StoredVehicle, MenuItem>(storedVehicle, menuItem);
				menuItem.RightIcon = MenuItem.Icon.CAR;
			}
			else if (selectedVehicle.Item1 != storedVehicle)
			{
				if (await instance.SwapVehicles(storedVehicle, selectedVehicle.Item1))
				{
					menu.SwapMenuItems(menuItem, selectedVehicle.Item2);
				}
				selectedVehicle.Item2.RightIcon = MenuItem.Icon.NONE;
				selectedVehicle = null;
			}
		}
		else if (menuItem.ItemData is int newParkingSpot && selectedVehicle != null)
		{
			int oldParkingSpot = selectedVehicle.Item1.GarageParkIndex;
			if (await instance.SetVehicleParkingSpot(selectedVehicle.Item1, newParkingSpot))
			{
				menu.SwapMenuItems(menuItem, selectedVehicle.Item2);
				menuItem.Text = $"Empty #{oldParkingSpot + 1}";
				menuItem.ItemData = oldParkingSpot;
			}
			selectedVehicle.Item2.RightIcon = MenuItem.Icon.NONE;
			selectedVehicle = null;
		}
	}

	private async Task<bool> SwapVehicles(StoredVehicle storedVehicleA, StoredVehicle storedVehicleB)
	{
		if (!HandleGarageChangeSlotResponse((GarageChangeSlotResponse)(await TriggerServerEventAsync<byte>("gtacnr:garages:swapVehicles", new object[2] { storedVehicleA.Id, storedVehicleB.Id }))))
		{
			return false;
		}
		int ownerServerId;
		Garage garage = GetCurrentGarage(out ownerServerId);
		parkedVehicles.TryGetValue(storedVehicleA.GarageParkIndex, out Vehicle value);
		parkedVehicles.TryGetValue(storedVehicleB.GarageParkIndex, out Vehicle value2);
		if ((Entity)(object)value == (Entity)null || (Entity)(object)value2 == (Entity)null)
		{
			return false;
		}
		GarageParking garageParking = garage.Interior.ParkingSpaces[storedVehicleA.GarageParkIndex];
		GarageParking garageParking2 = garage.Interior.ParkingSpaces[storedVehicleB.GarageParkIndex];
		int garageParkIndex = storedVehicleA.GarageParkIndex;
		storedVehicleA.GarageParkIndex = storedVehicleB.GarageParkIndex;
		storedVehicleB.GarageParkIndex = garageParkIndex;
		((Entity)value).Position = garageParking2.Position;
		((Entity)value).Heading = garageParking2.Heading;
		((Entity)value2).Position = garageParking.Position;
		((Entity)value2).Heading = garageParking.Heading;
		parkedVehicles[storedVehicleA.GarageParkIndex] = value;
		parkedVehicles[storedVehicleB.GarageParkIndex] = value2;
		return true;
	}

	private async Task<bool> SetVehicleParkingSpot(StoredVehicle storedVehicle, int newParkingSpot)
	{
		if (!HandleGarageChangeSlotResponse((GarageChangeSlotResponse)(await TriggerServerEventAsync<byte>("gtacnr:garages:changeVehicleParkingSpot", new object[2] { storedVehicle.Id, newParkingSpot }))))
		{
			return false;
		}
		int ownerServerId;
		Garage garage = GetCurrentGarage(out ownerServerId);
		parkedVehicles.TryGetValue(storedVehicle.GarageParkIndex, out Vehicle value);
		if ((Entity)(object)value == (Entity)null)
		{
			return false;
		}
		parkedVehicles.Remove(storedVehicle.GarageParkIndex);
		GarageParking garageParking = garage.Interior.ParkingSpaces[newParkingSpot];
		storedVehicle.GarageParkIndex = newParkingSpot;
		((Entity)value).Position = garageParking.Position;
		((Entity)value).Heading = garageParking.Heading;
		parkedVehicles.Add(newParkingSpot, value);
		return true;
	}

	private static bool HandleGarageChangeSlotResponse(GarageChangeSlotResponse response)
	{
		if (response == GarageChangeSlotResponse.Success)
		{
			return true;
		}
		Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xD0-{(int)response}"));
		return false;
	}
}
