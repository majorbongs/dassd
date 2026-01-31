using System;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.HUD;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.Vehicles.Fuel;

public class JerrycanScript : Script
{
	private bool drawRefuelMarker;

	private bool canRefuel;

	private bool couldRefuel;

	private bool isRefueling;

	private bool isFinishingRefuel;

	private Vehicle targetVehicle;

	private Vector3 fuelCapPosition;

	private BarTimerBar tankBar;

	private BarTimerBar jerrycanBar;

	private float totalAddedGal;

	[Update]
	private async Coroutine CheckTask()
	{
		try
		{
			await Script.Wait(250);
			canRefuel = false;
			drawRefuelMarker = false;
			if (isFinishingRefuel || (int)Game.PlayerPed.Weapons.Current.Hash != 883325847)
			{
				return;
			}
			Vehicle lastVehicle = Game.PlayerPed.LastVehicle;
			if ((Entity)(object)lastVehicle == (Entity)null || DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)lastVehicle).Model)).IsElectric)
			{
				return;
			}
			targetVehicle = lastVehicle;
			fuelCapPosition = Utils.GetVehicleFuelCapPos(lastVehicle);
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared(fuelCapPosition);
			drawRefuelMarker = num < 64f;
			canRefuel = num < 1.9599999f;
			if (isRefueling && tankBar != null)
			{
				float num2 = 0.125f;
				float num3 = Utils.GetVehicleFuel(lastVehicle) + totalAddedGal;
				float num4 = Utils.GetVehicleTankCapacityL(lastVehicle) * GasScript.GALLONS_PER_LITER;
				if (num3 + num2 > num4)
				{
					num2 = (float)Math.Round(num4 - num3, 3);
				}
				float num5 = (float)API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, -899475295) * GasScript.JERRYCAN_CAPACITY / 1000f - totalAddedGal;
				if (num5 - num2 < 0f)
				{
					num2 = (float)Math.Round(num5, 3);
				}
				if (num2 == 0f)
				{
					StopRefueling();
				}
				totalAddedGal += num2;
				UpdateFuelBars(targetVehicle);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			UpdateCanRefuelState();
		}
	}

	[Update]
	private async Coroutine UpdateTask()
	{
		try
		{
			if (drawRefuelMarker && !isRefueling)
			{
				World.DrawMarker((MarkerType)1, fuelCapPosition, Vector3.Zero, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.4f), System.Drawing.Color.FromArgb(-2136299008), false, false, false, (string)null, (string)null, false);
			}
			if (canRefuel || isFinishingRefuel)
			{
				API.DisableControlAction(0, 24, true);
				API.DisableControlAction(0, 257, true);
			}
			if (isRefueling && !Game.IsDisabledControlPressed(2, (Control)24))
			{
				StopRefueling();
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void UpdateCanRefuelState()
	{
		if (!couldRefuel && canRefuel)
		{
			EnableControls();
		}
		else if (couldRefuel && !canRefuel)
		{
			DisableControls();
		}
		couldRefuel = canRefuel;
	}

	private void ShowFuelBars(Vehicle vehicle)
	{
		HideFuelBars();
		tankBar = new BarTimerBar("VEHICLE")
		{
			Percentage = 0f
		};
		jerrycanBar = new BarTimerBar("JERRYCAN")
		{
			Percentage = 0f
		};
		UpdateFuelBars(vehicle);
		TimerBarScript.AddTimerBar(tankBar);
		TimerBarScript.AddTimerBar(jerrycanBar);
	}

	private void HideFuelBars()
	{
		if (tankBar != null)
		{
			TimerBarScript.RemoveTimerBar(tankBar);
			tankBar = null;
		}
		if (jerrycanBar != null)
		{
			TimerBarScript.RemoveTimerBar(jerrycanBar);
			jerrycanBar = null;
		}
	}

	private void UpdateFuelBars(Vehicle vehicle)
	{
		if (tankBar != null && jerrycanBar != null)
		{
			float num = Utils.GetVehicleFuel(vehicle) + totalAddedGal;
			float num2 = Utils.GetVehicleTankCapacityL(vehicle) * GasScript.GALLONS_PER_LITER;
			float percentage = num / num2;
			float percentage2 = ((float)API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, -899475295) * GasScript.JERRYCAN_CAPACITY / 1000f - totalAddedGal) / GasScript.JERRYCAN_CAPACITY;
			tankBar.Percentage = percentage;
			jerrycanBar.Percentage = percentage2;
			tankBar.Color = ((tankBar.Percentage >= 0.99f) ? BarColors.Green : ((tankBar.Percentage > 0.15f) ? BarColors.White : BarColors.Red));
			tankBar.TextColor = ((tankBar.Percentage >= 0.99f) ? TextColors.Green : ((tankBar.Percentage > 0.15f) ? TextColors.White : TextColors.Red));
			jerrycanBar.Color = ((jerrycanBar.Percentage >= 0.99f) ? BarColors.Green : ((jerrycanBar.Percentage > 0.15f) ? BarColors.White : BarColors.Red));
			jerrycanBar.TextColor = ((jerrycanBar.Percentage >= 0.99f) ? TextColors.Green : ((jerrycanBar.Percentage > 0.15f) ? TextColors.White : TextColors.Red));
		}
	}

	private void EnableControls()
	{
		KeysScript.AttachListener((Control)24, OnKeyEvent);
		Utils.AddInstructionalButton("useJerrycan", new Gtacnr.Client.API.UI.InstructionalButton("Refuel (hold)", 2, (Control)24));
	}

	private void DisableControls()
	{
		KeysScript.DetachListener((Control)24, OnKeyEvent);
		Utils.RemoveInstructionalButton("useJerrycan");
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (checkInput())
		{
			StartRefueling();
			return true;
		}
		return false;
		bool checkInput()
		{
			if (eventType == KeyEventType.Held)
			{
				return !MenuController.IsAnyMenuOpen();
			}
			return false;
		}
	}

	private async void StartRefueling()
	{
		isRefueling = true;
		totalAddedGal = 0f;
		DisableControls();
		Utils.AddInstructionalButton("useJerrycan", new Gtacnr.Client.API.UI.InstructionalButton("Stop (release)", 2, (Control)24));
		Utils.PlayContinueSound();
		API.TaskTurnPedToFaceEntity(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)targetVehicle).Handle, 1000);
		await BaseScript.Delay(1000);
		Game.PlayerPed.Task.PlayAnimation("weapon@w_sp_jerrycan", "fire_intro", 8f, -1, (AnimationFlags)0);
		await BaseScript.Delay(1000);
		Game.PlayerPed.Task.PlayAnimation("weapon@w_sp_jerrycan", "fire", 50f, -1, (AnimationFlags)1);
		ShowFuelBars(targetVehicle);
	}

	private async void StopRefueling()
	{
		isRefueling = false;
		isFinishingRefuel = true;
		EnableControls();
		Utils.PlayContinueSound();
		API.StopEntityAnim(((PoolObject)Game.PlayerPed).Handle, "weapon@w_sp_jerrycan", "fire_intro", 1f);
		API.StopEntityAnim(((PoolObject)Game.PlayerPed).Handle, "weapon@w_sp_jerrycan", "fire", 1f);
		API.StopEntityAnim(((PoolObject)Game.PlayerPed).Handle, "weapon@w_sp_jerrycan", "fire_outro", 1f);
		Game.PlayerPed.Task.PlayAnimation("weapon@w_sp_jerrycan", "fire_outro", 8f, -1, (AnimationFlags)128);
		float vehicleTankCapacityL = Utils.GetVehicleTankCapacityL(targetVehicle);
		string text = Utils.GetVehicleHealthData(targetVehicle).Json();
		BaseScript.TriggerServerEvent("gtacnr:fuel:usedJerrycan", new object[4]
		{
			totalAddedGal,
			((Entity)targetVehicle).NetworkId,
			vehicleTankCapacityL,
			text
		});
		await BaseScript.Delay(3000);
		HideFuelBars();
		isFinishingRefuel = false;
	}
}
