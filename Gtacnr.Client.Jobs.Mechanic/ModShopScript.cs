using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Mechanic;

public class ModShopScript : Script
{
	private List<Business> modShops = new List<Business>();

	private const float MOD_SHOP_RADIUS = 50f;

	private const float WORK_AREA_RADIUS = 12.5f;

	private MechanicShopWorkArea? currentWorkArea;

	private Business? currentModShop;

	private List<MechanicShopWorkArea> freeWorkAreas = new List<MechanicShopWorkArea>();

	private Menu tempMenu;

	private static ModShopScript instance;

	private bool areMechTasksAttached;

	private bool areNonMechTasksAttached;

	private bool isNonMechKeyAttached;

	public static Business? CurrentModShop => instance.currentModShop;

	public static MechanicShopWorkArea? CurrentWorkArea => instance.currentWorkArea;

	public ModShopScript()
	{
		instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChanged;
		tempMenu = new Menu("Mechanic", "");
		tempMenu.AddLoadingMenuItem();
		MenuController.AddMenu(tempMenu);
	}

	protected override async void OnStarted()
	{
		while (!BusinessScript.IsReady)
		{
			await BaseScript.Delay(10);
		}
		modShops = BusinessScript.Businesses.Values.Where(delegate(Business b)
		{
			if (b.Type == BusinessType.Mechanic)
			{
				MechanicShop mechanic = b.Mechanic;
				if (mechanic == null)
				{
					return false;
				}
				return mechanic.Type == MechanicType.ModShop;
			}
			return false;
		}).ToList();
	}

	private void OnJobChanged(object sender, JobArgs e)
	{
		if (e.CurrentJobEnum == JobsEnum.Mechanic)
		{
			AttachMechanicTasks();
			DetachNonMechanicTasks();
		}
		else
		{
			AttachNonMechanicTasks();
			DetachMechanicTasks();
		}
	}

	private void AttachMechanicTasks()
	{
		if (!areMechTasksAttached)
		{
			base.Update += MechCheckTask;
			base.Update += MechDrawTask;
			areMechTasksAttached = true;
		}
	}

	private void DetachMechanicTasks()
	{
		if (areMechTasksAttached)
		{
			base.Update -= MechCheckTask;
			base.Update -= MechDrawTask;
			areMechTasksAttached = false;
		}
	}

	private async Coroutine MechCheckTask()
	{
		await BaseScript.Delay(500);
		_ = currentModShop;
		MechanicShopWorkArea mechanicShopWorkArea = currentWorkArea;
		DetermineCurrentModShopAndWorkArea();
		if (currentModShop != null)
		{
			freeWorkAreas = currentModShop.Mechanic.WorkAreas.Where((MechanicShopWorkArea w) => IsWorkAreaFree(w)).ToList();
		}
		else
		{
			freeWorkAreas.Clear();
		}
		if (currentWorkArea == mechanicShopWorkArea)
		{
			return;
		}
		if (currentWorkArea != null && currentModShop != null)
		{
			if (IsWorkAreaFree(currentWorkArea))
			{
				Enter();
			}
		}
		else
		{
			BaseScript.TriggerServerEvent("gtacnr:modshop:exitWorkArea", new object[0]);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_LEFT_WORK_AREA));
		}
		async void Enter()
		{
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:modshop:enterWorkArea", currentModShop.Id, GetWorkAreaIndex(currentModShop, currentWorkArea));
			if (responseCode == ResponseCode.Success)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_ENTERED_WORK_AREA, GetWorkAreaTypeDescription(currentWorkArea.Type)));
			}
			else
			{
				Utils.DisplayError(responseCode, "", "MechCheckTask");
			}
		}
	}

	private async Coroutine MechDrawTask()
	{
		if (currentModShop == null || currentWorkArea != null)
		{
			return;
		}
		Vector3 val = default(Vector3);
		foreach (MechanicShopWorkArea freeWorkArea in freeWorkAreas)
		{
			Vector3 location = freeWorkArea.Location;
			((Vector3)(ref val))._002Ector(12.5f, 12.5f, 0.4f);
			Color color = Color.FromInt(1790108288);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(location.X, location.Y, location.Z, ref z, false))
			{
				location.Z = z;
			}
			API.DrawMarker(1, location.X, location.Y, location.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	private void AttachNonMechanicTasks()
	{
		if (!areNonMechTasksAttached)
		{
			base.Update += NonMechCheckTask;
			areNonMechTasksAttached = true;
		}
	}

	private void DetachNonMechanicTasks()
	{
		if (areNonMechTasksAttached)
		{
			base.Update -= NonMechCheckTask;
			areNonMechTasksAttached = false;
		}
	}

	private async Coroutine NonMechCheckTask()
	{
		await BaseScript.Delay(500);
		_ = currentModShop;
		MechanicShopWorkArea mechanicShopWorkArea = currentWorkArea;
		DetermineCurrentModShopAndWorkArea();
		if (mechanicShopWorkArea == null && currentWorkArea != null && !IsWorkAreaFree(currentWorkArea) && (Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			AttachNonMechKeybinds();
		}
		else if (currentWorkArea == null && mechanicShopWorkArea != null)
		{
			DetachNonMechKeybinds();
		}
	}

	private void AttachNonMechKeybinds()
	{
		if (!isNonMechKeyAttached)
		{
			KeysScript.AttachListener((Control)51, OnNonMechKeyEvent, 1000);
			Utils.AddInstructionalButton("modShopOpen", new InstructionalButton("Seller", 2, (Control)51));
			isNonMechKeyAttached = true;
		}
	}

	private void DetachNonMechKeybinds()
	{
		if (isNonMechKeyAttached)
		{
			KeysScript.DetachListener((Control)51, OnNonMechKeyEvent);
			Utils.RemoveInstructionalButton("modShopOpen");
			isNonMechKeyAttached = false;
		}
	}

	private bool OnNonMechKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed)
		{
			tempMenu.OpenMenu();
			OpenRealMechanicMenu();
		}
		return false;
		async void OpenRealMechanicMenu()
		{
			int num = await TriggerServerEventAsync<int>("gtacnr:modshop:getMechanicInWorkArea", new object[2]
			{
				currentModShop.Id,
				GetWorkAreaIndex(currentModShop, currentWorkArea)
			});
			MenuController.CloseAllMenus();
			if (num > 0)
			{
				Utils.PlayContinueSound();
				SellToPlayersScript.OpenSellerMenu(num);
			}
			else
			{
				Utils.PlayErrorSound();
				Print($"The server returned {num} when trying to get the id of the mechanic in the work area.");
			}
		}
	}

	private void DetermineCurrentModShopAndWorkArea()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		currentModShop = null;
		currentWorkArea = null;
		if (!((Entity)Game.PlayerPed).IsAlive)
		{
			return;
		}
		foreach (Business modShop in modShops)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared(modShop.Location) < 50f.Square()))
			{
				continue;
			}
			currentModShop = modShop;
			{
				foreach (MechanicShopWorkArea workArea in modShop.Mechanic.WorkAreas)
				{
					position = ((Entity)Game.PlayerPed).Position;
					if (((Vector3)(ref position)).DistanceToSquared(workArea.Location) < 12.5f.Square())
					{
						currentWorkArea = workArea;
						break;
					}
				}
				break;
			}
		}
	}

	private int GetWorkAreaIndex(Business modShop, MechanicShopWorkArea workArea)
	{
		return modShop.Mechanic.WorkAreas.IndexOf(workArea);
	}

	private bool IsWorkAreaFree(MechanicShopWorkArea workArea)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player.ServerId == Game.Player.ServerId)
			{
				continue;
			}
			PlayerState playerState = LatentPlayers.Get(player);
			if (playerState != null && playerState.JobEnum == JobsEnum.Mechanic)
			{
				Vector3 position = ((Entity)player.Character).Position;
				if (((Vector3)(ref position)).DistanceToSquared(workArea.Location) < 12.5f.Square())
				{
					return false;
				}
			}
		}
		return true;
	}

	public static string GetWorkAreaTypeDescription(MechanicShopWorkAreaType workAreaT)
	{
		return workAreaT switch
		{
			MechanicShopWorkAreaType.Bodywork => LocalizationController.S(Entries.Jobs.MECHANIC_WORK_AREA_TYPE_BODYWORK), 
			MechanicShopWorkAreaType.Respray => LocalizationController.S(Entries.Jobs.MECHANIC_WORK_AREA_TYPE_RESPRAY), 
			_ => "", 
		};
	}
}
