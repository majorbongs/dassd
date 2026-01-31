using System;
using System.Collections.Generic;
using System.Drawing;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.IMenu;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Mechanic;

public class ImpoundLotScript : Script
{
	private List<Impound> impounds = Gtacnr.Utils.LoadJson<List<Impound>>("data/mechanic/impounds.json");

	private static VehicleServiceInfo serviceInfo = Gtacnr.Utils.LoadJson<VehicleServiceInfo>("data/vehicles/vehicleServices.json");

	private Impound currentImpound;

	private bool canOpenImpoundMenu;

	private Menu menu;

	private bool isBusy;

	protected override void OnStarted()
	{
		menu = new Menu(LocalizationController.S(Entries.Businesses.MENU_IMPOUND_TITLE), LocalizationController.S(Entries.Businesses.MENU_IMPOUND_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		menu.PlaySelectSound = false;
		menu.OnItemSelect += OnItemSelect;
	}

	private async void OpenMenu()
	{
		menu.OpenMenu();
		menu.ClearMenuItems();
		menu.AddLoadingMenuItem();
		string text = await TriggerServerEventAsync<string>("gtacnr:mechanic:getImpoundedVehicles", new object[0]);
		List<StoredVehicle> impoundedVehicles = ((text != null) ? text.Unjson<List<StoredVehicle>>() : new List<StoredVehicle>());
		long playerMoney = await Money.GetCachedBalanceOrFetch(AccountType.Bank);
		menu.ClearMenuItems();
		if (impoundedVehicles.Count > 0)
		{
			foreach (StoredVehicle item in impoundedVehicles)
			{
				string vehicleFullName = Utils.GetVehicleFullName(item.Model);
				DealershipSupply dealershipSupply = DealershipScript.FindFirstSupplyOfModel(item.Model);
				int price = ((dealershipSupply != null) ? serviceInfo.Tow.CalculatePrice(dealershipSupply.Price) : serviceInfo.Tow.DefaultPrice);
				menu.AddMenuItem(new MenuItem(vehicleFullName)
				{
					Description = "Release this vehicle from the impound. ~y~Cashless payments only.~s~",
					Label = price.ToPriceTagString(playerMoney),
					ItemData = item
				});
			}
			return;
		}
		menu.AddMenuItem(new MenuItem("No vehicles :)", "You don't have any impounded vehicle!"));
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		object itemData = menuItem.ItemData;
		if (!(itemData is StoredVehicle storedVehicle))
		{
			return;
		}
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		try
		{
			isBusy = true;
			Utils.PlaySelectSound();
			int num = await TriggerServerEventAsync<int>("gtacnr:mechanic:unimpound", new object[1] { storedVehicle.Id });
			if (num == 1)
			{
				string vehicleFullName = Utils.GetVehicleFullName(storedVehicle.Model);
				DealershipSupply dealershipSupply = DealershipScript.FindFirstSupplyOfModel(storedVehicle.Model);
				int amount = ((dealershipSupply != null) ? serviceInfo.Tow.CalculatePrice(dealershipSupply.Price) : serviceInfo.Tow.DefaultPrice);
				Utils.DisplayHelpText("You paid ~g~" + amount.ToCurrencyString() + " ~s~to the impound office. Your ~b~" + vehicleFullName + " ~s~has been returned to your garage.");
				VehiclesMenuScript.InvalidateCache();
				menu.RemoveMenuItem(menuItem);
				if (menu.GetMenuItems().Count == 0)
				{
					menu.AddMenuItem(new MenuItem("No vehicles :)", "You don't have any impounded vehicle!"));
				}
			}
			else
			{
				Utils.DisplayErrorMessage(90, num);
			}
		}
		catch (Exception exception)
		{
			Utils.DisplayErrorMessage(90);
			Print(exception);
		}
		finally
		{
			isBusy = false;
		}
	}

	[Update]
	private async Coroutine GetClosestImpoundTask()
	{
		await Script.Wait(2000);
		Impound impound = currentImpound;
		bool flag = canOpenImpoundMenu;
		currentImpound = null;
		canOpenImpoundMenu = false;
		foreach (Impound impound2 in impounds)
		{
			Vector3 val = impound2.Position.XYZ();
			float num = ((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
			if (num < 900f)
			{
				currentImpound = impound2;
				if (num <= 2.25f)
				{
					canOpenImpoundMenu = true;
				}
				break;
			}
		}
		if (impound == null && currentImpound != null)
		{
			base.Update += DrawImpoundTask;
		}
		else if (impound != null && currentImpound == null)
		{
			base.Update -= DrawImpoundTask;
		}
		if (!flag && canOpenImpoundMenu)
		{
			KeysScript.AttachListener((Control)51, OnKeyEvent, 20);
			Utils.AddInstructionalButton("impoundMenu", new InstructionalButton("Impound", 2, (Control)51));
		}
		else if (flag && !canOpenImpoundMenu)
		{
			KeysScript.DetachListener((Control)51, OnKeyEvent);
			Utils.RemoveInstructionalButton("impoundMenu");
		}
	}

	private bool OnKeyEvent(Control ctrl, KeyEventType eventType, InputType inputType)
	{
		if (canOpenImpoundMenu && eventType == KeyEventType.JustPressed)
		{
			OpenMenu();
		}
		return false;
	}

	private async Coroutine DrawImpoundTask()
	{
		if (currentImpound != null)
		{
			World.DrawMarker((MarkerType)1, currentImpound.Position.XYZ(), Vector3.Zero, Vector3.Zero, new Vector3(1f, 1f, 0.75f), System.Drawing.Color.FromArgb(-2135228416), false, false, false, (string)null, (string)null, false);
		}
	}
}
