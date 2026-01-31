using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Admin;

public class ModeratorVehiclesScript : Script
{
	private static readonly int[] SantaSkins = new List<string> { "Santaclaus", "Mrsclaus" }.Select((string i) => API.GetHashKey(i)).ToArray();

	private Vehicle staffVehicle;

	public ModeratorVehiclesScript()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		ShuffleSeatScript.SeatShuffled = (EventHandler<VehicleEventArgs>)Delegate.Combine(ShuffleSeatScript.SeatShuffled, new EventHandler<VehicleEventArgs>(OnEnteredVehicle));
		ModeratorMenuScript.ModeratorCommandsRegistered = (EventHandler<EventArgs>)Delegate.Combine(ModeratorMenuScript.ModeratorCommandsRegistered, new EventHandler<EventArgs>(OnModeratorCommandsRegistered));
	}

	private static void SetStaffVehicleLivery(Vehicle vehicle)
	{
		if (((Entity)vehicle).Model == Model.op_Implicit("staffbufsx") || ((Entity)vehicle).Model == Model.op_Implicit("staffglx") || ((Entity)vehicle).Model == Model.op_Implicit("staffcara"))
		{
			if ((int)StaffLevelScript.StaffLevel >= 130)
			{
				vehicle.Mods.Livery = 3;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 120)
			{
				vehicle.Mods.Livery = 2;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 115)
			{
				vehicle.Mods.Livery = 4;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 110)
			{
				vehicle.Mods.Livery = 1;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 100)
			{
				vehicle.Mods.Livery = 0;
			}
		}
		else if (((Entity)vehicle).Model == Model.op_Implicit("staffbuffalos") || ((Entity)vehicle).Model == Model.op_Implicit("staffgo4"))
		{
			if ((int)StaffLevelScript.StaffLevel >= 130)
			{
				vehicle.Mods.Livery = 3;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 120)
			{
				vehicle.Mods.Livery = 2;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 110)
			{
				vehicle.Mods.Livery = 1;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 100)
			{
				vehicle.Mods.Livery = 0;
			}
		}
		else if (((Entity)vehicle).Model == Model.op_Implicit("staffvigeror"))
		{
			if ((int)StaffLevelScript.StaffLevel >= 130)
			{
				vehicle.Mods.Livery = 1;
			}
			else if ((int)StaffLevelScript.StaffLevel >= 120)
			{
				vehicle.Mods.Livery = 0;
			}
		}
		else if (((Entity)vehicle).Model == Model.op_Implicit("testrbufsx"))
		{
			if (StaffLevelScript.StaffLevel == StaffLevel.TrialTester)
			{
				vehicle.Mods.Livery = 0;
			}
			else if (StaffLevelScript.StaffLevel == StaffLevel.Tester)
			{
				vehicle.Mods.Livery = 1;
			}
			else if (StaffLevelScript.StaffLevel == StaffLevel.LeadTester)
			{
				vehicle.Mods.Livery = 2;
			}
		}
	}

	[EventHandler("gtacnr:temp:spawnStaffVehicle")]
	private async void OnSpawnStaffVehicle(string vehicleName)
	{
		if (vehicleName == "sled" && !SantaSkins.Contains(((Entity)Game.PlayerPed).Model.Hash))
		{
			AntiHealthLockScript.JustHealed();
			await Game.Player.ChangeModel(Model.op_Implicit(API.GetHashKey("Santaclaus")));
		}
		if ((Entity)(object)staffVehicle != (Entity)null)
		{
			((PoolObject)staffVehicle).Delete();
			await BaseScript.Delay(100);
		}
		staffVehicle = await World.CreateVehicle(Model.op_Implicit(vehicleName), ((Entity)Game.PlayerPed).Position, ((Entity)Game.PlayerPed).Heading);
		if ((Entity)(object)staffVehicle == (Entity)null)
		{
			Utils.DisplayHelpText("~r~Staff vehicle (" + vehicleName + ") couldn't be spawned due to an error");
			return;
		}
		if (vehicleName != "gstturc1" && vehicleName != "testrbufsx")
		{
			staffVehicle.Mods.LicensePlate = "B4N Y0U";
			staffVehicle.Repair();
			staffVehicle.Wash();
			((Entity)staffVehicle).IsInvincible = true;
			staffVehicle.CanTiresBurst = false;
			SetStaffVehicleLivery(staffVehicle);
		}
		switch (vehicleName)
		{
		case "staffcara":
		case "staffegt":
			staffVehicle.ToggleExtra(1, true);
			break;
		case "stafflimo":
		case "staffrebla":
			((Entity)staffVehicle).IsInvincible = false;
			staffVehicle.CanTiresBurst = true;
			staffVehicle.Mods.WindowTint = (VehicleWindowTint)1;
			staffVehicle.Mods.PrimaryColor = (VehicleColor)0;
			staffVehicle.Mods.SecondaryColor = (VehicleColor)0;
			API.SetVehicleModKit(((PoolObject)staffVehicle).Handle, 0);
			API.SetVehicleMod(((PoolObject)staffVehicle).Handle, 11, 5, false);
			API.SetVehicleMod(((PoolObject)staffVehicle).Handle, 12, 5, false);
			API.SetVehicleMod(((PoolObject)staffVehicle).Handle, 13, 5, false);
			API.SetVehicleMod(((PoolObject)staffVehicle).Handle, 16, 5, false);
			break;
		}
		int handle = ((PoolObject)staffVehicle).Handle;
		API.SetEntityAsNoLongerNeeded(ref handle);
		Game.PlayerPed.Task.WarpIntoVehicle(staffVehicle, (VehicleSeat)(-1));
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)staffVehicle);
	}

	public static bool IsStaffVehicle(Vehicle vehicle)
	{
		return Constants.Staff.StaffVehicles.Contains(Model.op_Implicit(((Entity)vehicle).Model));
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if ((int)e.Seat != -1)
		{
			return;
		}
		if (Constants.Staff.StaffVehicles.Contains(Model.op_Implicit(((Entity)e.Vehicle).Model)))
		{
			if (StaffLevelScript.StaffLevel != StaffLevel.None && !StaffLevelScript.HasAnyTesterLevel && ((Entity)e.Vehicle).Model == Model.op_Implicit("testrbufsx"))
			{
				Utils.DisplayHelpText("~r~This vehicle is not available for your staff level.");
				Game.PlayerPed.Task.WarpOutOfVehicle(e.Vehicle);
				((PoolObject)e.Vehicle).Delete();
			}
			else if ((int)StaffLevelScript.StaffLevel >= 100)
			{
				SetStaffVehicleLivery(e.Vehicle);
			}
			else if ((int)StaffLevelScript.StaffLevel >= 10 && ((Entity)e.Vehicle).Model == Model.op_Implicit("sled"))
			{
				if (!SantaSkins.Contains(((Entity)Game.PlayerPed).Model.Hash))
				{
					AntiHealthLockScript.JustHealed();
					await Game.Player.ChangeModel(Model.op_Implicit(API.GetHashKey("Santaclaus")));
					Game.PlayerPed.Task.WarpIntoVehicle(e.Vehicle, (VehicleSeat)(-1));
				}
			}
			else if (StaffLevelScript.HasAnyTesterLevel && ((Entity)e.Vehicle).Model == Model.op_Implicit("testrbufsx"))
			{
				SetStaffVehicleLivery(e.Vehicle);
			}
			else if (!(((Entity)e.Vehicle).Model == Model.op_Implicit("gstturc1")) || !(LatentPlayers.Get(Game.Player.ServerId)?.Uid == "usr-KwIqr1AphkKdXkoSFxqPpQ"))
			{
				Utils.DisplayHelpText("~r~This vehicle can only be used by the staff.");
				Game.PlayerPed.Task.WarpOutOfVehicle(e.Vehicle);
				((PoolObject)e.Vehicle).Delete();
			}
		}
		else if (Constants.Staff.SharedStaffVehicles.Contains(Model.op_Implicit(((Entity)e.Vehicle).Model)))
		{
			if ((int)StaffLevelScript.StaffLevel >= 100 && ((Entity)e.Vehicle).Model != Model.op_Implicit("stafflimo") && ((Entity)e.Vehicle).Model != Model.op_Implicit("staffrebla"))
			{
				e.Vehicle.Repair();
				e.Vehicle.Wash();
				((Entity)e.Vehicle).IsInvincible = true;
				e.Vehicle.CanTiresBurst = false;
			}
			else
			{
				((Entity)e.Vehicle).IsInvincible = false;
				e.Vehicle.CanTiresBurst = true;
			}
		}
		else if (((Entity)e.Vehicle).Model == Model.op_Implicit("snowmobile"))
		{
			if ((int)StaffLevelScript.StaffLevel < 100)
			{
				((Entity)e.Vehicle).IsInvincible = false;
				e.Vehicle.CanTiresBurst = true;
			}
			else if ((Entity)(object)staffVehicle == (Entity)(object)e.Vehicle)
			{
				e.Vehicle.Repair();
				e.Vehicle.Wash();
				((Entity)e.Vehicle).IsInvincible = true;
				e.Vehicle.CanTiresBurst = false;
			}
		}
	}

	private void OnModeratorCommandsRegistered(object sender, EventArgs e)
	{
		Chat.AddSuggestion("/staff-buffalos", "Spawns the staff Buffalo S.");
		Chat.AddSuggestion("/staff-buffalosx", "Spawns the staff Buffalo SX.");
		Chat.AddSuggestion("/staff-glx", "Spawns the staff Granger GLX.");
		Chat.AddSuggestion("/staff-vigeror", "Spawns the staff Vigero R.");
		Chat.AddSuggestion("/staff-go4", "Spawns the staff GO-4.");
		Chat.AddSuggestion("/staff-partybus", "Spawns a Party Bus.");
		Chat.AddSuggestion("/staff-cara", "Spawns the staff Caracara.");
		Chat.AddSuggestion("/staff-potty", "Spawns a Potty.");
		Chat.AddSuggestion("/staff-couch", "Spawns a Couch.");
		Chat.AddSuggestion("/staff-limo", "Spawns a staff armoured Limousine.");
		Chat.AddSuggestion("/staff-rebla", "Spawns a staff Rebla.");
		Chat.AddSuggestion("/staff-bankbuf", "Spawns a staff Bank Buffalo.");
		Chat.AddSuggestion("/staff-stockade", "Spawns a staff Stockade.");
		if (StaffLevelScript.StaffLevel == StaffLevel.LeadModerator)
		{
			Chat.AddSuggestion("/staff-egt", "Spawns the Staff Omnis e-GT.");
		}
		Chat.AddSuggestion("/staff-snowmobile", "Spawns a Snowmobile.");
		Chat.AddSuggestion("/staff-cv22", "Spawns a CV-22 VTOL aircraft.");
		Chat.AddSuggestion("/staff-opp", "Spawns an Oppressor Mk2.");
	}
}
