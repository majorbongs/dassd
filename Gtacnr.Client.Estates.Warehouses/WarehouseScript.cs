using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Crimes.Robberies.Jewelry;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Robberies.Jewelry;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Estates.Warehouses;

public class WarehouseScript : Script
{
	private class WarehouseKnockHandler : KnockHandler
	{
		protected override void OnAnswer()
		{
			PlayerState playerState = LatentPlayers.Get(knockInfo.PlayerId);
			BaseScript.TriggerServerEvent("gtacnr:warehouses:answer", new object[2] { knockInfo.PlayerId, knockInfo.PropertyId });
			Utils.DisplayHelpText("You answered " + playerState.ColorNameAndId + ".");
		}
	}

	public static Warehouse ClosestWarehouse;

	public static Dictionary<string, WarehouseInterior> WarehouseInteriors = Gtacnr.Utils.LoadJson<List<WarehouseInterior>>("data/estates/warehouses/warehouseInteriors.json").ToDictionary((WarehouseInterior w) => w.Id, (WarehouseInterior w) => w);

	public static Dictionary<string, Warehouse> Warehouses = Gtacnr.Utils.LoadJson<List<Warehouse>>("data/estates/warehouses/warehouses.json").ToDictionary((Warehouse w) => w.Id, (Warehouse w) => w);

	public static List<string> OwnedWarehouseIds = new List<string>();

	public static Tuple<string, int> CurrentWarehouse;

	private static int propertyBlipsMode = 1;

	private static bool canOpenMainMenu = false;

	private static bool canExit = false;

	private static bool canOpenManageMenu = false;

	private static bool canManage = false;

	private static List<Blip> warehouseBlips = new List<Blip>();

	private static bool isEnteringOrExiting;

	private WarehouseKnockHandler knockHandler = new WarehouseKnockHandler();

	private bool antiFallTaskAttached;

	private bool warehouseKeysEnabled;

	private static int lastVehicleNetId = -1;

	public static bool IsInWarehouse => CurrentWarehouse != null;

	public static WarehouseScript Instance { get; private set; }

	public static IEnumerable<Warehouse> OwnedWarehouses => OwnedWarehouseIds.Select((string wId) => Warehouses[wId]);

	public static event EventHandler<WarehouseEventArgs> Entered;

	public static event EventHandler<WarehouseEventArgs> Exited;

	public static Warehouse GetCurrentWarehouse(out int ownerServerId)
	{
		ownerServerId = 0;
		if (CurrentWarehouse == null)
		{
			return null;
		}
		ownerServerId = CurrentWarehouse.Item2;
		return Warehouses[CurrentWarehouse.Item1];
	}

	public WarehouseScript()
	{
		Instance = this;
		DeathScript.Respawning += OnRespawningOrArrested;
	}

	public static Warehouse GetWarehouse(string warehouseId)
	{
		if (Warehouses.TryGetValue(warehouseId, out Warehouse value))
		{
			return value;
		}
		return null;
	}

	public static WarehouseInterior GetWarehouseInterior(string warehouseInteriorId)
	{
		if (WarehouseInteriors.TryGetValue(warehouseInteriorId, out WarehouseInterior value))
		{
			return value;
		}
		return null;
	}

	protected override async void OnStarted()
	{
		propertyBlipsMode = Preferences.PropertyBlipsMode.Get();
		await LoadOwnedWarehouses();
	}

	public static void RefreshBlips()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		foreach (Blip warehouseBlip in warehouseBlips)
		{
			((PoolObject)warehouseBlip).Delete();
		}
		warehouseBlips.Clear();
		foreach (Warehouse value in Warehouses.Values)
		{
			bool flag = OwnedWarehouseIds.Contains(value.Id);
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
			Blip val = World.CreateBlip(value.OnFootPosition);
			if (flag)
			{
				val.Sprite = (BlipSprite)473;
				val.Scale = 1f;
				Utils.SetBlipName(val, "Owned Warehouse", "owned_warehouse");
			}
			else
			{
				val.Sprite = (BlipSprite)474;
				val.Scale = 0.6f;
				Utils.SetBlipName(val, "Warehouse", "warehouse");
			}
			val.IsShortRange = true;
			warehouseBlips.Add(val);
		}
	}

	private async Task LoadOwnedWarehouses()
	{
		while (!SpawnScript.HasSpawned)
		{
			await BaseScript.Delay(0);
		}
		string text = await TriggerServerEventAsync<string>("gtacnr:warehouses:getAllOwned", new object[0]);
		if (string.IsNullOrWhiteSpace(text))
		{
			await BaseScript.Delay(10000);
			await LoadOwnedWarehouses();
		}
		else
		{
			OwnedWarehouseIds = text.Unjson<List<string>>();
			RefreshBlips();
		}
	}

	[Update]
	private async Coroutine FindClosestWarehouseTask()
	{
		ClosestWarehouse = null;
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 10000f;
		foreach (Warehouse value in Warehouses.Values)
		{
			Vector3 onFootPosition = value.OnFootPosition;
			float num2 = ((Vector3)(ref onFootPosition)).DistanceToSquared(position);
			if (num2 < num)
			{
				num = num2;
				ClosestWarehouse = value;
			}
		}
		await Script.Wait(5000);
	}

	[Update]
	private async Coroutine DrawTask()
	{
		if (CurrentWarehouse == null)
		{
			if (ClosestWarehouse != null)
			{
				Vector3 onFootPosition = ClosestWarehouse.OnFootPosition;
				Vector3 val = default(Vector3);
				((Vector3)(ref val))._002Ector(1f, 1f, 0.75f);
				Color color = Color.FromUint(3137339520u);
				float z = 0f;
				if (API.GetGroundZFor_3dCoord(onFootPosition.X, onFootPosition.Y, onFootPosition.Z, ref z, false))
				{
					onFootPosition.Z = z;
				}
				API.DrawMarker(1, onFootPosition.X, onFootPosition.Y, onFootPosition.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
			}
			return;
		}
		Warehouse warehouse = Warehouses[CurrentWarehouse.Item1];
		WarehouseInterior warehouseInterior = WarehouseInteriors[warehouse.InteriorId];
		API.SetPlayerBlipPositionThisFrame(warehouse.OnFootPosition.X, warehouse.OnFootPosition.Y);
		Vector3 onFootPosition2 = warehouseInterior.OnFootPosition;
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(1f, 1f, 0.75f);
		Color color2 = Color.FromUint(3137339520u);
		float z2 = 0f;
		if (API.GetGroundZFor_3dCoord(onFootPosition2.X, onFootPosition2.Y, onFootPosition2.Z, ref z2, false))
		{
			onFootPosition2.Z = z2;
		}
		API.DrawMarker(1, onFootPosition2.X, onFootPosition2.Y, onFootPosition2.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color2.R, (int)color2.G, (int)color2.B, (int)color2.A, false, true, 2, false, (string)null, (string)null, false);
		if (canManage)
		{
			onFootPosition2 = warehouseInterior.ManagePosition;
			((Vector3)(ref val2))._002Ector(0.5f, 0.5f, 0.4f);
			color2 = Color.FromUint(4125163648u);
			if (API.GetGroundZFor_3dCoord(onFootPosition2.X, onFootPosition2.Y, onFootPosition2.Z, ref z2, false))
			{
				onFootPosition2.Z = z2;
			}
			API.DrawMarker(1, onFootPosition2.X, onFootPosition2.Y, onFootPosition2.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color2.R, (int)color2.G, (int)color2.B, (int)color2.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	[Update]
	private async Coroutine UpdateTask()
	{
		await Script.Wait(100);
		Vector3 val;
		if (CurrentWarehouse == null)
		{
			if (ClosestWarehouse != null)
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				val = ClosestWarehouse.OnFootPosition;
				if (((Vector3)(ref val)).DistanceToSquared(position) <= 1f)
				{
					EnableWarehouseKeys("Warehouse");
					canOpenMainMenu = true;
					return;
				}
				canOpenMainMenu = false;
			}
		}
		else
		{
			Warehouse warehouse = Warehouses[CurrentWarehouse.Item1];
			WarehouseInterior warehouseInterior = WarehouseInteriors[warehouse.InteriorId];
			val = warehouseInterior.OnFootPosition;
			if (((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 1f)
			{
				EnableWarehouseKeys("Exit");
				canExit = true;
				return;
			}
			val = warehouseInterior.ManagePosition;
			if (((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 0.64000005f)
			{
				EnableWarehouseKeys("Manage");
				canOpenManageMenu = true;
				return;
			}
			canOpenManageMenu = false;
			canExit = false;
		}
		DisableWarehouseKeys();
	}

	private async Coroutine AntiFallTask()
	{
		await Script.Wait(1000);
		if (CurrentWarehouse != null)
		{
			Warehouse warehouse = Warehouses[CurrentWarehouse.Item1];
			WarehouseInterior warehouseInterior = WarehouseInteriors[warehouse.InteriorId];
			Vector3 onFootPosition = warehouseInterior.OnFootPosition;
			if (((Vector3)(ref onFootPosition)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position) > 2500f)
			{
				await Utils.TeleportToCoords(warehouseInterior.OnFootPosition, warehouseInterior.OnFootHeading, Utils.TeleportFlags.VisualEffects);
				Utils.DisplayHelpText("You fell out of the property interior, so you were teleported back in.");
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

	private void EnableWarehouseKeys(string label)
	{
		if (!warehouseKeysEnabled)
		{
			warehouseKeysEnabled = true;
			Utils.AddInstructionalButton("warehouse", new InstructionalButton(label, 2, (Control)51));
			KeysScript.AttachListener((Control)51, OnInteractKeyEvent, 50);
		}
	}

	private void DisableWarehouseKeys()
	{
		if (warehouseKeysEnabled)
		{
			warehouseKeysEnabled = false;
			Utils.RemoveInstructionalButton("warehouse");
			KeysScript.DetachListener((Control)51, OnInteractKeyEvent);
		}
	}

	private bool OnInteractKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed)
		{
			if (canOpenMainMenu)
			{
				WarehouseMenuScript.ShowMenu();
			}
			else if (canExit)
			{
				MenuController.CloseAllMenus();
				ExitWarehouse();
			}
			else if (canOpenManageMenu && canManage)
			{
				WarehouseInsideMenuScript.OpenMenu();
			}
		}
		return false;
	}

	public static async void EnterWarehouse(Warehouse warehouse, int ownerPlayerId, bool skipAuth = false)
	{
		if (CurrentWarehouse != null || isEnteringOrExiting)
		{
			return;
		}
		try
		{
			isEnteringOrExiting = true;
			if (CuffedScript.IsCuffed || SurrenderScript.IsSurrendered || Game.PlayerPed.IsBeingStunned || CuffedScript.IsBeingCuffedOrUncuffed)
			{
				Utils.PlayErrorSound();
				return;
			}
			if (JewelryRobberyStateScript.IsPlayerRobbing && JewelryRobberyStateScript.Instance.Player.Phase != RobberyPhase.GoingToHideout)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification("You are still in the ~o~orange area~s~. Leave it before entering a ~y~hideout~s~.");
				return;
			}
			Vehicle lastVehicle = Game.PlayerPed.LastVehicle;
			if ((Entity)(object)lastVehicle != (Entity)null)
			{
				lastVehicleNetId = ((Entity)lastVehicle).NetworkId;
				API.SetEntityAsMissionEntity(((PoolObject)lastVehicle).Handle, true, true);
				API.SetNetworkIdExistsOnAllMachines(lastVehicleNetId, true);
				API.SetNetworkIdCanMigrate(lastVehicleNetId, true);
			}
			MenuController.CloseAllMenus();
			if (skipAuth)
			{
				await Enter();
				return;
			}
			BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
			Utils.RemoveAllAttachedProps();
			EnterExitPropertyResponse enterExitPropertyResponse = (EnterExitPropertyResponse)(await Instance.TriggerServerEventAsync<int>("gtacnr:warehouses:enter", new object[2] { warehouse.Id, ownerPlayerId }));
			switch (enterExitPropertyResponse)
			{
			case EnterExitPropertyResponse.Success:
				await Enter();
				break;
			case EnterExitPropertyResponse.MissingPermission:
				Utils.DisplayHelpText("~r~You're not authorized to enter this warehouse.");
				break;
			case EnterExitPropertyResponse.OwnerOffline:
				Utils.DisplayHelpText("~r~The owner of this warehouse is offline.");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC8-{(int)enterExitPropertyResponse}"));
				break;
			}
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
		}
		finally
		{
			isEnteringOrExiting = false;
		}
		async Task Enter()
		{
			WarehouseInterior warehouseInterior = WarehouseInteriors[warehouse.InteriorId];
			API.DisplayRadar(false);
			await Utils.TeleportToCoords(warehouseInterior.OnFootPosition, warehouseInterior.OnFootHeading, Utils.TeleportFlags.VisualEffects);
			CurrentWarehouse = new Tuple<string, int>(warehouse.Id, ownerPlayerId);
			WarehouseScript.Entered?.Invoke(Instance, new WarehouseEventArgs(warehouse.Id, ownerPlayerId));
			canManage = ownerPlayerId == API.GetPlayerServerId(API.PlayerId());
			Instance.AttachAntiFallTask();
		}
	}

	public static async void ExitWarehouse()
	{
		if (CurrentWarehouse == null || isEnteringOrExiting)
		{
			return;
		}
		isEnteringOrExiting = true;
		try
		{
			BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
			Utils.RemoveAllAttachedProps();
			EnterExitPropertyResponse enterExitPropertyResponse = (EnterExitPropertyResponse)(await Instance.TriggerServerEventAsync<int>("gtacnr:warehouses:exit", new object[0]));
			if (enterExitPropertyResponse == EnterExitPropertyResponse.Success || enterExitPropertyResponse == EnterExitPropertyResponse.AlreadyInOrOut)
			{
				Warehouse warehouse = Warehouses[CurrentWarehouse.Item1];
				int ownerPlayerId = CurrentWarehouse.Item2;
				CurrentWarehouse = null;
				Instance.DetachAntiFallTask();
				await Utils.TeleportToCoords(warehouse.OnFootPosition, warehouse.OnFootHeading, Utils.TeleportFlags.VisualEffects);
				API.DisplayRadar(true);
				if (lastVehicleNetId != -1)
				{
					Vehicle val = null;
					if (API.NetworkDoesEntityExistWithNetworkId(lastVehicleNetId))
					{
						Entity obj = Entity.FromNetworkId(lastVehicleNetId);
						val = (Vehicle)(object)((obj is Vehicle) ? obj : null);
					}
					if ((Entity)(object)val != (Entity)null)
					{
						API.SetEntityAsMissionEntity(((PoolObject)Game.PlayerPed.LastVehicle).Handle, false, false);
					}
					lastVehicleNetId = -1;
				}
				WarehouseScript.Exited?.Invoke(Instance, new WarehouseEventArgs(warehouse.Id, ownerPlayerId));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC9-{(int)enterExitPropertyResponse}"));
			}
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
		}
		isEnteringOrExiting = false;
	}

	public static string GetWarehouseSize(Warehouse warehouse)
	{
		if (!WarehouseInteriors.ContainsKey(warehouse.InteriorId))
		{
			return "~r~ERROR~s~";
		}
		WarehouseInterior warehouseInterior = WarehouseInteriors[warehouse.InteriorId];
		if (warehouseInterior.Capacity >= 500000f)
		{
			return "Large";
		}
		if (warehouseInterior.Capacity >= 200000f)
		{
			return "Medium";
		}
		return "Small";
	}

	[EventHandler("gtacnr:warehouses:gotKnockedAt")]
	private async void OnWarehouseGotKnockedAt(int playerId, string warehouseId)
	{
		if (OwnedWarehouseIds.Contains(warehouseId))
		{
			while (knockHandler.IsKnockActive())
			{
				await BaseScript.Delay(0);
			}
			PlayerState? playerState = LatentPlayers.Get(playerId);
			Utils.DisplayHelpText(string.Concat(str2: GetWarehouse(warehouseId).Name, str0: playerState.ColorNameAndId, str1: " ~s~is knocking at ~y~", str3: "~s~."));
			KnockInfo newKnock = new KnockInfo
			{
				PlayerId = playerId,
				PropertyId = warehouseId
			};
			await knockHandler.Knock(newKnock);
		}
	}

	[EventHandler("gtacnr:warehouses:answered")]
	private void OnGotAnsweredToEnterWarehouse(int ownerId, string warehouseId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Warehouse warehouse = GetWarehouse(warehouseId);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(warehouse.OnFootPosition) < 100f)
		{
			EnterWarehouse(warehouse, ownerId);
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
		CurrentWarehouse = null;
		DetachAntiFallTask();
		API.DisplayRadar(true);
	}
}
