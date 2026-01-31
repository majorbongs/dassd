using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Vehicles.Behaviors;

namespace Gtacnr.Model;

public class VehicleModData
{
	public int PrimaryColor { get; set; }

	public int SecondaryColor { get; set; }

	public int TrimColor { get; set; }

	public int DashboardColor { get; set; }

	public int PearlescentColor { get; set; }

	public int Livery { get; set; }

	public Dictionary<int, int> Mods { get; set; } = new Dictionary<int, int>();

	public int WindowTint { get; set; }

	public static VehicleModData FromVehicle(Vehicle vehicle)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected I4, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected I4, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected I4, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected I4, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected I4, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected I4, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected I4, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected I4, but got Unknown
		VehicleModData vehicleModData = new VehicleModData
		{
			PrimaryColor = (int)vehicle.Mods.PrimaryColor,
			SecondaryColor = (int)vehicle.Mods.SecondaryColor,
			TrimColor = (int)vehicle.Mods.TrimColor,
			DashboardColor = (int)vehicle.Mods.DashboardColor,
			PearlescentColor = (int)vehicle.Mods.PearlescentColor,
			Livery = vehicle.Mods.Livery,
			WindowTint = (int)vehicle.Mods.WindowTint
		};
		foreach (VehicleModType value in Enum.GetValues(typeof(VehicleModType)))
		{
			int vehicleMod = API.GetVehicleMod(((PoolObject)vehicle).Handle, (int)value);
			vehicleModData.Mods[(int)value] = vehicleMod;
		}
		return vehicleModData;
	}

	public void ApplyOnVehicle(Vehicle vehicle)
	{
		if ((Entity)(object)vehicle == (Entity)null || !vehicle.Exists())
		{
			return;
		}
		bool vehicleModVariation = API.GetVehicleModVariation(((PoolObject)vehicle).Handle, 23);
		API.SetVehicleModKit(((PoolObject)vehicle).Handle, 0);
		vehicle.Mods.PrimaryColor = (VehicleColor)PrimaryColor;
		vehicle.Mods.SecondaryColor = (VehicleColor)SecondaryColor;
		vehicle.Mods.TrimColor = (VehicleColor)TrimColor;
		vehicle.Mods.DashboardColor = (VehicleColor)DashboardColor;
		vehicle.Mods.PearlescentColor = (VehicleColor)PearlescentColor;
		vehicle.Mods.Livery = Livery;
		vehicle.Mods.WindowTint = (VehicleWindowTint)WindowTint;
		foreach (int key in Mods.Keys)
		{
			int num = Mods[key];
			API.SetVehicleMod(((PoolObject)vehicle).Handle, key, num, vehicleModVariation);
		}
		DisableMountedGunsScript.DisableMountedGuns(vehicle);
	}
}
