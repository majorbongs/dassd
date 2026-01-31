using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.HUD;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Client.Zones;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using NativeUI;

namespace Gtacnr.Client.Items;

public class C4Script : Script
{
	public C4Script()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		ShuffleSeatScript.SeatShuffled = (EventHandler<VehicleEventArgs>)Delegate.Combine(ShuffleSeatScript.SeatShuffled, new EventHandler<VehicleEventArgs>(OnEnteredVehicle));
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Invalid comparison between Unknown and I4
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null || !itemDefinition.HasExtraData("IsC4Bomb") || !itemDefinition.GetExtraDataBool("IsC4Bomb"))
		{
			return;
		}
		if (amount != 1f)
		{
			Utils.SendNotification("You can only use one ~r~" + itemDefinition.Name + " ~s~at a time.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (InventoryMenuScript.Cache != null)
		{
			InventoryEntry inventoryEntry = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemId);
			if (inventoryEntry == null || inventoryEntry.Amount < 1f)
			{
				Utils.SendNotification("You don't have a ~r~" + itemDefinition.Name + "~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
		}
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
			Utils.SendNotification("You can't use a ~r~" + itemDefinition.Name + " ~s~as a " + jobData.GetColoredName() + ".");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (CuffedScript.IsInCustody)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_USE_WHEN_IN_CUSTODY, itemDefinition.Name));
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (SafezoneScript.Current != null)
		{
			Utils.SendNotification("You can't plant a ~r~" + itemDefinition.Name + " ~s~when you're in a ~g~Safe Zone~s~.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (!Game.PlayerPed.IsInVehicle())
		{
			Utils.SendNotification("You can only plant a ~r~" + itemDefinition.Name + " ~s~when you're in a vehicle.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if ((int)Game.PlayerPed.SeatIndex != -1)
		{
			Utils.SendNotification("You can only plant a ~r~" + itemDefinition.Name + " ~s~when you're in the driver seat.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (currentVehicle.ClassType.In((VehicleClass[])(object)new VehicleClass[2]
		{
			(VehicleClass)13,
			(VehicleClass)8
		}))
		{
			Utils.SendNotification("You cannot plant a ~r~" + itemDefinition.Name + " ~s~on this type of vehicle.");
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
		else if (currentVehicle.IsEngineRunning)
		{
			Utils.SendNotification("You must ~r~turn off ~s~the vehicle's engine before planting a ~r~" + itemDefinition.Name + "~s~.");
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
		else if ((LatentVehicleStateScript.Get(((Entity)currentVehicle).NetworkId)?.BombCharges ?? 0) > 0)
		{
			Utils.SendNotification("This ~b~vehicle ~s~has already a ~r~bomb ~s~planted in it.");
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
	}

	[EventHandler("gtacnr:inventories:usedItem")]
	private async void OnUsedItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition != null && itemDefinition.HasExtraData("IsC4Bomb") && itemDefinition.GetExtraDataBool("IsC4Bomb"))
		{
			await BaseScript.Delay(1000);
			await WaitForIgnition();
		}
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if ((int)e.Seat == -1)
		{
			await WaitForIgnition();
		}
	}

	private async Task WaitForIgnition()
	{
		Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)vehicle == (Entity)null)
		{
			return;
		}
		VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId);
		byte charges = vehicleState?.BombCharges ?? 0;
		if (charges == 0 || (vehicleState?.BombStarted ?? false))
		{
			return;
		}
		while (!vehicle.IsEngineStarting && !vehicle.IsEngineRunning)
		{
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)(object)vehicle)
			{
				return;
			}
			await BaseScript.Delay(250);
		}
		StartBombFuse(vehicle, charges);
	}

	private async void StartBombFuse(Vehicle vehicle, int charges)
	{
		if ((Entity)(object)vehicle == (Entity)null || SafezoneScript.Current != null)
		{
			return;
		}
		Utils.DisplayHelpText("~p~Run for your life! ~s~This ~b~vehicle ~s~has a ~r~bomb ~s~that's about to go off.", playSound: false, 3000);
		TextTimerBar fuseBar = new TextTimerBar("BOMB", "")
		{
			TextColor = TextColors.Red
		};
		TimerBarScript.AddTimerBar(fuseBar);
		BaseScript.TriggerServerEvent("gtacnr:carbomb:triggered", new object[0]);
		try
		{
			for (int i = 3; i > 0; i--)
			{
				fuseBar.Text = $"00:{i:00}";
				Utils.ShakeGamepad(200);
				await BaseScript.Delay(1000);
				if (SafezoneScript.Current != null)
				{
					Utils.DisplayHelpText("~r~The bomb has been defused because you entered a ~s~Safe Zone~r~.", playSound: false, 5000);
					return;
				}
			}
			BlowUpVehicle(vehicle, charges);
		}
		finally
		{
			TimerBarScript.RemoveTimerBar(fuseBar);
		}
	}

	private void BlowUpVehicle(Vehicle vehicle, int charges)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		ExplosionType val = (ExplosionType)(charges switch
		{
			10 => 2, 
			20 => 31, 
			_ => 0, 
		});
		World.AddExplosion(new Vector3(((Entity)vehicle).Position.X, ((Entity)vehicle).Position.Y, ((Entity)vehicle).Position.Z - 0.1f), val, (float)charges * 0.8f, (float)charges * 0.9f, Game.PlayerPed, true, false);
		((Entity)vehicle).IsExplosionProof = false;
		vehicle.Explode();
		vehicle.ExplodeNetworked();
	}
}
