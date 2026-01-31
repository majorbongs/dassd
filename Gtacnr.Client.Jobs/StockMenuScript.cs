using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Jobs;

public class StockMenuScript : Script
{
	private static Menu pricesMenu;

	private static StockMenuScript instance;

	private Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private float totalWeight;

	private bool closingPricesMenu;

	private List<InventoryEntry> entriesCache;

	private DateTime lastRefreshTimestamp;

	private DateTime manualRefreshTimestamp;

	private Job jobCache;

	private InventoryEntry selectedEntry;

	public static Menu Menu { get; private set; }

	public static IEnumerable<InventoryEntry> Cache => instance.entriesCache;

	public static bool ShouldRefreshCache()
	{
		if (instance.entriesCache != null)
		{
			return Gtacnr.Utils.CheckTimePassed(instance.lastRefreshTimestamp, 30000.0);
		}
		return true;
	}

	public static async Task<IEnumerable<InventoryEntry>> ReloadStock()
	{
		string cachedJob = Gtacnr.Client.API.Jobs.CachedJob;
		instance.jobCache = Gtacnr.Data.Jobs.GetJobData(cachedJob);
		StockMenuScript stockMenuScript = instance;
		stockMenuScript.entriesCache = await Inventories.GetJobInventory(instance.jobCache.Id);
		return instance.entriesCache;
	}

	public StockMenuScript()
	{
		CreateMainMenu();
		CreatePricesMenu();
		instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		jobCache = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId);
	}

	private void CreateMainMenu()
	{
		Menu = new Menu(LocalizationController.S(Entries.Businesses.MENU_STOCK_TITLE), LocalizationController.S(Entries.Businesses.MENU_STOCK_TITLE));
		RefreshMainMenuOptions();
		Menu.OnMenuOpen += OnMenuOpen;
		Menu.OnIndexChange += OnMenuSelectedIndexChanged;
	}

	private void CreatePricesMenu()
	{
		pricesMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_SALESINVENTORY_TITLE), LocalizationController.S(Entries.Businesses.MENU_SALESINVENTORY_PRICES_SUBTITLE));
		pricesMenu.OnMenuOpen += OnMenuOpen;
		pricesMenu.OnItemSelect += OnMenuSelect;
		pricesMenu.OnMenuClosing += OnPricesMenuClosing;
		MenuController.AddSubmenu(Menu, pricesMenu);
	}

	private void RefreshMainMenuOptions(Menu menu = null)
	{
		if (menu == null)
		{
			menu = Menu;
		}
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem == null || !(currentMenuItem.ItemData is InventoryEntry inventoryEntry))
		{
			return;
		}
		menu.InstructionalButtons.Clear();
		menu.ButtonPressHandlers.Clear();
		if (Gtacnr.Data.Items.IsItemBaseDefined(inventoryEntry.ItemId))
		{
			InventoryItemBase? itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(inventoryEntry.ItemId);
			if (itemBaseDefinition.CanSell)
			{
				menu.InstructionalButtons.Add((Control)201, "Manage prices");
			}
			if (itemBaseDefinition.CanMove)
			{
				bool flag = Gtacnr.Data.Items.IsWeaponDefined(inventoryEntry.ItemId) || Gtacnr.Data.Items.IsAmmoDefined(inventoryEntry.ItemId) || Gtacnr.Data.Items.IsWeaponComponentDefined(inventoryEntry.ItemId);
				menu.InstructionalButtons.Add((Control)204, flag ? "Move to armory" : "Move to inventory");
				menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)204, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, (flag ? new Action<Menu, Control>(OnMoveToArmory) : new Action<Menu, Control>(OnMoveToInventory)).Invoke, disableControl: true));
			}
		}
		if (menu == Menu)
		{
			Menu.InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
			Menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)166, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnStockRefresh, disableControl: true));
			Menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		}
	}

	private async void OnStockRefresh(Menu menu, Control control)
	{
		if (!Gtacnr.Utils.CheckTimePassed(manualRefreshTimestamp, 2000.0))
		{
			Utils.SendNotification("You must ~r~wait ~s~before refreshing the stock menu again.");
			Utils.PlayErrorSound();
		}
		else
		{
			manualRefreshTimestamp = DateTime.UtcNow;
			RefreshItemsMenu(forceReload: true);
		}
	}

	private async void RefreshItemsMenu(bool forceReload = false)
	{
		if (forceReload || ShouldRefreshCache())
		{
			lastRefreshTimestamp = DateTime.UtcNow;
			Menu.ClearMenuItems();
			Menu.AddLoadingMenuItem();
			Menu.CounterPreText = "";
			await ReloadStock();
		}
		float num = 0f;
		Menu.MenuTitle = jobCache.Name;
		Menu.ClearMenuItems();
		subMenus.Clear();
		int num2 = 0;
		foreach (InventoryEntry item in from e in entriesCache
			where Gtacnr.Data.Items.IsItemBaseDefined(e.ItemId)
			orderby !Gtacnr.Data.Items.GetItemBaseDefinition(e.ItemId).CanSell, e.Data.Selling.Path ?? Gtacnr.Data.Items.GetItemBaseDefinition(e.ItemId).DefaultPath, e.Position, Gtacnr.Data.Items.GetItemBaseDefinition(e.ItemId).Name
			select e)
		{
			num2++;
			InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(item.ItemId);
			string text = item.Data.Selling.Path ?? itemBaseDefinition.DefaultPath;
			Menu menu = null;
			if (text == string.Empty)
			{
				menu = Menu;
			}
			else if (subMenus.ContainsKey(text))
			{
				menu = subMenus[text];
			}
			else
			{
				string[] array = text.Split('/');
				string text2 = "";
				MenuItem itemData = null;
				string[] array2 = array;
				foreach (string text3 in array2)
				{
					text2 = (text2 + "/" + text3).Trim('/');
					if (!subMenus.ContainsKey(text2))
					{
						subMenus[text2] = new Menu(Menu.MenuTitle, text3 ?? "")
						{
							PlaySelectSound = false
						};
						subMenus[text2].OnMenuOpen += OnMenuOpen;
						subMenus[text2].OnIndexChange += OnMenuSelectedIndexChanged;
						if (menu == null)
						{
							menu = Menu;
						}
						MenuItem menuItem = new MenuItem(text3)
						{
							Label = Utils.MENU_ARROW
						};
						menu.AddMenuItem(menuItem);
						MenuController.AddSubmenu(menu, subMenus[text2]);
						MenuController.BindMenuItem(menu, subMenus[text2], menuItem);
						if (text3 == array.Last())
						{
							menuItem.ItemData = Tuple.Create(item);
						}
						else
						{
							menuItem.ItemData = itemData;
						}
						itemData = menuItem;
					}
					menu = subMenus[text2];
				}
			}
			if (itemBaseDefinition.Unit == null)
			{
				LocalizationController.S(Entries.Businesses.STP_ITEM_UNIT_PIECE);
			}
			Gtacnr.Data.Items.IsAmmoDefined(itemBaseDefinition.Id);
			num += itemBaseDefinition.Weight * item.Amount;
			MenuItem menuItem2 = new MenuItem(itemBaseDefinition.Name);
			RefreshMenuInventoryItem(menuItem2, item);
			menu.AddMenuItem(menuItem2);
		}
		if (num2 == 0)
		{
			Menu.AddMenuItem(new MenuItem("No items in your stock inventory :(", "Visit a supplier to refill."));
		}
		totalWeight = num;
		RefreshMenuPreText();
	}

	private async void RefreshPricesMenu()
	{
		if (selectedEntry == null)
		{
			return;
		}
		InventoryEntryData data = selectedEntry.Data;
		if (data == null)
		{
			return;
		}
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(selectedEntry.ItemId);
		pricesMenu.ClearMenuItems();
		pricesMenu.MenuSubtitle = itemBaseDefinition.Name ?? "";
		pricesMenu.CounterPreText = "PRICE";
		int num = 0;
		if (data.Selling == null)
		{
			data.Selling = new InventoryEntrySellingData();
		}
		if (data.Selling.Supplies.Count == 0)
		{
			data.Selling.Supplies.AddRange(itemBaseDefinition.DefaultSupplies);
		}
		foreach (SellableItemSupply supply in data.Selling.Supplies)
		{
			GetExtremePrices(selectedEntry.ItemId, num, out var marketValue, out var minPrice, out var maxPrice);
			int amount = Convert.ToInt32(Math.Ceiling((float)supply.Price / supply.Amount));
			pricesMenu.AddMenuItem(new MenuItem(supply.FormatAmount(itemBaseDefinition.Unit))
			{
				Label = "~g~" + supply.Price.ToCurrencyString() + " ~s~(" + amount.ToCurrencyString() + "/" + (itemBaseDefinition.Unit ?? "piece") + ")",
				Description = "Market value: ~g~" + marketValue.ToCurrencyString() + "~s~~n~Valid price range: ~b~" + minPrice.ToCurrencyString() + "~s~-~b~" + maxPrice.ToCurrencyString(),
				ItemData = supply
			});
			num++;
		}
	}

	private void RefreshMenuInventoryItem(MenuItem menuItem, InventoryEntry entry)
	{
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(entry.ItemId);
		menuItem.ItemData = entry;
		menuItem.Label = entry.FormatAmount(itemBaseDefinition.Unit);
		if (itemBaseDefinition.JobLimits.ContainsKey(jobCache.Id))
		{
			menuItem.Label += $" ~b~of {itemBaseDefinition.JobLimits[jobCache.Id]:0.##}";
		}
		menuItem.Description = "";
		if (!string.IsNullOrWhiteSpace(itemBaseDefinition.Description))
		{
			menuItem.Description = menuItem.Description + itemBaseDefinition.Description + "\n";
		}
		if (!itemBaseDefinition.CanSell)
		{
			menuItem.Text = "~y~" + menuItem.Text;
			menuItem.Description += "~y~This item cannot be sold to players directly.";
		}
		else
		{
			MenuController.BindMenuItem(Menu, pricesMenu, menuItem);
		}
		menuItem.Description = menuItem.Description.TrimEnd('\n');
	}

	private void RefreshMenuPreText(Menu menu = null)
	{
		if (menu == null)
		{
			menu = Menu;
		}
		float num = jobCache.InventoryCapacity / 1000f;
		float num2 = totalWeight / 1000f;
		string text = (menu.CounterPreText = ((jobCache.InventoryCapacity <= 0f) ? $"{num2:0.##}kg" : $"{num2:0.##}kg / {num}kg"));
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry inventoryEntry)
		{
			InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(inventoryEntry.ItemId);
			float num3 = inventoryEntry.Amount * itemBaseDefinition.Weight;
			if (num3 > 1000f)
			{
				float num4 = num3 / 1000f;
				menu.CounterPreText = $"{num4:0.##}kg          " + text;
			}
			else
			{
				menu.CounterPreText = $"{num3:0.##}g           " + text;
			}
		}
	}

	private void GetExtremePrices(string itemId, int supplyIndex, out int marketValue, out int minPrice, out int maxPrice)
	{
		List<SellableItemSupply> defaultSupplies = Gtacnr.Data.Items.GetItemBaseDefinition(itemId).DefaultSupplies;
		marketValue = defaultSupplies[supplyIndex].Price;
		minPrice = Convert.ToInt32(Math.Ceiling((float)marketValue * 0.25f));
		maxPrice = Convert.ToInt32(Math.Ceiling((float)marketValue * 2.5f));
	}

	private async void OnMenuSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu != pricesMenu || !(menuItem.ItemData is SellableItemSupply))
		{
			return;
		}
		InventoryEntryData data = selectedEntry.Data;
		if (data != null && data.Selling != null)
		{
			int oldPrice = data.Selling.Supplies[itemIndex].Price;
			InventoryItemBase itemInfo = Gtacnr.Data.Items.GetItemBaseDefinition(selectedEntry.ItemId);
			List<SellableItemSupply> defaultPrices = itemInfo.DefaultSupplies;
			GetExtremePrices(selectedEntry.ItemId, itemIndex, out var _, out var minPrice, out var maxPrice);
			string text = await Utils.GetUserInput("Price", "Enter a price for this item", "", 11, "number");
			if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text, out var newPrice))
			{
				if (newPrice < minPrice)
				{
					Utils.SendNotification("The price of this item can't be lower than ~r~" + minPrice.ToCurrencyString() + "~s~.");
				}
				else if (newPrice > maxPrice)
				{
					Utils.SendNotification("The price of this item can't be higher than ~r~" + maxPrice.ToCurrencyString() + "~s~.");
				}
				else if (newPrice == oldPrice)
				{
					Utils.SendNotification("The price remains ~p~unchanged~s~.");
				}
				else if (await TriggerServerEventAsync<bool>("gtacnr:sellmenu:setPrice", new object[3] { selectedEntry.ItemId, itemIndex, newPrice }))
				{
					data.Selling.Supplies[itemIndex].Price = newPrice;
					int currentIndex = pricesMenu.CurrentIndex;
					RefreshPricesMenu();
					pricesMenu.CurrentIndex = currentIndex;
					Utils.SendNotification($"You've set the price of ~p~{defaultPrices[itemIndex].Amount:0.##}{itemInfo.Unit} ~p~{itemInfo.Name} ~s~to ~g~{newPrice.ToCurrencyString()}~s~.");
				}
				else
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				}
			}
			else
			{
				Utils.SendNotification("You must enter a ~r~numeric value~s~.");
			}
		}
		else
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
	}

	private void OnMenuSelectedIndexChanged(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		RefreshMenuPreText(menu);
		RefreshMainMenuOptions(menu);
		selectedEntry = newItem.ItemData as InventoryEntry;
	}

	private async void OnMoveToInventory(Menu menu, Control control)
	{
		MenuItem menuItem = menu.GetCurrentMenuItem();
		object itemData = menuItem.ItemData;
		if (!(itemData is InventoryEntry entry))
		{
			return;
		}
		InventoryItemBase itemInfo = Gtacnr.Data.Items.GetItemBaseDefinition(entry.ItemId);
		if (!itemInfo.CanMove)
		{
			Utils.PlayErrorSound();
			return;
		}
		string text = await Utils.GetUserInput("Amount", "Enter the amount to move.", "", 11, "number");
		if (!string.IsNullOrWhiteSpace(text) && float.TryParse(text, out var amount))
		{
			InventoryItemBase item = Gtacnr.Data.Items.GetItemBaseDefinition(entry.ItemId);
			if (amount > entry.Amount)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification($"~r~You don't have {amount:0.##}{itemInfo.Unit} {item.Name}.");
				return;
			}
			if (amount < 0f || Math.Round(amount % 0.1f) != 0.0)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification("~r~The amount must be a multiple of 0.1.");
				return;
			}
			MoveItemResponse moveItemResponse = (MoveItemResponse)(await TriggerServerEventAsync<int>("gtacnr:sellmenu:moveToMainInventory", new object[2] { entry.ItemId, amount }));
			switch (moveItemResponse)
			{
			case MoveItemResponse.Success:
				Utils.SendNotification($"You moved ~p~{amount:0.##}{itemInfo.Unit} {item.Name} ~s~to your inventory.");
				Utils.PlayContinueSound();
				RefreshMenuInventoryItem(menuItem, entry);
				break;
			case MoveItemResponse.LimitReached:
				Utils.SendNotification($"~r~You can't hold {amount:0.##}{itemInfo.Unit} more {item.Name} in your inventory.");
				Utils.PlayErrorSound();
				break;
			case MoveItemResponse.NoSpaceLeft:
				Utils.SendNotification($"~r~You don't have enough space for {amount:0.##}{itemInfo.Unit} {item.Name} in your inventory.");
				Utils.PlayErrorSound();
				break;
			case MoveItemResponse.InsufficientAmount:
				Utils.SendNotification($"~r~You don't have {amount:0.##}{itemInfo.Unit} {item.Name}.");
				Utils.PlayErrorSound();
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x6C-{(int)moveItemResponse}"));
				break;
			}
		}
		else
		{
			Utils.PlayErrorSound();
		}
	}

	private async void OnMoveToArmory(Menu menu, Control control)
	{
		Utils.PlayErrorSound();
		Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED), playSound: false);
	}

	private void OnMove(Menu menu, Control control)
	{
		Utils.PlayErrorSound();
	}

	private void OnDrop(Menu menu, Control control)
	{
		Utils.PlayErrorSound();
	}

	private void OnMenuOpen(Menu menu)
	{
		if (menu == pricesMenu)
		{
			RefreshPricesMenu();
			return;
		}
		if (closingPricesMenu)
		{
			closingPricesMenu = false;
		}
		else
		{
			RefreshItemsMenu();
		}
		RefreshMenuPreText(menu);
		RefreshMainMenuOptions(menu);
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		selectedEntry = currentMenuItem.ItemData as InventoryEntry;
	}

	private bool OnPricesMenuClosing(Menu menu)
	{
		closingPricesMenu = true;
		return true;
	}

	[EventHandler("gtacnr:inventories:entryAdded")]
	private void OnEntryAdded(string jEntry, int iType, string job)
	{
		InventoryEntry inventoryEntry = jEntry.Unjson<InventoryEntry>();
		if (Gtacnr.Data.Items.GetItemBaseDefinition(inventoryEntry.ItemId) == null || iType != 3 || !(job == jobCache.Id))
		{
			return;
		}
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu != null)
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			if (currentMenu.ParentMenu == Menu && currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry)
			{
				return;
			}
		}
		if (entriesCache == null)
		{
			return;
		}
		bool flag = false;
		foreach (InventoryEntry item in entriesCache.ToList())
		{
			if (item.ItemId == inventoryEntry.ItemId)
			{
				item.Amount += inventoryEntry.Amount;
				if (item.Amount == 0f)
				{
					entriesCache.Remove(item);
				}
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			entriesCache.Add(inventoryEntry);
		}
	}
}
