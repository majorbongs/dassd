using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Items;

public class VehicleToolsScript : Script
{
	private bool tasksAttached;

	private Vehicle targetVehicle;

	private Vehicle actualTargetVehicle;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Prop powerSawProp;

	private bool powerSawKeysAttached;

	private Menu powerSawMenu;

	private Dictionary<string, VehicleDoorIndex> doorBonesIndices = new Dictionary<string, VehicleDoorIndex>
	{
		["door_dside_f"] = (VehicleDoorIndex)0,
		["door_dside_r"] = (VehicleDoorIndex)2,
		["door_pside_f"] = (VehicleDoorIndex)1,
		["door_pside_r"] = (VehicleDoorIndex)3
	};

	public static bool IsUsingAnyVehicleTool => IsUsingPowerSaw;

	public static bool IsUsingPowerSaw { get; private set; }

	protected override void OnStarted()
	{
		CreatePowerSawMenu();
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null || !(itemId == "power_saw"))
		{
			return;
		}
		if (IsUsingPowerSaw)
		{
			Utils.SendNotification("You are already using a ~r~" + itemDefinition.Name + "~s~.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (amount != 1f)
		{
			Utils.SendNotification("You can only use one ~r~" + itemDefinition.Name + " ~s~at a time.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (Game.PlayerPed.IsInVehicle())
		{
			Utils.SendNotification("You cannot use a ~r~" + itemDefinition.Name + " ~s~when you're in a vehicle.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (InventoryMenuScript.Cache != null)
		{
			InventoryEntry inventoryEntry = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == "power_saw");
			if (inventoryEntry == null || inventoryEntry.Amount < 1f)
			{
				Utils.SendNotification("You don't have a ~r~" + itemDefinition.Name + "~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
		}
		if (CuffedScript.IsCuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_USE_WHEN_CUFFED, itemDefinition.Name));
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
		else
		{
			GivePowerSaw();
			API.CancelEvent();
		}
	}

	private async void GivePowerSaw()
	{
		if (!IsUsingPowerSaw)
		{
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			IsUsingPowerSaw = true;
			AttachTasks();
			powerSawProp = await World.CreateProp(Model.op_Implicit(API.GetHashKey(Gtacnr.Data.Items.GetItemDefinition("power_saw").Model)), ((Entity)Game.PlayerPed).Position, true, false);
			Prop obj = powerSawProp;
			int pedBoneIndex = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422);
			API.AttachEntityToEntity(((PoolObject)obj).Handle, ((PoolObject)Game.PlayerPed).Handle, pedBoneIndex, 0.42f, 0.1f, 0f, 45f, 0f, 180f, true, true, false, true, 1, true);
		}
	}

	private void RemovePowerSaw()
	{
		if (IsUsingPowerSaw)
		{
			IsUsingPowerSaw = false;
			DetachTasks();
			if (!((Entity)(object)powerSawProp == (Entity)null))
			{
				((Entity)powerSawProp).Detach();
				((PoolObject)powerSawProp).Delete();
			}
		}
	}

	private void AttachTasks()
	{
		if (IsUsingAnyVehicleTool && !tasksAttached)
		{
			tasksAttached = true;
			base.Update += UpdateTargetVehicleTask;
			base.Update += VehicleToolsTask;
		}
	}

	private void DetachTasks()
	{
		if (!IsUsingAnyVehicleTool && tasksAttached)
		{
			tasksAttached = false;
			base.Update -= UpdateTargetVehicleTask;
			base.Update -= VehicleToolsTask;
			DetachPowerSawKeys();
		}
	}

	private async Coroutine UpdateTargetVehicleTask()
	{
		await BaseScript.Delay(1000);
		float num = 25f;
		targetVehicle = null;
		Vehicle[] allVehicles = World.GetAllVehicles();
		foreach (Vehicle val in allVehicles)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)val).Position);
			if (num2 < num)
			{
				num = num2;
				targetVehicle = val;
			}
		}
	}

	private async Coroutine VehicleToolsTask()
	{
		await BaseScript.Delay(100);
		if (!IsUsingPowerSaw)
		{
			return;
		}
		if ((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261 || Game.PlayerPed.IsInMeleeCombat || CuffedScript.IsCuffed)
		{
			RemovePowerSaw();
		}
		else if ((Entity)(object)targetVehicle != (Entity)null)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)targetVehicle).Position) < 6.25f)
			{
				AttachPowerSawKeys();
			}
			else
			{
				DetachPowerSawKeys();
			}
		}
		else
		{
			DetachPowerSawKeys();
		}
	}

	private void AttachPowerSawKeys()
	{
		if (!powerSawKeysAttached)
		{
			powerSawKeysAttached = true;
			Utils.AddInstructionalButton("usePowerSaw", new InstructionalButton("Power Saw", 2, (Control)51));
			KeysScript.AttachListener((Control)51, OnPowerSawKeyEvent, 50);
		}
	}

	private void DetachPowerSawKeys()
	{
		if (powerSawKeysAttached)
		{
			powerSawKeysAttached = false;
			Utils.RemoveInstructionalButton("usePowerSaw");
			KeysScript.DetachListener((Control)51, OnPowerSawKeyEvent);
		}
	}

	private bool OnPowerSawKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (!powerSawKeysAttached)
		{
			return false;
		}
		if (eventType == KeyEventType.JustPressed)
		{
			OpenPowerSawMenu();
			return true;
		}
		return false;
	}

	private void CreatePowerSawMenu()
	{
		powerSawMenu = new Menu("Power Saw")
		{
			MaxDistance = 2f
		};
		powerSawMenu.OnItemSelect += OnPowerSawMenuItemSelect;
		MenuController.AddMenu(powerSawMenu);
	}

	private void OpenPowerSawMenu()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		actualTargetVehicle = targetVehicle;
		MenuController.CloseAllMenus();
		powerSawMenu.ClearMenuItems();
		powerSawMenu.MenuSubtitle = "Choose how to use your power saw";
		_ = Gtacnr.Client.API.Jobs.CachedJob;
		bool enabled = false;
		foreach (VehicleDoorIndex value in doorBonesIndices.Values)
		{
			if (actualTargetVehicle.Doors.HasDoor(value) && !actualTargetVehicle.Doors[value].IsBroken)
			{
				enabled = true;
			}
		}
		Menu menu = powerSawMenu;
		MenuItem item = (menuItems["cutDoor"] = new MenuItem("Door", "Cut off the ~y~door ~s~closest to you.")
		{
			Enabled = enabled
		});
		menu.AddMenuItem(item);
		powerSawMenu.OpenMenu();
	}

	private void OnPowerSawMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (IsSelected("cutDoor"))
		{
			menu.CloseMenu();
			CutDoor();
		}
		else if (IsSelected("cutCat"))
		{
			menu.CloseMenu();
			CutCatalyticConverter();
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItems[key] == menuItem;
			}
			return false;
		}
	}

	private async void CutDoor()
	{
		if ((Entity)(object)targetVehicle != (Entity)(object)actualTargetVehicle)
		{
			Utils.DisplayHelpText("The ~r~vehicle ~s~is no longer in range!", playSound: false);
			Utils.PlayErrorSound();
			return;
		}
		string animLib = "amb@world_human_stand_fishing@idle_a";
		string animName = "idle_a";
		AudioScript.PlayAudio("powersaw.wav", 0.5f);
		await Utils.LoadAnimDictionary(animLib);
		Game.PlayerPed.Task.PlayAnimation(animLib, animName, 4f, -1, (AnimationFlags)1);
		string ptfxLibrary = "scr_oddjobtowtruck";
		string ptfxName = "scr_ojtt_train_sparks";
		DateTime t = DateTime.UtcNow;
		API.RequestNamedPtfxAsset(ptfxLibrary);
		while (!API.HasNamedPtfxAssetLoaded(ptfxLibrary) && !Gtacnr.Utils.CheckTimePassed(t, 5000.0))
		{
			await BaseScript.Delay(10);
		}
		API.UseParticleFxAssetNextCall(ptfxLibrary);
		int ptfxHandle = API.StartNetworkedParticleFxLoopedOnEntity(ptfxName, ((PoolObject)powerSawProp).Handle, -0.15f, 0f, 0f, 0f, 0f, 0f, 2f, false, false, false);
		await BaseScript.Delay(2000);
		AudioScript.StopAudio();
		AudioScript.PlayAudio("powersaw_end.wav", 0.5f);
		API.StopParticleFxLooped(ptfxHandle, false);
		API.RemoveNamedPtfxAsset(ptfxLibrary);
		RemovePowerSaw();
		Game.PlayerPed.Task.ClearAnimation(animLib, animName);
		float num = 25f;
		VehicleDoorIndex val = (VehicleDoorIndex)(-1);
		foreach (KeyValuePair<string, VehicleDoorIndex> doorBonesIndex in doorBonesIndices)
		{
			string key = doorBonesIndex.Key;
			int entityBoneIndexByName = API.GetEntityBoneIndexByName(((PoolObject)actualTargetVehicle).Handle, key);
			Vector3 worldPositionOfEntityBone = API.GetWorldPositionOfEntityBone(((PoolObject)actualTargetVehicle).Handle, entityBoneIndexByName);
			float num2 = ((Vector3)(ref worldPositionOfEntityBone)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
			if (actualTargetVehicle.Doors.HasDoor(doorBonesIndex.Value) && !actualTargetVehicle.Doors[doorBonesIndex.Value].IsBroken && num2 < num)
			{
				val = doorBonesIndex.Value;
				num = num2;
			}
		}
		if ((int)val == -1)
		{
			Utils.DisplayHelpText("The ~r~vehicle ~s~has no doors to cut off!", playSound: false);
			Utils.PlayErrorSound();
			return;
		}
		int num3 = await TriggerServerEventAsync<int>("gtacnr:powersaw:cutOffDoor", new object[2]
		{
			((Entity)actualTargetVehicle).NetworkId,
			(int)val
		});
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition("power_saw");
		switch (num3)
		{
		case 1:
			Utils.DisplayHelpText("You ~y~sawed off ~s~a door from this vehicle.");
			break;
		case 10:
			Utils.DisplayHelpText("You ~y~sawed off ~s~a door from this vehicle. Your ~r~" + itemDefinition.Name + " ~s~broke.");
			break;
		default:
			Utils.DisplayErrorMessage(149, num3);
			break;
		}
	}

	[EventHandler("gtacnr:powersaw:doorCutOff")]
	private void OnDoorCutOff(int playerId, int vehicleId, int doorIndex)
	{
		if (API.NetworkDoesEntityExistWithNetworkId(vehicleId))
		{
			Entity obj = Entity.FromNetworkId(vehicleId);
			Vehicle val = (Vehicle)(object)((obj is Vehicle) ? obj : null);
			PlayerState playerState = LatentPlayers.Get(playerId);
			string vehicleFullName = Utils.GetVehicleFullName(((Entity)val).Model.Hash);
			val.Doors[(VehicleDoorIndex)doorIndex].Break(true);
			if (doorIndex == 0)
			{
				val.LockStatus = (VehicleLockStatus)1;
			}
			if (playerId != Game.Player.ServerId)
			{
				Utils.SendNotification(playerState.ColorNameAndId + " cut off the door of a ~y~" + vehicleFullName + "~s~.");
			}
		}
	}

	private void CutCatalyticConverter()
	{
	}
}
