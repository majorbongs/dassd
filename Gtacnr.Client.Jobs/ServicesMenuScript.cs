using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Jobs;

public class ServicesMenuScript : Script
{
	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private bool hasUnsavedChanges;

	public static Menu Menu { get; private set; }

	protected override async void OnStarted()
	{
		CreateMenu();
	}

	private void CreateMenu()
	{
		Menu = new Menu(LocalizationController.S(Entries.Businesses.JOBMENU_SERVICES), LocalizationController.S(Entries.Businesses.JOBMENU_SERVICES_DESCR));
		Menu.CounterPreText = "PRICE (%)";
		Menu.OnMenuOpen += OnMenuOpen;
		Menu.OnSliderPositionChange += OnSliderPositionChange;
		Menu.OnMenuClosing += OnMenuClosing;
		Menu.OnItemSelect += OnItemSelect;
		Menu.InstructionalButtons.Add((Control)204, "Self");
		Menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)204, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnSelfService, disableControl: true));
	}

	private void OnMenuOpen(Menu menu)
	{
		if (menu == Menu)
		{
			RefreshMenu();
		}
	}

	private string GetPriceColorCode(int priceInt)
	{
		if (priceInt > 50)
		{
			return "~r~";
		}
		if (priceInt > 40)
		{
			return "~y~";
		}
		if (priceInt == 40)
		{
			return "~b~";
		}
		return "~g~";
	}

	private void UpdateServiceMenuItemDescription(MenuItem menuItem, Service serviceInfo, string colorCode, float pricePercent)
	{
		string text = serviceInfo.Description;
		if (serviceInfo.NeedsToBeAtModShop)
		{
			text += "\n~y~You must be inside a modshop to be able to provide this service.";
		}
		if (serviceInfo.UseItems.Count > 0)
		{
			text += "\n~s~Uses: ";
			foreach (string useItem in serviceInfo.UseItems)
			{
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(useItem);
				if (itemDefinition != null)
				{
					text = text + "~b~" + itemDefinition.Name + "~s~, ";
				}
			}
			text = text.TrimEnd().TrimEnd(',');
		}
		string text2 = "";
		if (pricePercent < 1f)
		{
			text2 = $"{Math.Abs(pricePercent * 100f - 100f):0}% ~s~cheaper than market";
		}
		else if (pricePercent > 1f)
		{
			text2 = $"{Math.Abs(pricePercent * 100f - 100f):0}% ~s~more expensive than market";
		}
		else if (pricePercent == 1f)
		{
			text2 = "market value";
		}
		text = text + "\n~s~Price: " + colorCode + text2;
		menuItem.Description = text;
	}

	private async void RefreshMenu()
	{
		Menu.ClearMenuItems();
		Menu.AddLoadingMenuItem();
		ServiceDataResponse data = (await TriggerServerEventAsync<string>("gtacnr:services:getAll", new object[1] { Game.Player.ServerId })).Unjson<ServiceDataResponse>();
		if (data.Response == ServiceResponse.Success)
		{
			List<MenuItem> newMenuItems = new List<MenuItem>();
			foreach (string key in data.ServiceData.Prices.Keys)
			{
				Service serviceInfo = Gtacnr.Data.Items.GetServiceDefinition(key);
				if (serviceInfo != null)
				{
					float num = data.ServiceData.Prices[key];
					int num2 = Convert.ToInt32(Math.Round(num * 40f));
					string priceColorCode = GetPriceColorCode(num2);
					MenuItem menuItem = new MenuSliderItem(serviceInfo.Name, 20, 60, num2, showDivider: true)
					{
						Description = serviceInfo.Description,
						ItemData = Tuple.Create(serviceInfo.Id, num)
					};
					UpdateServiceMenuItemDescription(menuItem, serviceInfo, priceColorCode, num);
					if (!string.IsNullOrEmpty(serviceInfo.License) && await Inventories.GetEntry(serviceInfo.License) == null)
					{
						InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(serviceInfo.License);
						menuItem = new MenuItem(serviceInfo.Name)
						{
							Description = serviceInfo.Description
						};
						menuItem.RightIcon = MenuItem.Icon.LOCK;
						menuItem.Enabled = false;
						menuItem.Label = "";
						MenuItem menuItem2 = menuItem;
						menuItem2.Description = menuItem2.Description + "\n~r~You need a ~s~" + itemDefinition.Name + " ~r~to be able to provide this service.";
					}
					newMenuItems.Add(menuItem);
				}
			}
			Menu.ClearMenuItems();
			foreach (MenuItem item2 in newMenuItems)
			{
				Menu.AddMenuItem(item2);
			}
			Menu menu = Menu;
			MenuItem item = (menuItems["saveServices"] = new MenuItem("Save", "Save all the changes you've made.")
			{
				Enabled = false
			});
			menu.AddMenuItem(item);
		}
		else if (data.Response == ServiceResponse.NoData)
		{
			Menu.ClearMenuItems();
			Menu.AddMenuItem(new MenuItem("No services", "You don't have any service."));
		}
		else
		{
			Debug.WriteLine($"gtacnr:services:getAll returned error code 0xAB-{(int)data.Response}");
			Menu.ClearMenuItems();
			Menu.AddErrorMenuItem();
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu != Menu || menuItem != menuItems["saveServices"])
		{
			return;
		}
		ServiceData serviceData = new ServiceData();
		foreach (MenuItem menuItem2 in menu.GetMenuItems())
		{
			if (menuItem2.ItemData is Tuple<string, float> tuple)
			{
				serviceData.Prices[tuple.Item1] = tuple.Item2;
			}
		}
		await TriggerServerEventAsync<bool>("gtacnr:services:update", new object[1] { serviceData.Json() });
		menuItems["saveServices"].Text = "Save";
		menuItems["saveServices"].Enabled = false;
		hasUnsavedChanges = false;
		Utils.SendNotification("The ~b~changes ~s~have been saved.");
	}

	private bool OnMenuClosing(Menu menu)
	{
		if (menu == Menu && hasUnsavedChanges)
		{
			Confirm();
			return false;
		}
		return true;
		async void Confirm()
		{
			if (await Utils.ShowConfirm("Are you sure you want to close the menu without saving the changes?", "Close?"))
			{
				hasUnsavedChanges = false;
				menu.GoBack();
			}
		}
	}

	private void OnSliderPositionChange(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex)
	{
		if (sliderItem.ItemData is Tuple<string, float> { Item1: var item })
		{
			Service serviceDefinition = Gtacnr.Data.Items.GetServiceDefinition(item);
			float num = (float)newPosition * 0.025f;
			string priceColorCode = GetPriceColorCode(newPosition);
			UpdateServiceMenuItemDescription(sliderItem, serviceDefinition, priceColorCode, num);
			sliderItem.ItemData = Tuple.Create(item, num);
			if (!hasUnsavedChanges)
			{
				hasUnsavedChanges = true;
				menuItems["saveServices"].Enabled = true;
				menuItems["saveServices"].Text = "~b~Save";
			}
		}
	}

	private void OnSelfService(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is Tuple<string, float> tuple)
		{
			Service serviceDefinition = Gtacnr.Data.Items.GetServiceDefinition(tuple.Item1);
			if (!serviceDefinition.CanUseOnSelf)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification("~r~You cannot perform ~s~" + serviceDefinition.Name + " ~r~on yourself.");
			}
			else
			{
				Utils.PlaySelectSound();
				BaseScript.TriggerServerEvent("gtacnr:services:performOnSelf", new object[1] { serviceDefinition.Id });
			}
		}
	}
}
