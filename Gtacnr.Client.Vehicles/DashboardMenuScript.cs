using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Businesses.Banks;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.PlayerInteraction;
using Gtacnr.Client.PlayerInteraction.Racing;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Vehicles;

public class DashboardMenuScript : Script
{
	private Dictionary<string, Menu> menus = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private List<PlayerState> passengersList = new List<PlayerState>();

	private DateTime lastEjectTimestamp;

	private bool isBusyEjecting;

	private Vehicle targetVehicle;

	private bool isInActiveVehicle;

	private bool leaveEngineOn;

	private static DashboardMenuScript instance;

	public DashboardMenuScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		CreateMenus();
		AttachKeyListeners();
		AddCommandSuggestions();
		AttachTasks();
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if ((Entity)(object)e.Vehicle != (Entity)null)
		{
			API.SetVehicleKeepEngineOnWhenAbandoned(((PoolObject)e.Vehicle).Handle, leaveEngineOn);
		}
	}

	private void CreateMenus()
	{
		menus["main"] = new Menu(LocalizationController.S(Entries.Vehicles.DASHBOARD_MENU_TITLE), "")
		{
			PlaySelectSound = false
		};
		menus["main"].OnItemSelect += OnMenuItemSelect;
		menus["main"].OnListItemSelect += OnMenuListItemSelect;
		MenuController.AddMenu(menus["main"]);
	}

	private void OpenMenu()
	{
		if (!DealershipScript.IsInDealership && SpawnScript.HasSpawned && !menus["main"].Visible)
		{
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if (!((Entity)(object)currentVehicle == (Entity)null) && currentVehicle.Exists())
			{
				RefreshMenu();
				RefreshVehicleState();
				menus["main"].OpenMenu();
			}
		}
	}

	private void RefreshMenu()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Expected I4, but got Unknown
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Expected I4, but got Unknown
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		targetVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)targetVehicle == (Entity)null || !targetVehicle.Exists())
		{
			targetVehicle = null;
			return;
		}
		bool flag = (Entity)(object)targetVehicle.Driver == (Entity)(object)Game.PlayerPed;
		Vehicle activeVehicle = ActiveVehicleScript.ActiveVehicle;
		bool flag2 = (int)targetVehicle.LockStatus == 2;
		bool isEngineRunning = targetVehicle.IsEngineRunning;
		bool flag3 = !(LatentVehicleStateScript.Get(((Entity)targetVehicle).NetworkId)?.SilentSiren ?? false);
		bool flag4 = ModeratorVehiclesScript.IsStaffVehicle(targetVehicle) && (int)StaffLevelScript.StaffLevel >= 100;
		menus["main"].ClearMenuItems();
		MenuItem item;
		if (flag)
		{
			Menu menu = menus["main"];
			item = (menuItems["lock"] = new MenuItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_DOORS))
			{
				Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_DOORS_DESC),
				Label = (flag2 ? LocalizationController.S(Entries.Vehicles.DASHBOARD_LOCKED) : LocalizationController.S(Entries.Vehicles.DASHBOARD_UNLOCKED))
			});
			menu.AddMenuItem(item);
			if ((Entity)(object)targetVehicle != (Entity)(object)activeVehicle && !flag4)
			{
				menuItems["lock"].Enabled = false;
				menuItems["lock"].Label = LocalizationController.S(Entries.Main.LABEL_UNAVAILABLE);
				MenuItem menuItem2 = menuItems["lock"];
				menuItem2.Description = menuItem2.Description + "\n~r~" + LocalizationController.S(Entries.Vehicles.DASHBOARD_DOORS_FEATURE_UNAVAILABLE);
			}
			Menu menu2 = menus["main"];
			item = (menuItems["engine"] = new MenuItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_ENGINE))
			{
				Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_ENGINE_DESC),
				Label = (isEngineRunning ? ("~g~" + LocalizationController.S(Entries.Main.LABEL_ON)) : ("~r~" + LocalizationController.S(Entries.Main.LABEL_OFF)))
			});
			menu2.AddMenuItem(item);
			Menu menu3 = menus["main"];
			item = (menuItems["keepEngineOn"] = new MenuItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_KEEP_ENGINE_ON))
			{
				Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_KEEP_ENGINE_ON_DESC),
				Label = (leaveEngineOn ? ("~r~" + LocalizationController.S(Entries.Main.LABEL_ON)) : ("~g~" + LocalizationController.S(Entries.Main.LABEL_OFF)))
			});
			menu3.AddMenuItem(item);
			List<string> list = new List<string>();
			VehicleWindow[] allWindows = targetVehicle.Windows.GetAllWindows();
			foreach (VehicleWindow val in allWindows)
			{
				if (!flag)
				{
					VehicleWindowIndex val2 = (VehicleWindowIndex)(Game.PlayerPed.SeatIndex + 1);
					if (val.Index != val2)
					{
						continue;
					}
				}
				string item2 = $"#{val.Index + 1:0}";
				switch ((int)val.Index)
				{
				case 0:
					item2 = LocalizationController.S(Entries.Vehicles.DASHBOARD_WINDOW_FL);
					break;
				case 1:
					item2 = LocalizationController.S(Entries.Vehicles.DASHBOARD_WINDOW_FR);
					break;
				case 2:
					item2 = LocalizationController.S(Entries.Vehicles.DASHBOARD_WINDOW_BL);
					break;
				case 3:
					item2 = LocalizationController.S(Entries.Vehicles.DASHBOARD_WINDOW_BR);
					break;
				}
				list.Add(item2);
			}
			if (list.Count > 0)
			{
				Menu menu4 = menus["main"];
				item = (menuItems["windows"] = new MenuListItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_WINDOWS), list)
				{
					Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_WINDOWS_DESC)
				});
				menu4.AddMenuItem(item);
			}
			if (targetVehicle.Doors.HasDoor((VehicleDoorIndex)4))
			{
				bool isOpen = targetVehicle.Doors[(VehicleDoorIndex)4].IsOpen;
				Menu menu5 = menus["main"];
				item = (menuItems["hood"] = new MenuItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_HOOD))
				{
					Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_HOOD_DESC),
					Label = ((!isOpen) ? ("~g~" + LocalizationController.S(Entries.Main.LABEL_CLOSED)) : ("~r~" + LocalizationController.S(Entries.Main.LABEL_OPEN)))
				});
				menu5.AddMenuItem(item);
			}
			if (targetVehicle.Doors.HasDoor((VehicleDoorIndex)5))
			{
				bool isOpen2 = targetVehicle.Doors[(VehicleDoorIndex)5].IsOpen;
				Menu menu6 = menus["main"];
				item = (menuItems["trunk"] = new MenuItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_TRUNK))
				{
					Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_TRUNK_DESC),
					Label = ((!isOpen2) ? ("~g~" + LocalizationController.S(Entries.Main.LABEL_CLOSED)) : ("~r~" + LocalizationController.S(Entries.Main.LABEL_OPEN)))
				});
				menu6.AddMenuItem(item);
			}
			PersonalVehicleModel value = DealershipScript.VehicleModelData.FirstOrDefault<KeyValuePair<string, PersonalVehicleModel>>((KeyValuePair<string, PersonalVehicleModel> d) => Model.op_Implicit(API.GetHashKey(d.Key)) == ((Entity)targetVehicle).Model).Value;
			if (value != null && value.HasSiren)
			{
				Menu menu7 = menus["main"];
				item = (menuItems["siren"] = new MenuItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_SIREN))
				{
					Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_SIREN_DESC),
					Label = ((!flag3) ? ("~g~" + LocalizationController.S(Entries.Main.LABEL_ENABLED)) : ("~r~" + LocalizationController.S(Entries.Main.LABEL_DISABLED)))
				});
				menu7.AddMenuItem(item);
			}
			if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
			{
				Menu menu8 = menus["main"];
				item = (menuItems["racing"] = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_MAIN))
				{
					Description = LocalizationController.S(Entries.Player.RACING_START_NEW_RACE)
				});
				menu8.AddMenuItem(item);
			}
		}
		List<string> passengersMenuList = GetPassengersMenuList();
		Menu menu9 = menus["main"];
		item = (menuItems["passengers"] = new MenuListItem(LocalizationController.S(Entries.Vehicles.DASHBOARD_PASSENGERS), passengersMenuList)
		{
			Description = LocalizationController.S(Entries.Vehicles.DASHBOARD_PASSENGERS_DESC)
		});
		menu9.AddMenuItem(item);
		if (passengersMenuList.Count == 0)
		{
			menuItems["passengers"].Enabled = false;
			MenuItem menuItem11 = menuItems["passengers"];
			menuItem11.Description = menuItem11.Description + "\n~r~" + LocalizationController.S(Entries.Vehicles.DASHBOARD_PASSENGERS_NONE);
		}
		else
		{
			MenuController.BindMenuItem(menus["main"], PlayerMenuScript.PlayerMenu, menuItems["passengers"]);
		}
	}

	private void AttachTasks()
	{
		base.Update += CheckVehicleTask;
	}

	private void RefreshVehicleState()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		VehicleState vehicleState = LatentVehicleStateScript.Get((currentVehicle != null) ? ((Entity)currentVehicle).NetworkId : 0);
		if ((Entity)(object)currentVehicle == (Entity)null || vehicleState == null)
		{
			SetFuelText("");
			return;
		}
		float num = vehicleState?.Fuel ?? 0f;
		if (num > 1f)
		{
			num = 1f;
		}
		else if (num < 0f)
		{
			num = 0f;
		}
		PersonalVehicleModel personalVehicleModel = DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)currentVehicle).Model));
		float num2 = currentVehicle.EngineHealth / 1000f;
		SetHealthText($"{LocalizationController.S(Entries.Vehicles.DASHBOARD_LABEL_HEALTH)}: {num2 * 100f:0.#}%");
		if (personalVehicleModel == null || personalVehicleModel.Type != PersonalVehicleType.Bicycle)
		{
			string arg = ((personalVehicleModel == null || !personalVehicleModel.IsElectric) ? LocalizationController.S(Entries.Vehicles.DASHBOARD_LABEL_FUEL) : LocalizationController.S(Entries.Vehicles.DASHBOARD_LABEL_BATTERY));
			SetFuelText($"{arg}: {num * 100f:0.#}%");
		}
		else
		{
			SetFuelText("");
		}
		void SetFuelText(string text)
		{
			menus["main"].CounterPreText = text;
		}
		void SetHealthText(string text)
		{
			menus["main"].MenuSubtitle = text;
		}
	}

	private List<string> GetPassengersMenuList()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null)
		{
			return null;
		}
		List<string> list = new List<string>();
		passengersList.Clear();
		foreach (Ped passenger in from p in currentVehicle.Passengers.Concat((IEnumerable<Ped>)(object)new Ped[1] { currentVehicle.Driver })
			where p.IsPlayer && ((PoolObject)p).Handle != ((PoolObject)Game.PlayerPed).Handle
			select p)
		{
			Player val = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => ((PoolObject)p.Character).Handle == ((PoolObject)passenger).Handle);
			if (val != (Player)null)
			{
				PlayerState playerState = LatentPlayers.Get(val);
				list.Add(playerState.ColorNameAndId ?? "");
				passengersList.Add(playerState);
			}
		}
		return list;
	}

	public static void UpdatePassengersMenuItem()
	{
		instance.UpdatePassengersMenuItem_();
	}

	private void UpdatePassengersMenuItem_()
	{
		if (!((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null))
		{
			List<string> passengersMenuList = GetPassengersMenuList();
			(menuItems["passengers"] as MenuListItem).ListItems = passengersMenuList;
			menuItems["passengers"].Enabled = passengersMenuList.Count > 0;
		}
	}

	private void OpenMenuOrToggleLock()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists())
		{
			ToggleVehicleLock();
		}
		else
		{
			OpenMenu();
		}
	}

	private void ToggleVehicleLock(bool? toggle = null)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Invalid comparison between Unknown and I4
		targetVehicle = ActiveVehicleScript.ActiveVehicle;
		if ((Entity)(object)targetVehicle == (Entity)null || !targetVehicle.Exists())
		{
			if (toggle.HasValue)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Error, LocalizationController.S(Entries.Vehicles.DASHBOARD_NO_ACTIVE_VEHICLE));
			}
			return;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return;
		}
		if (CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsInCustody)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_CANT_LOCK_UNLOCK_WHEN_CUFFED));
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(((Entity)targetVehicle).Position) > 900f)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_OUT_OF_RANGE));
			return;
		}
		bool flag = (int)targetVehicle.LockStatus == 2;
		if (!toggle.HasValue)
		{
			flag = !flag;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_LOCK_UNLOCK, flag ? LocalizationController.S(Entries.Vehicles.VEHICLE_OPTION_LOCKED) : LocalizationController.S(Entries.Vehicles.VEHICLE_OPTION_UNLOCKED)), playSound: false, 2000);
		}
		else
		{
			if (flag == toggle.Value)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Your vehicle is already " + (flag ? "locked" : "unlocked") + ".");
				return;
			}
			flag = toggle.Value;
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, "You " + (flag ? "locked" : "unlocked") + " your vehicle.");
		}
		targetVehicle.LockStatus = (VehicleLockStatus)((!flag) ? 1 : 2);
		bool num = (Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)(object)targetVehicle;
		string fileName = (num ? "car_lock_inside.wav" : "car_lock.wav");
		if (!num && !ATMScript.IsHacking && !BaseReviveScript.IsReviving)
		{
			Animate(1500);
		}
		AudioScript.PlayAudio(fileName, 0.2f);
		if (menuItems.ContainsKey("lock"))
		{
			menuItems["lock"].Label = (flag ? ("~g~" + LocalizationController.S(Entries.Vehicles.DASHBOARD_LOCKED)) : ("~r~" + LocalizationController.S(Entries.Vehicles.DASHBOARD_UNLOCKED)));
		}
		static async void Animate(int duration)
		{
			if (API.GetVehiclePedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle) == 0)
			{
				Game.PlayerPed.Task.ClearAnimation("anim@mp_player_intmenu@key_fob@", "fob_click");
			}
			Game.PlayerPed.Task.PlayAnimation("anim@mp_player_intmenu@key_fob@", "fob_click", 4f, duration, (AnimationFlags)51);
			await BaseScript.Delay(duration);
			Game.PlayerPed.Task.ClearAnimation("anim@mp_player_intmenu@key_fob@", "fob_click");
		}
	}

	private bool ToggleVehicleEngine()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			return false;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return false;
		}
		if (currentVehicle.IsEngineRunning)
		{
			if (((Entity)currentVehicle).Model.IsPlane && ((Entity)currentVehicle).IsInAir)
			{
				Utils.PlayErrorSound();
				return false;
			}
			currentVehicle.IsEngineRunning = false;
			menuItems["engine"].Label = "~r~" + LocalizationController.S(Entries.Main.LABEL_OFF);
		}
		else
		{
			currentVehicle.IsEngineStarting = true;
			menuItems["engine"].Label = "~g~" + LocalizationController.S(Entries.Main.LABEL_ON);
		}
		return true;
	}

	private bool SetVehicleKeepEngineOn(bool keepOn)
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			return false;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return false;
		}
		API.SetVehicleKeepEngineOnWhenAbandoned(((PoolObject)currentVehicle).Handle, keepOn);
		menuItems["keepEngineOn"].Label = (keepOn ? ("~g~" + LocalizationController.S(Entries.Main.LABEL_ON)) : ("~r~" + LocalizationController.S(Entries.Main.LABEL_OFF)));
		leaveEngineOn = keepOn;
		return true;
	}

	private bool ToggleVehicleDoor(VehicleDoorIndex door, MenuItem menuItemToRefresh = null)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || currentVehicle.Speed > 0.2f || !currentVehicle.Doors.HasDoor(door) || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			return false;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return false;
		}
		bool flag;
		if (currentVehicle.Doors[door].IsOpen)
		{
			currentVehicle.Doors[door].Close(false);
			flag = false;
		}
		else
		{
			currentVehicle.Doors[door].Open(false, false);
			flag = true;
		}
		if (menuItemToRefresh != null)
		{
			menuItemToRefresh.Label = (flag ? ("~r~" + LocalizationController.S(Entries.Main.LABEL_OPEN)) : ("~g~" + LocalizationController.S(Entries.Main.LABEL_CLOSED)));
		}
		return true;
	}

	private bool ToggleSirenSound()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			return false;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return false;
		}
		int networkId = ((Entity)currentVehicle).NetworkId;
		if (networkId == 0)
		{
			return false;
		}
		bool flag = LatentVehicleStateScript.Get(networkId)?.SilentSiren ?? false;
		currentVehicle.IsSirenSilent = !flag;
		BaseScript.TriggerServerEvent("gtacnr:vehicles:updateSilentSiren", new object[2]
		{
			((Entity)currentVehicle).NetworkId,
			!flag
		});
		if (menuItems.ContainsKey("siren"))
		{
			menuItems["siren"].Label = ((!flag) ? ("~g~" + LocalizationController.S(Entries.Main.LABEL_ENABLED)) : ("~r~" + LocalizationController.S(Entries.Main.LABEL_DISABLED)));
		}
		return true;
	}

	private void ToggleIndicator(int index)
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			Utils.PlayErrorSound();
			return;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return;
		}
		switch (index)
		{
		case -1:
			currentVehicle.IsLeftIndicatorLightOn = !currentVehicle.IsLeftIndicatorLightOn;
			break;
		case 1:
			currentVehicle.IsRightIndicatorLightOn = !currentVehicle.IsRightIndicatorLightOn;
			break;
		case 0:
			currentVehicle.IsLeftIndicatorLightOn = !currentVehicle.IsLeftIndicatorLightOn;
			currentVehicle.IsRightIndicatorLightOn = currentVehicle.IsLeftIndicatorLightOn;
			break;
		}
		Entity entityAttachedTo = ((Entity)currentVehicle).GetEntityAttachedTo();
		Vehicle val = (Vehicle)(object)((entityAttachedTo is Vehicle) ? entityAttachedTo : null);
		if (val != null)
		{
			val.IsLeftIndicatorLightOn = currentVehicle.IsLeftIndicatorLightOn;
			val.IsRightIndicatorLightOn = currentVehicle.IsRightIndicatorLightOn;
		}
	}

	[Update]
	private async Coroutine SilentSirenTask()
	{
		await Script.Wait(1000);
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player == Game.Player)
			{
				continue;
			}
			Vehicle currentVehicle = player.Character.CurrentVehicle;
			if (!((Entity)(object)currentVehicle == (Entity)null) && currentVehicle.Exists() && ((Entity)currentVehicle).NetworkId != 0)
			{
				VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)currentVehicle).NetworkId);
				if (vehicleState != null)
				{
					bool silentSiren = vehicleState.SilentSiren;
					currentVehicle.IsSirenSilent = silentSiren;
				}
			}
		}
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (IsSelected("lock"))
		{
			ToggleVehicleLock();
		}
		else if (IsSelected("engine"))
		{
			if (ToggleVehicleEngine())
			{
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
		else if (IsSelected("keepEngineOn"))
		{
			if (SetVehicleKeepEngineOn(!leaveEngineOn))
			{
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
		else if (IsSelected("hood"))
		{
			if (ToggleVehicleDoor((VehicleDoorIndex)4, menuItem))
			{
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
		else if (IsSelected("trunk"))
		{
			if (ToggleVehicleDoor((VehicleDoorIndex)5, menuItem))
			{
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
		else if (IsSelected("siren"))
		{
			if (ToggleSirenSound())
			{
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
		else if (IsSelected("racing"))
		{
			Utils.PlaySelectSound();
			RacingMenuScript.ShowMenu(editor: true, menus["main"]);
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

	private async void OnMenuListItemSelect(Menu menu, MenuListItem menuItem, int selectedIndex, int itemIndex)
	{
		if (IsSelected("windows"))
		{
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists())
			{
				Utils.PlayErrorSound();
				return;
			}
			VehicleWindow val = currentVehicle.Windows.GetAllWindows()[selectedIndex];
			if (val.IsIntact)
			{
				val.RollDown();
			}
			else
			{
				val.RollUp();
			}
			Utils.PlaySelectSound();
		}
		else if (IsSelected("passengers"))
		{
			PlayerState playerInfo = passengersList[selectedIndex];
			Player val2 = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => p.ServerId == playerInfo.Id);
			if (val2 != (Player)null)
			{
				PlayerMenuScript.SetTargetPlayer(val2);
				PlayerMenuScript.DisableMaxDistance();
			}
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

	public static async Task<bool> EjectPlayer(Player player)
	{
		return await instance.EjectPlayer_(player);
	}

	private async Task<bool> EjectPlayer_(Player player)
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed || (Entity)(object)player.Character.CurrentVehicle != (Entity)(object)currentVehicle)
		{
			return false;
		}
		try
		{
			if (!Gtacnr.Utils.CheckTimePassed(lastEjectTimestamp, 1000.0) || isBusyEjecting)
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_EJECTING_PASSENGER_COOLDOWN));
				return false;
			}
			lastEjectTimestamp = DateTime.UtcNow;
			isBusyEjecting = true;
			if (!(await TriggerServerEventAsync<bool>("gtacnr:vehicles:eject", new object[1] { player.ServerId })))
			{
				Utils.PlayErrorSound();
				return false;
			}
			PlayerState playerState = LatentPlayers.Get(player);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.PLAYER_EJECTED, playerState.ColorNameAndId));
			return true;
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isBusyEjecting = false;
		}
		return false;
	}

	[EventHandler("gtacnr:vehicles:onEjected")]
	private void OnEjected(int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.PLAYER_EJECTED_YOU, playerState.ColorNameAndId));
		Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)4096);
	}

	private void AttachKeyListeners()
	{
		KeysScript.AttachListener((Control)303, OnKeyEvent, -10);
	}

	private void AddCommandSuggestions()
	{
		Chat.AddSuggestion("/veh-menu", "Opens the vehicle interaction menu.");
		Chat.AddSuggestion("/veh-deliver", "Requests the delivery of a vehicle to your location.", new ChatParamSuggestion("plate", "The license plate of the vehicle to deliver."));
		Chat.AddSuggestion("/veh-store", "Stores your active vehicle.");
		Chat.AddSuggestion("/lock", "Locks your active vehicle if unlocked.");
		Chat.AddSuggestion("/unlock", "Unlocks your active vehicle if locked.");
		Chat.AddSuggestion("/engine", "Turns on or off your vehicle engine.");
		Chat.AddSuggestion("/hood", "Opens or closes the vehicle's hood.");
		Chat.AddSuggestion("/trunk", "Opens or closes the vehicle's trunk.");
		Chat.AddSuggestion("/siren", "Enables or disables sirens.");
		Chat.AddSuggestion("/indicator", "Toggle your vehicle turn indicators on and off.", new ChatParamSuggestion("which", "l/r/all"));
		Chat.AddSuggestion("/eject", "Eject someone from your vehicle.", new ChatParamSuggestion("player", "The id of the player to eject."));
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (!MenuController.IsAnyMenuOpen() && ((eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard) || (eventType == KeyEventType.DoublePressed && inputType == InputType.Controller)))
		{
			OpenMenuOrToggleLock();
			return true;
		}
		return false;
	}

	[Command("veh-menu")]
	private void VehMenuCommand()
	{
		if (!menus["main"].Visible)
		{
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must be driving a vehicle to open its interaction menu.");
			}
			else
			{
				OpenMenu();
			}
		}
	}

	[Command("veh-deliver")]
	private void DeliverCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /deliver [license plate]");
			return;
		}
		string text = args[0];
		text = text.ToUpperInvariant().Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /deliver [license plate]");
		}
		else
		{
			VehiclesMenuScript.DeliverVehicle(text);
		}
	}

	[Command("veh-store")]
	private void StoreCommand(string[] args)
	{
		BaseScript.TriggerEvent("gtacnr:vehicles:store", new object[0]);
	}

	[Command("togglelock")]
	private void ToggleLockCommand()
	{
		ToggleVehicleLock();
	}

	[Command("lock")]
	private void LockCommand()
	{
		ToggleVehicleLock(true);
	}

	[Command("unlock")]
	private void UnlockCommand()
	{
		ToggleVehicleLock(false);
	}

	[Command("engine")]
	private void EngineCommand()
	{
		ToggleVehicleEngine();
	}

	[Command("hood")]
	private void HoodCommand()
	{
		ToggleVehicleDoor((VehicleDoorIndex)4);
	}

	[Command("trunk")]
	private void TrunkCommand()
	{
		ToggleVehicleDoor((VehicleDoorIndex)5);
	}

	[Command("siren")]
	private void SirenCommand()
	{
		ToggleSirenSound();
	}

	[Command("indicator")]
	private void IndicatorCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Utils.PlayErrorSound();
			return;
		}
		int num = ((!(args[0] == "all")) ? ((args[0] == "l") ? (-1) : ((args[0] == "r") ? 1 : 255)) : 0);
		if (num == 255)
		{
			Utils.PlayErrorSound();
		}
		else
		{
			ToggleIndicator(num);
		}
	}

	[Command("eject")]
	private async void EjectCommand(string[] args)
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must be driving a vehicle to eject players.");
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /eject [player id]");
			return;
		}
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player.ServerId == result)
			{
				PlayerState targetInfo = LatentPlayers.Get(player);
				if ((Entity)(object)player.Character.CurrentVehicle != (Entity)(object)currentVehicle)
				{
					Chat.AddMessage(Gtacnr.Utils.Colors.Error, targetInfo.NameAndId + " is not a passenger in your vehicle.");
				}
				else if (await EjectPlayer_(player))
				{
					Chat.AddMessage(Gtacnr.Utils.Colors.Info, targetInfo.NameAndId + " has been ejected.");
				}
				else
				{
					Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Unable to eject " + targetInfo.NameAndId + ".");
				}
				return;
			}
		}
		Chat.AddMessage(Gtacnr.Utils.Colors.Error, $"Player id {result} is not connected.");
	}

	[Update]
	private async Coroutine RefreshFuelTask()
	{
		await Script.Wait(3000);
		RefreshVehicleState();
	}

	[Update]
	private async Coroutine CheckLockedTask()
	{
		try
		{
			await Script.Wait(250);
			Vehicle activeVehicle = ActiveVehicleScript.ActiveVehicle;
			if ((Entity)(object)activeVehicle == (Entity)null)
			{
				return;
			}
			if ((Entity)(object)activeVehicle == (Entity)(object)Game.PlayerPed.VehicleTryingToEnter)
			{
				if ((int)activeVehicle.LockStatus == 2)
				{
					ToggleVehicleLock(false);
				}
			}
			else if ((Entity)(object)activeVehicle == (Entity)(object)Game.PlayerPed.CurrentVehicle)
			{
				if (!isInActiveVehicle)
				{
					activeVehicle.IsEngineRunning = true;
					isInActiveVehicle = true;
				}
			}
			else
			{
				isInActiveVehicle = false;
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async Coroutine CheckVehicleTask()
	{
		_ = 1;
		try
		{
			await Script.Wait(1000);
			if (!Game.PlayerPed.IsInVehicle())
			{
				return;
			}
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if (!((Entity)(object)((currentVehicle != null) ? currentVehicle.Driver : null) != (Entity)(object)Game.PlayerPed) || DealershipScript.IsInDealership)
			{
				base.Update -= CheckVehicleTask;
				await Script.Wait(10000);
				if (Utils.IsUsingKeyboard())
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.DASHBOARD_PRESS_TO_OPEN, "~INPUT_REPLAY_SCREENSHOT~"));
				}
				else
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.DASHBOARD_DOUBLE_PRESS_TO_OPEN, "~INPUT_REPLAY_SCREENSHOT~"));
				}
				Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Vehicles.DASHBOARD_EXPLANATION));
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}
}
