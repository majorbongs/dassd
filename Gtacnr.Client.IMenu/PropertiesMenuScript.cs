using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.Estates.Garages;
using Gtacnr.Client.Estates.Warehouses;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class PropertiesMenuScript : Script
{
	private static PropertiesMenuScript instance;

	public static Menu Menu { get; private set; }

	public PropertiesMenuScript()
	{
		instance = this;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Properties.MENU_PROPERTIES_TITLE), LocalizationController.S(Entries.Properties.MENU_PROPERTIES_SUBTITLE));
		MenuController.AddSubmenu(MainMenuScript.MainMenu, Menu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, Menu, MainMenuScript.MainMenuItems["properties"]);
		Menu.InstructionalButtons.Clear();
		Menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Properties.BTN_MENU_PROPERTIES_GPS));
		Menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		MainMenuScript.MainMenu.OnItemSelect += OnMainMenuItemSelect;
		Menu.OnItemSelect += OnMenuItemSelect;
	}

	public static void OpenMenu(IEnumerable<PropertyType> filter = null, string overrideTitle = null, string overrideSubtitle = null)
	{
		MenuController.CloseAllMenus();
		instance.RefreshMenu(filter, overrideTitle, overrideSubtitle);
		Menu.OpenMenu();
	}

	private async void RefreshMenu(IEnumerable<PropertyType> filter = null, string overrideTitle = null, string overrideSubtitle = null)
	{
		int count = 0;
		Menu.ClearMenuItems();
		Menu.MenuTitle = ((overrideTitle != null) ? overrideTitle : LocalizationController.S(Entries.Properties.MENU_PROPERTIES_TITLE));
		Menu.MenuSubtitle = ((overrideSubtitle != null) ? overrideSubtitle : LocalizationController.S(Entries.Properties.MENU_PROPERTIES_SUBTITLE));
		Vector3 onFootPosition;
		if (ShouldShow(PropertyType.Garage) && GarageScript.OwnedGaragesCount() > 0)
		{
			if (VehiclesMenuScript.VehicleCache == null)
			{
				Menu.AddLoadingMenuItem();
				await VehiclesMenuScript.EnsureVehicleCache();
				Menu.ClearMenuItems();
			}
			Menu.AddMenuItem(Utils.GetSpacerMenuItem("\u02c5 " + LocalizationController.S(Entries.Properties.GARAGE) + " \u02c5"));
			foreach (Garage garage in GarageScript.OwnedGarages.OrderBy(delegate(Garage g)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
				Vector3 onFootPosition2 = g.OnFootPosition;
				return ((Vector3)(ref onFootPosition2)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position);
			}))
			{
				string text = Utils.GetLocationName(garage.OnFootPosition);
				int num = VehiclesMenuScript.VehicleCache.Where((StoredVehicle v) => v.GarageId == garage.Id).Count();
				onFootPosition = garage.OnFootPosition;
				float meters = (float)Math.Sqrt(((Vector3)(ref onFootPosition)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position));
				string text2 = garage.Name;
				string text3 = LocalizationController.S(Entries.Properties.MENU_PROPERTIES_GARAGE_OCCUPANCY, num, garage.Interior.ParkingSpaces.Count);
				if (text2.Length > 22)
				{
					text2 = garage.Name.Substring(0, 20) + "...";
					text3 = garage.Name + "\n" + text3;
				}
				if (text.Length > 16)
				{
					text = text.Substring(0, 13) + "...";
				}
				Menu.AddMenuItem(new MenuItem(text2, text3)
				{
					Label = text + " ~b~(" + Utils.FormatDistanceString(meters) + ")",
					ItemData = garage
				});
				count++;
			}
		}
		if (ShouldShow(PropertyType.Warehouse) && WarehouseScript.OwnedWarehouses.Count() > 0)
		{
			Menu.AddMenuItem(Utils.GetSpacerMenuItem("\u02c5 " + LocalizationController.S(Entries.Properties.WAREHOUSE) + " \u02c5"));
			foreach (Warehouse item in WarehouseScript.OwnedWarehouses.OrderBy(delegate(Warehouse g)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
				Vector3 onFootPosition2 = g.OnFootPosition;
				return ((Vector3)(ref onFootPosition2)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position);
			}))
			{
				WarehouseInterior warehouseInterior = WarehouseScript.WarehouseInteriors[item.InteriorId];
				string text4 = Utils.GetLocationName(item.OnFootPosition);
				onFootPosition = item.OnFootPosition;
				float meters2 = (float)Math.Sqrt(((Vector3)(ref onFootPosition)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position));
				string text5 = item.Name;
				string text6 = LocalizationController.S(Entries.Properties.MENU_PROPERTIES_WAREHOUSE_CAPACITY, warehouseInterior.Capacity);
				if (text5.Length > 22)
				{
					text5 = item.Name.Substring(0, 20) + "...";
					text6 = item.Name + "\n" + text6;
				}
				if (text4.Length > 16)
				{
					text4 = text4.Substring(0, 13) + "...";
				}
				Menu.AddMenuItem(new MenuItem(text5, text6)
				{
					Label = text4 + " ~b~(" + Utils.FormatDistanceString(meters2) + ")",
					ItemData = item
				});
				count++;
			}
		}
		if (count == 0)
		{
			Menu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Properties.MENU_PROPERTIES_EMPTY), LocalizationController.S(Entries.Properties.MENU_PROPERTIES_EMPTY_DESCRIPTION))
			{
				Enabled = false
			});
		}
		if (count != 1)
		{
			Menu.CounterPreText = LocalizationController.S(Entries.Properties.MENU_PROPERTIES_PRETEXT, count);
		}
		else
		{
			Menu.CounterPreText = LocalizationController.S(Entries.Properties.MENU_PROPERTIES_PRETEXT_SINGULAR, count);
		}
		bool ShouldShow(PropertyType propertyType)
		{
			if (filter != null)
			{
				return filter.Contains(propertyType);
			}
			return true;
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem selectedItem, int itemIndex)
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Properties.VEHICLE_GPS_SET));
		}
		else if (selectedItem.ItemData is Warehouse warehouse)
		{
			GPSScript.SetDestination("Warehouse", warehouse.OnFootPosition, 0f, shortRange: true, null, null, 255, autoDelete: true, 50f);
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Properties.PROPERTY_NAVIGATING, warehouse.Name));
		}
		else if (selectedItem.ItemData is Garage garage)
		{
			GPSScript.SetDestination("Garage", garage.OnFootPosition, 0f, shortRange: true, null, null, 255, autoDelete: true, 50f);
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Properties.PROPERTY_NAVIGATING, garage.Name));
		}
	}

	private void OnMainMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == MainMenuScript.MainMenuItems["properties"])
		{
			RefreshMenu();
		}
	}
}
