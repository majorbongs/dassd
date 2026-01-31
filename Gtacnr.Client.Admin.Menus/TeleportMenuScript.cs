using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus;

public class TeleportMenuScript : Script, ICnRAdminMenu, ICnRMenu
{
	private Menu MainMenu;

	private MenuItem MapTpItem;

	private MenuItem SavePosItem;

	private MenuItem GoToPosItem;

	private bool isMapTpActive;

	private Vector3 savedPosition;

	private bool savedPosFindZ;

	public static TeleportMenuScript Instance { get; private set; }

	public TeleportMenuScript()
	{
		Instance = this;
		ModeratorMenuScript.ModeratorCommandsRegistered = (EventHandler<EventArgs>)Delegate.Combine(ModeratorMenuScript.ModeratorCommandsRegistered, new EventHandler<EventArgs>(OnModeratorCommandsRegistered));
	}

	public void CreateMenus()
	{
		if (MainMenu == null)
		{
			MainMenu = new Menu("Moderator Menu", "Teleport options")
			{
				CloseWhenDead = false
			};
			Menu mainMenu = MainMenu;
			MenuItem obj = new MenuItem("Map Teleport", "Makes you teleport by placing a waypoint on the map and pressing ~b~Page Down~s~.")
			{
				Label = "~r~OFF"
			};
			MenuItem item = obj;
			MapTpItem = obj;
			mainMenu.AddMenuItem(item);
			MainMenu.AddMenuItem(SavePosItem = new MenuItem("Save Position", "Saves the current position (~b~Page Up~s~), so that you can quickly teleport back."));
			MainMenu.AddMenuItem(GoToPosItem = new MenuItem("Teleport to Saved Position", "Teleports you back to the saved position (~b~Page Down~s~)."));
			MainMenu.OnItemSelect += OnItemSelect;
		}
	}

	private void OnModeratorCommandsRegistered(object sender, EventArgs e)
	{
		API.RegisterCommand("asavepos", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
		{
			SavePos();
		}), false);
		Chat.AddSuggestion("/asavepos", "Saves your current position for quick teleport.");
		API.RegisterCommand("agotopos", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
		{
			GotoPos();
		}), false);
		Chat.AddSuggestion("/agotopos", "Teleports to the saved position.");
		API.RegisterCommand("amaptp", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
		{
			ToggleMapTpMode();
		}), false);
		Chat.AddSuggestion("/amaptp", "Toggles map teleport mode.");
	}

	public Menu GetMenu()
	{
		return MainMenu;
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			if (IsSelected(MapTpItem))
			{
				ToggleMapTpMode();
			}
			else if (IsSelected(SavePosItem))
			{
				SavePos();
			}
			else if (IsSelected(GoToPosItem))
			{
				GotoPos();
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
			}
		}
		bool IsSelected(MenuItem key)
		{
			return key == menuItem;
		}
	}

	private void ToggleMapTpMode()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			isMapTpActive = !isMapTpActive;
			if (isMapTpActive)
			{
				Utils.DisplayHelpText("~g~You enabled map teleport: set a waypoint on the map, and press Page Down to teleport.", playSound: false);
				MapTpItem.Label = "~g~ON";
			}
			else
			{
				Utils.DisplayHelpText("~g~You disabled map teleport.", playSound: false);
				MapTpItem.Label = "~r~OFF";
			}
		}
	}

	public void SavePos()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if ((int)StaffLevelScript.StaffLevel >= 100 && !((Entity)(object)Game.PlayerPed == (Entity)null))
		{
			if (isMapTpActive && Game.IsWaypointActive)
			{
				Utils.DisplayHelpText("~g~You can't set a quick teleport while map teleport is active and the waypoint is set.", playSound: false);
				return;
			}
			savedPosition = new Vector3(((Entity)Game.PlayerPed).Position.X, ((Entity)Game.PlayerPed).Position.Y, ((Entity)Game.PlayerPed).Position.Z);
			savedPosFindZ = false;
			Utils.DisplayHelpText("~g~You saved the current position for quick teleport.", playSound: false);
		}
	}

	public async void GotoPos()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		string vehModelName = "";
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed)
			{
				Utils.DisplayHelpText("~r~You cannot teleport when you're not the driver of a vehicle.", playSound: false);
				return;
			}
			if (Game.PlayerPed.CurrentVehicle.PassengerCount > 0)
			{
				Utils.DisplayHelpText("~r~You cannot teleport when you have passengers.", playSound: false);
				return;
			}
			string displayNameFromVehicleModel = API.GetDisplayNameFromVehicleModel(Model.op_Implicit(((Entity)Game.PlayerPed.CurrentVehicle).Model));
			vehModelName = Game.GetGXTEntry(displayNameFromVehicleModel);
		}
		Vector3 positionToTeleportTo = savedPosition;
		if (isMapTpActive && Game.IsWaypointActive)
		{
			positionToTeleportTo = World.WaypointPosition;
			savedPosFindZ = true;
		}
		if (positionToTeleportTo == default(Vector3))
		{
			Utils.DisplayHelpText("~g~Quick teleport position has ~s~not been set~g~.");
			return;
		}
		Utils.TeleportFlags flags = Utils.TeleportFlags.TeleportVehicle | Utils.TeleportFlags.PlaceOnGround | Utils.TeleportFlags.VisualEffects;
		if (savedPosFindZ)
		{
			flags |= Utils.TeleportFlags.FindGroundHeight;
		}
		if (await TriggerServerEventAsync<bool>("gtacnr:admin:usingGotoPos", new object[0]))
		{
			await Utils.TeleportToCoords(positionToTeleportTo, -1f, flags);
			string locationName = Utils.GetLocationName(positionToTeleportTo);
			Utils.DisplayHelpText("~g~You teleported to your saved position.", playSound: false);
			BaseScript.TriggerServerEvent("gtacnr:admin:usedGotoPos", new object[3]
			{
				positionToTeleportTo.Json(),
				locationName,
				vehModelName
			});
		}
	}
}
