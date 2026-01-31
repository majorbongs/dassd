using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Estates.Garages;

public class JobGarageScript : Script
{
	private static List<Blip> blips = new List<Blip>();

	private JobGarage currentJobGarage;

	private JobGarage previousJobGarage;

	private bool canOpenJobGarageMenu;

	private bool couldOpenJobGarageMenu;

	private DateTime lastAttemptT;

	private Menu jobGarageMenu;

	private static readonly Menu.ButtonPressHandler searchButtonPressHandler = new Menu.ButtonPressHandler((Control)206, Menu.ControlPressCheckType.JUST_PRESSED, async delegate(Menu menu, Control control)
	{
		await VehiclesMenuScript.SearchVehicles(menu);
	}, disableControl: true);

	private static ICollection<JobGarage> CurrentJobGarages => JobGarages.GetJobGaragesByJobEnum(Gtacnr.Client.API.Jobs.CachedJobEnum);

	public JobGarageScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		RefreshBlips();
	}

	protected override void OnStarted()
	{
		CreateMenu();
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(1000);
		float num = 2500f;
		currentJobGarage = null;
		canOpenJobGarageMenu = false;
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null && ((Entity)Game.PlayerPed).IsAlive)
		{
			foreach (JobGarage currentJobGarage in CurrentJobGarages)
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				float num2 = ((Vector3)(ref position)).DistanceToSquared(currentJobGarage.OnFootPosition);
				if (num2 < num)
				{
					num = num2;
					this.currentJobGarage = currentJobGarage;
					canOpenJobGarageMenu = num <= 2.25f;
				}
			}
		}
		if (canOpenJobGarageMenu && !couldOpenJobGarageMenu)
		{
			EnableControls();
		}
		else if (!canOpenJobGarageMenu && couldOpenJobGarageMenu)
		{
			DisableControls();
		}
		if (this.currentJobGarage != null && previousJobGarage == null)
		{
			EnableDrawTask();
		}
		if (this.currentJobGarage == null && previousJobGarage != null)
		{
			DisableDrawTask();
		}
		couldOpenJobGarageMenu = canOpenJobGarageMenu;
		previousJobGarage = this.currentJobGarage;
	}

	private void RefreshBlips()
	{
		DeleteBlips();
		CreateBlips();
	}

	private void CreateBlips()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		foreach (JobGarage currentJobGarage in CurrentJobGarages)
		{
			Blip val = World.CreateBlip(currentJobGarage.OnFootPosition);
			val.Sprite = (BlipSprite)524;
			val.Scale = 0.7f;
			string job = currentJobGarage.Job;
			string name;
			if (!(job == "police"))
			{
				if (job == "paramedic")
				{
					name = "EMS Garage";
					val.Color = (BlipColor)50;
				}
				else
				{
					name = "Job Garage";
				}
			}
			else
			{
				name = "Police Garage";
				val.Color = (BlipColor)3;
			}
			Utils.SetBlipName(val, name, "job_garage");
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

	private void EnableControls()
	{
		KeysScript.AttachListener((Control)51, OnKeyEvent, 10);
		Utils.AddInstructionalButton("jobGarage", new InstructionalButton("Job Garage", 2, (Control)51));
	}

	private void DisableControls()
	{
		KeysScript.DetachListener((Control)51, OnKeyEvent);
		Utils.RemoveInstructionalButton("jobGarage");
	}

	private void EnableDrawTask()
	{
		base.Update += DrawTask;
	}

	private void DisableDrawTask()
	{
		base.Update -= DrawTask;
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (canOpenJobGarageMenu && !jobGarageMenu.Visible && eventType == KeyEventType.JustPressed)
		{
			OpenMenu();
			return true;
		}
		return false;
	}

	private async Coroutine DrawTask()
	{
		if (currentJobGarage != null)
		{
			Vector3 onFootPosition = currentJobGarage.OnFootPosition;
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(onFootPosition.X, onFootPosition.Y, onFootPosition.Z + 0.5f, ref z, false))
			{
				onFootPosition.Z = z;
			}
			World.DrawMarker((MarkerType)1, onFootPosition, Vector3.Zero, Vector3.Zero, new Vector3(1f, 1f, 0.75f), System.Drawing.Color.FromArgb(-2135228416), false, false, false, (string)null, (string)null, false);
		}
	}

	private void CreateMenu()
	{
		jobGarageMenu = new Menu(LocalizationController.S(Entries.Jobs.JOB_GARAGE), LocalizationController.S(Entries.Jobs.MENU_JOB_GARAGE_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		jobGarageMenu.OnItemSelect += OnMenuItemSelect;
		jobGarageMenu.InstructionalButtons.Add((Control)206, LocalizationController.S(Entries.Main.BTN_SEARCH));
		jobGarageMenu.InstructionalButtons.Add((Control)204, LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_FAVORITE_BUTTON));
		jobGarageMenu.InstructionalButtons.Add((Control)327, LocalizationController.S(Entries.Main.BTN_REFRESH));
		jobGarageMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)204, Menu.ControlPressCheckType.JUST_PRESSED, VehiclesMenuScript.FavouriteButtonHandler, disableControl: true));
		jobGarageMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)327, Menu.ControlPressCheckType.JUST_PRESSED, delegate(Menu menu, Control control)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			VehiclesMenuScript.RefreshButtonHandler(menu, control, delegate
			{
				RefreshMenu();
			});
		}, disableControl: true));
		jobGarageMenu.OnMenuOpen += OnMenuOpen;
		jobGarageMenu.OnMenuClose += OnMenuClose;
		MenuController.AddMenu(jobGarageMenu);
	}

	private async void OnMenuOpen(Menu menu)
	{
		await BaseScript.Delay(500);
		jobGarageMenu.ButtonPressHandlers.Add(searchButtonPressHandler);
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		jobGarageMenu.ButtonPressHandlers.Remove(searchButtonPressHandler);
	}

	private void OpenMenu()
	{
		if (!jobGarageMenu.Visible)
		{
			jobGarageMenu.OpenMenu();
			RefreshMenu();
		}
	}

	private async void RefreshMenu()
	{
		jobGarageMenu.ClearMenuItems();
		jobGarageMenu.ResetFilter();
		if (currentJobGarage == null)
		{
			return;
		}
		jobGarageMenu.AddLoadingMenuItem();
		if (VehiclesMenuScript.VehicleCache == null)
		{
			await VehiclesMenuScript.EnsureVehicleCache();
		}
		jobGarageMenu.ClearMenuItems();
		IEnumerable<StoredVehicle> enumerable = VehiclesMenuScript.VehicleCache.Where((StoredVehicle v) => v.Job == Gtacnr.Client.API.Jobs.CachedJob);
		foreach (StoredVehicle item in enumerable)
		{
			try
			{
				MenuItem menuItem = item.ToMenuItem();
				if (VehiclesMenuScript.IsVehicleFavorite(item.LicensePlate))
				{
					menuItem.RightIcon = MenuItem.Icon.MISSION_STAR;
				}
				jobGarageMenu.AddMenuItem(menuItem);
			}
			catch (Exception ex)
			{
				Print(ex);
				jobGarageMenu.AddErrorMenuItem(ex);
			}
		}
		if (enumerable.Count() == 0)
		{
			jobGarageMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Jobs.MENU_JOB_GARAGE_EMPTY_TEXT))
			{
				Description = LocalizationController.S(Entries.Jobs.MENU_JOB_GARAGE_EMPTY_DESCRIPTION),
				Enabled = false
			});
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		object itemData = menuItem.ItemData;
		if (!(itemData is StoredVehicle storedVehicle))
		{
			return;
		}
		try
		{
			if (!Gtacnr.Utils.CheckTimePassed(lastAttemptT, 2000.0))
			{
				Utils.PlayErrorSound();
				return;
			}
			lastAttemptT = DateTime.UtcNow;
			MenuController.CloseAllMenus();
			Vehicle activeVehicle = ActiveVehicleScript.ActiveVehicle;
			if ((Entity)(object)activeVehicle == (Entity)(object)TruckerJobScript.DeliveryVehicle && (Entity)(object)activeVehicle != (Entity)null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_SUMMON_VEHICLE_WHEN_PART_OF_MISSION));
				return;
			}
			string text = ((!((Entity)(object)activeVehicle != (Entity)null)) ? null : ActiveVehicleScript.ActiveVehicleHealthData?.Json());
			if (!VehiclesMenuScript.ResolveSummonVehicleResponse((SummonVehicleResponse)(await TriggerServerEventAsync<int>("gtacnr:jobGarages:obtainVehicle", new object[3] { currentJobGarage.Id, storedVehicle.Id, text })), 66))
			{
				return;
			}
			VehiclesMenuScript.InvalidateCache();
			string vehicleFullName = Utils.GetVehicleFullName(storedVehicle.Model);
			await Utils.FadeOut();
			Vehicle vehicle = await Utils.CreateStoredVehicle(storedVehicle, currentJobGarage.VehiclePosition, currentJobGarage.VehicleHeading);
			int attempts = 1;
			if ((Entity)(object)vehicle == (Entity)null)
			{
				while (true)
				{
					Utils.DisplayHelpText($"Unable to download the ~y~vehicle model~s~. Retrying ({attempts}/{3})...");
					await BaseScript.Delay(5000);
					vehicle = await Utils.CreateStoredVehicle(storedVehicle, currentJobGarage.VehiclePosition, currentJobGarage.VehicleHeading);
					if ((Entity)(object)vehicle != (Entity)null)
					{
						break;
					}
					attempts++;
					if (attempts > 3)
					{
						Utils.DisplayHelpText("~r~Unable to load the vehicle model (0x5C-3). Please, try again later later.");
						return;
					}
				}
			}
			vehicle.LockStatus = (VehicleLockStatus)10;
			vehicle.CanBeVisiblyDamaged = false;
			if (!(await AntiEntitySpawnScript.RegisterEntity((Entity)(object)vehicle)))
			{
				((PoolObject)vehicle).Delete();
				Utils.DisplayHelpText("~r~Unable to obtain vehicle network id (0x5C-4). Please, try again later later.");
				return;
			}
			Game.PlayerPed.Task.WarpIntoVehicle(vehicle, (VehicleSeat)(-1));
			await Utils.FadeIn();
			Utils.DisplaySubtitle("Your ~b~" + vehicleFullName + " ~s~is ready!");
			if (!(await ActiveVehicleScript.SetActiveVehicle(storedVehicle)))
			{
				Utils.DisplayErrorMessage(66, 3, "Unable to set active vehicle.");
			}
			vehicle.LockStatus = (VehicleLockStatus)2;
			vehicle.CanBeVisiblyDamaged = true;
		}
		catch (Exception exception)
		{
			Print(exception);
			Utils.DisplayErrorMessage(66, 2, LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
		}
	}
}
