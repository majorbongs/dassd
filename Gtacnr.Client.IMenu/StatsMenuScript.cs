using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class StatsMenuScript : Script
{
	private Menu statsMenu;

	private Dictionary<string, Menu> catMenus = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Dictionary<string, StatInfo> statInfos = Gtacnr.Utils.LoadJson<Dictionary<string, StatInfo>>("data/stats.json");

	private Dictionary<string, string> statsCache;

	private DateTime lastUpdateTimestamp;

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		string counterPreText = await Authentication.GetAccountName();
		statsMenu = new Menu(LocalizationController.S(Entries.Player.MENU_STATS_TITLE), LocalizationController.S(Entries.Player.MENU_STATS_SUBTITLE));
		statsMenu.CounterPreText = counterPreText;
		MenuController.AddSubmenu(MainMenuScript.StatsAndTasksMenu, statsMenu);
		MenuController.BindMenuItem(MainMenuScript.StatsAndTasksMenu, statsMenu, MainMenuScript.MainMenuItems["stats"]);
		foreach (string key in statInfos.Keys)
		{
			statInfos[key].Id = key;
		}
		MainMenuScript.StatsAndTasksMenu.OnItemSelect += OnMainMenuItemSelect;
	}

	private void OnMainMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == MainMenuScript.MainMenuItems["stats"])
		{
			RefreshMenu();
		}
	}

	private async void RefreshMenu()
	{
		_ = 1;
		try
		{
			if (Gtacnr.Utils.CheckTimePassed(lastUpdateTimestamp, 60000.0))
			{
				statsMenu.ClearMenuItems();
				statsMenu.AddLoadingMenuItem();
				string text = await TriggerServerEventAsync<string>("gtacnr:stats:getAll", new object[1] { 0 });
				if (!string.IsNullOrEmpty(text))
				{
					statsCache = text.Unjson<Dictionary<string, string>>();
				}
			}
			string counterPreText = await Authentication.GetAccountName();
			lastUpdateTimestamp = DateTime.UtcNow;
			statsMenu.ClearMenuItems();
			catMenus.Clear();
			menuItems.Clear();
			foreach (StatInfo value in statInfos.Values)
			{
				if (value.Hide)
				{
					continue;
				}
				MenuItem menuItem = new MenuItem(value.Name, value.Description);
				string text2 = (statsCache.ContainsKey(value.Id) ? statsCache[value.Id] : null);
				string key = $"cat_{value.Category}";
				if (!catMenus.ContainsKey(key))
				{
					catMenus[key] = new Menu("Statistics", Gtacnr.Utils.GetDescription(value.Category));
					menuItems[key] = new MenuItem(Gtacnr.Utils.GetDescription(value.Category));
					catMenus[key].CounterPreText = counterPreText;
					statsMenu.AddMenuItem(menuItems[key]);
					MenuController.AddSubmenu(statsMenu, catMenus[key]);
					MenuController.BindMenuItem(statsMenu, catMenus[key], menuItems[key]);
				}
				catMenus[key].AddMenuItem(menuItem);
				if (value.Id == "total_kdr")
				{
					double num = double.Parse(statsCache.ContainsKey("total_kills") ? statsCache["total_kills"] : "0");
					double num2 = double.Parse(statsCache.ContainsKey("total_deaths") ? statsCache["total_deaths"] : "0");
					text2 = ((num2 == 0.0) ? "~r~Undefined" : $"{num / num2:0.00}");
				}
				else if (value.Id == "headshot_rate")
				{
					double num3 = double.Parse(statsCache.ContainsKey("kills_0_3_306") ? statsCache["kills_0_3_306"] : "0");
					double num4 = double.Parse(statsCache.ContainsKey("headshots") ? statsCache["headshots"] : "0") / num3;
					text2 = ((!(num3 < 30.0)) ? $"{num4:0.00}" : "~y~Unavailable");
				}
				if (value.ExtraType == StatExtraType.None)
				{
					string text3 = "0";
					switch (value.Type)
					{
					case StatType.Count:
					{
						if (text2 != null && double.TryParse(text2, out var result2))
						{
							text2 = "~b~" + result2.ToString("0.##");
						}
						else
						{
							text3 = "~b~0";
						}
						break;
					}
					case StatType.Currency:
					{
						if (text2 != null && long.TryParse(text2, out var result3))
						{
							text2 = "~g~" + result3.ToCurrencyString();
						}
						else
						{
							text3 = "~g~$0";
						}
						break;
					}
					case StatType.Time:
					{
						if (text2 != null && double.TryParse(text2, out var result4))
						{
							text2 = ((!(result4 >= 60.0)) ? $"~b~{result4:0} minutes" : $"~b~{result4 / 60.0:0} hours");
						}
						else
						{
							text3 = "~b~0 minutes";
						}
						break;
					}
					case StatType.Percentage:
					{
						if (text2 != null && double.TryParse(text2, out var result))
						{
							double num5 = result * 100.0;
							text2 = $"~b~{num5:0.##}%";
						}
						else
						{
							text3 = "~r~Undefined";
						}
						break;
					}
					}
					menuItem.Label = text2 ?? text3;
					continue;
				}
				menuItem.Text = value.ExtraName;
				menuItem.Description = value.ExtraDescription;
				menuItem.Label = "›";
				if (text2 != null)
				{
					Dictionary<string, string> dictionary = text2.Unjson<Dictionary<string, string>>();
					Menu menu = new Menu("Statistics", value.ExtraName);
					menu.CounterPreText = counterPreText;
					MenuController.AddSubmenu(catMenus[key], menu);
					MenuController.BindMenuItem(catMenus[key], menu, menuItem);
					List<MenuItem> list = new List<MenuItem>();
					foreach (string key2 in dictionary.Keys)
					{
						text2 = dictionary[key2];
						MenuItem menuItem2 = new MenuItem("", "");
						list.Add(menuItem2);
						string text4 = "0";
						switch (value.Type)
						{
						case StatType.Count:
						{
							if (text2 != null && double.TryParse(text2, out var result6))
							{
								text2 = "~b~" + result6.ToString("0.##");
								menuItem2.ItemData = result6;
							}
							else
							{
								text4 = "~b~0";
							}
							break;
						}
						case StatType.Currency:
						{
							if (text2 != null && long.TryParse(text2, out var result7))
							{
								text2 = "~g~" + result7.ToCurrencyString();
								menuItem2.ItemData = result7;
							}
							else
							{
								text4 = "~g~$0";
							}
							break;
						}
						case StatType.Time:
						{
							if (text2 != null && double.TryParse(text2, out var result8))
							{
								text2 = ((!(result8 >= 60.0)) ? $"~b~{result8:0} minutes" : $"~b~{result8 / 60.0:0} hours");
								menuItem2.ItemData = result8;
							}
							else
							{
								text4 = "~b~0 minutes";
							}
							break;
						}
						case StatType.Percentage:
						{
							if (text2 != null && double.TryParse(text2, out var result5))
							{
								double num6 = result5 * 100.0;
								text2 = $"~b~{num6:0.##}%";
								menuItem2.ItemData = num6;
							}
							else
							{
								text4 = "~r~Undefined";
							}
							break;
						}
						}
						menuItem2.Label = text2 ?? text4;
						switch (value.ExtraType)
						{
						case StatExtraType.Item:
						{
							InventoryItemBase inventoryItemBase = (Gtacnr.Data.Items.IsItemDefined(key2) ? Gtacnr.Data.Items.GetItemDefinition(key2) : (Gtacnr.Data.Items.IsWeaponDefined(key2) ? Gtacnr.Data.Items.GetWeaponDefinition(key2) : (Gtacnr.Data.Items.IsWeaponComponentDefined(key2) ? Gtacnr.Data.Items.GetWeaponComponentDefinition(key2) : (Gtacnr.Data.Items.IsAmmoDefined(key2) ? ((InventoryItemBase)Gtacnr.Data.Items.GetAmmoDefinition(key2)) : ((InventoryItemBase)(Gtacnr.Data.Items.IsClothingItemDefined(key2) ? Gtacnr.Data.Items.GetClothingItemDefinition(key2) : null))))));
							if (inventoryItemBase != null)
							{
								menuItem2.Text = value.Name.Replace("{Item}", inventoryItemBase.Name);
								menuItem2.Description = value.Description.Replace("{Item}", inventoryItemBase.Name);
								menuItem2.Label = text2 + (inventoryItemBase.Unit ?? " pieces");
							}
							else
							{
								list.Remove(menuItem2);
							}
							break;
						}
						case StatExtraType.Service:
						{
							Service serviceDefinition = Gtacnr.Data.Items.GetServiceDefinition(key2);
							if (serviceDefinition != null)
							{
								menuItem2.Text = value.Name.Replace("{Service}", serviceDefinition.Name);
								menuItem2.Description = value.Description.Replace("{Service}", serviceDefinition.Name);
							}
							else
							{
								list.Remove(menuItem2);
							}
							break;
						}
						case StatExtraType.WeaponHash:
						{
							string deathCauseString = Utils.GetDeathCauseString(int.Parse(key2));
							menuItem2.Text = value.Name.Replace("{Weapon}", deathCauseString);
							menuItem2.Description = value.Description.Replace("{Weapon}", deathCauseString);
							break;
						}
						case StatExtraType.Job:
						{
							Job jobData = Gtacnr.Data.Jobs.GetJobData(key2);
							if (jobData != null)
							{
								string newValue = jobData.Name;
								if (key2 == "none")
								{
									newValue = "Civilian";
								}
								menuItem2.Text = value.Name.Replace("{Job}", newValue);
								menuItem2.Description = value.Description.Replace("{Job}", newValue);
							}
							else
							{
								list.Remove(menuItem2);
							}
							break;
						}
						case StatExtraType.WantedLevel:
							menuItem2.Text = value.Name.Replace("{WL}", key2);
							menuItem2.Description = value.Description.Replace("{WL}", key2);
							break;
						}
					}
					foreach (MenuItem item in list.OrderByDescending((MenuItem i) => i.ItemData))
					{
						menu.AddMenuItem(item);
					}
				}
				else
				{
					menuItem.Label = "N/A";
				}
			}
		}
		catch (Exception ex)
		{
			Print(ex);
			statsMenu.ClearMenuItems();
			statsMenu.AddErrorMenuItem(ex);
		}
	}
}
