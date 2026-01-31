using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Events.Holidays.Halloween;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class DisableMountedGunsScript : Script
{
	private static readonly List<int> weapons = new List<int>
	{
		API.GetHashKey("VEHICLE_WEAPON_ROTORS"),
		API.GetHashKey("VEHICLE_WEAPON_TANK"),
		API.GetHashKey("VEHICLE_WEAPON_SEARCHLIGHT"),
		API.GetHashKey("VEHICLE_WEAPON_RADAR"),
		API.GetHashKey("VEHICLE_WEAPON_PLAYER_BULLET"),
		API.GetHashKey("VEHICLE_WEAPON_PLAYER_LAZER"),
		API.GetHashKey("VEHICLE_WEAPON_ENEMY_LASER"),
		API.GetHashKey("VEHICLE_WEAPON_PLAYER_BUZZARD"),
		API.GetHashKey("VEHICLE_WEAPON_PLAYER_HUNTER"),
		API.GetHashKey("VEHICLE_WEAPON_PLANE_ROCKET"),
		API.GetHashKey("VEHICLE_WEAPON_SPACE_ROCKET"),
		API.GetHashKey("VEHICLE_WEAPON_TURRET_INSURGENT"),
		API.GetHashKey("VEHICLE_WEAPON_PLAYER_SAVAGE"),
		API.GetHashKey("VEHICLE_WEAPON_TURRET_TECHNICAL"),
		API.GetHashKey("VEHICLE_WEAPON_NOSE_TURRET_VALKYRIE"),
		API.GetHashKey("VEHICLE_WEAPON_TURRET_VALKYRIE"),
		API.GetHashKey("VEHICLE_WEAPON_CANNON_BLAZER"),
		API.GetHashKey("VEHICLE_WEAPON_TURRET_BOXVILLE"),
		API.GetHashKey("VEHICLE_WEAPON_RUINER_BULLET"),
		API.GetHashKey("VEHICLE_WEAPON_RUINER_ROCKET"),
		API.GetHashKey("VEHICLE_WEAPON_HUNTER_MG"),
		API.GetHashKey("VEHICLE_WEAPON_HUNTER_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_HUNTER_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_HUNTER_BARRAGE"),
		API.GetHashKey("VEHICLE_WEAPON_TULA_NOSEMG"),
		API.GetHashKey("VEHICLE_WEAPON_TULA_MG"),
		API.GetHashKey("VEHICLE_WEAPON_TULA_DUALMG"),
		API.GetHashKey("VEHICLE_WEAPON_TULA_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_SEABREEZE_MG"),
		API.GetHashKey("VEHICLE_WEAPON_MICROLIGHT_MG"),
		API.GetHashKey("VEHICLE_WEAPON_DOGFIGHTER_MG"),
		API.GetHashKey("VEHICLE_WEAPON_DOGFIGHTER_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_MOGUL_NOSE"),
		API.GetHashKey("VEHICLE_WEAPON_MOGUL_DUALNOSE"),
		API.GetHashKey("VEHICLE_WEAPON_MOGUL_TURRET"),
		API.GetHashKey("VEHICLE_WEAPON_MOGUL_DUALTURRET"),
		API.GetHashKey("VEHICLE_WEAPON_ROGUE_MG"),
		API.GetHashKey("VEHICLE_WEAPON_ROGUE_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_ROGUE_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_BOMBUSHKA_DUALMG"),
		API.GetHashKey("VEHICLE_WEAPON_BOMBUSHKA_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_HAVOK_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_VIGILANTE_MG"),
		API.GetHashKey("VEHICLE_WEAPON_VIGILANTE_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_TURRET_LIMO"),
		API.GetHashKey("VEHICLE_WEAPON_DUNE_MG"),
		API.GetHashKey("VEHICLE_WEAPON_DUNE_GRENADELAUNCHER"),
		API.GetHashKey("VEHICLE_WEAPON_DUNE_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_TAMPA_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_TAMPA_MORTAR"),
		API.GetHashKey("VEHICLE_WEAPON_TAMPA_FIXEDMINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_TAMPA_DUALMINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_HALFTRACK_DUALMG"),
		API.GetHashKey("VEHICLE_WEAPON_HALFTRACK_QUADMG"),
		API.GetHashKey("VEHICLE_WEAPON_APC_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_APC_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_APC_MG"),
		API.GetHashKey("VEHICLE_WEAPON_ARDENT_MG"),
		API.GetHashKey("VEHICLE_WEAPON_TECHNICAL_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_INSURGENT_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_TRAILER_QUADMG"),
		API.GetHashKey("VEHICLE_WEAPON_TRAILER_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_TRAILER_DUALAA"),
		API.GetHashKey("VEHICLE_WEAPON_NIGHTSHARK_MG"),
		API.GetHashKey("VEHICLE_WEAPON_OPPRESSOR_MG"),
		API.GetHashKey("VEHICLE_WEAPON_OPPRESSOR_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_MOBILEOPS_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_AKULA_TURRET_SINGLE"),
		API.GetHashKey("VEHICLE_WEAPON_AKULA_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_AKULA_TURRET_DUAL"),
		API.GetHashKey("VEHICLE_WEAPON_AKULA_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_AKULA_BARRAGE"),
		API.GetHashKey("VEHICLE_WEAPON_AVENGER_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_BARRAGE_TOP_MG"),
		API.GetHashKey("VEHICLE_WEAPON_BARRAGE_TOP_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_BARRAGE_REAR_MG"),
		API.GetHashKey("VEHICLE_WEAPON_BARRAGE_REAR_MINIGUN"),
		API.GetHashKey("VEHICLE_WEAPON_BARRAGE_REAR_GL"),
		API.GetHashKey("VEHICLE_WEAPON_CHERNO_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_COMET_MG"),
		API.GetHashKey("VEHICLE_WEAPON_DELUXO_MG"),
		API.GetHashKey("VEHICLE_WEAPON_DELUXO_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_KHANJALI_CANNON"),
		API.GetHashKey("VEHICLE_WEAPON_KHANJALI_CANNON_HEAVY"),
		API.GetHashKey("VEHICLE_WEAPON_KHANJALI_MG"),
		API.GetHashKey("VEHICLE_WEAPON_KHANJALI_GL"),
		API.GetHashKey("VEHICLE_WEAPON_REVOLTER_MG"),
		API.GetHashKey("VEHICLE_WEAPON_SAVESTRA_MG"),
		API.GetHashKey("VEHICLE_WEAPON_SUBCAR_MG"),
		API.GetHashKey("VEHICLE_WEAPON_SUBCAR_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_SUBCAR_TORPEDO"),
		API.GetHashKey("VEHICLE_WEAPON_THRUSTER_MG"),
		API.GetHashKey("VEHICLE_WEAPON_THRUSTER_MISSILE"),
		API.GetHashKey("VEHICLE_WEAPON_VISERIS_MG"),
		API.GetHashKey("VEHICLE_WEAPON_VOLATOL_DUALMG")
	};

	private static readonly List<int> halloweenWeapons = new List<int> { -1291819974 };

	private static bool AreWeaponsAllowed(Vehicle vehicle)
	{
		if (ModeratorMenuScript.IsOnDuty)
		{
			return true;
		}
		if ((int)StaffLevelScript.StaffLevel >= 10 && ((Entity)vehicle).Model == Model.op_Implicit("sled"))
		{
			return true;
		}
		return false;
	}

	protected override void OnStarted()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		ShuffleSeatScript.SeatShuffled = (EventHandler<VehicleEventArgs>)Delegate.Combine(ShuffleSeatScript.SeatShuffled, new EventHandler<VehicleEventArgs>(OnSeatShuffled));
	}

	private async void OnSeatShuffled(object sender, VehicleEventArgs e)
	{
		if (!AreWeaponsAllowed(e.Vehicle))
		{
			DateTime start = DateTime.UtcNow;
			while (!Gtacnr.Utils.CheckTimePassed(start, 2000.0))
			{
				await BaseScript.Delay(50);
				DisableMountedGuns(e.Vehicle);
			}
		}
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if (AreWeaponsAllowed(e.Vehicle))
		{
			return;
		}
		DisableMountedGuns(e.Vehicle);
		if ((int)Game.PlayerPed.SeatIndex == -1)
		{
			return;
		}
		VehicleSeat targetSeat = (VehicleSeat)API.GetSeatPedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle);
		if ((int)targetSeat == -1)
		{
			VehicleSeat seatIndex;
			while ((int)(seatIndex = Game.PlayerPed.SeatIndex) != -3 && seatIndex != targetSeat)
			{
				await BaseScript.Delay(50);
			}
			if (seatIndex == targetSeat)
			{
				DisableMountedGuns(e.Vehicle);
			}
		}
	}

	public static void DisableMountedGuns(Vehicle vehicle)
	{
		foreach (int weapon in weapons)
		{
			API.DisableVehicleWeapon(true, (uint)weapon, ((PoolObject)vehicle).Handle, ((PoolObject)Game.PlayerPed).Handle);
		}
		if (HalloweenScript.IsHalloween)
		{
			return;
		}
		foreach (int halloweenWeapon in halloweenWeapons)
		{
			API.DisableVehicleWeapon(true, (uint)halloweenWeapon, ((PoolObject)vehicle).Handle, ((PoolObject)Game.PlayerPed).Handle);
		}
	}
}
