using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Items;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.PlayerInteraction;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class InventoryMenuScript : Script
{
	private static Menu amountMenu;

	private bool isUsingItem;

	private float totalWeight;

	private List<InventoryEntry> entriesCache;

	private Job jobCache;

	private DateTime lastRefreshTimestamp;

	private DateTime manualRefreshTimestamp;

	private Dictionary<string, DropInfo> droppedItems = new Dictionary<string, DropInfo>();

	private DropInfo targetDrop;

	private bool isPickingUpItem;

	private string lastPickedUpDropId;

	private bool wasLastPickedUpDropIdUpdated;

	public static InventoryMenuScript Instance { get; private set; }

	public static Menu Menu { get; private set; }

	private Dictionary<string, Menu> subMenus { get; } = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems { get; } = new Dictionary<string, MenuItem>();

	public static bool ShouldRefreshCache
	{
		get
		{
			if (Instance.entriesCache != null && Instance.jobCache != null)
			{
				return Gtacnr.Utils.CheckTimePassed(Instance.lastRefreshTimestamp, 180000.0);
			}
			return true;
		}
	}

	public static IEnumerable<InventoryEntry> Cache => Instance.entriesCache;

	public static Func<Menu, MenuItem, Task> OnItemSelected { private get; set; }

	public static string ItemSelectInstructionalText { private get; set; }

	public InventoryMenuScript()
	{
		Instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SUBTITLE));
		MenuController.AddSubmenu(MainMenuScript.MainMenu, Menu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, Menu, MainMenuScript.MainMenuItems["inventory"]);
		MainMenuScript.MainMenu.OnItemSelect += OnMainMenuItemSelect;
		RegisterCommandSuggestions();
		KeysScript.AttachListener((Control)289, OnRefreshKeyPress, 1000);
		amountMenu = new Menu(LocalizationController.S(Entries.Inventory.INV_AMOUNT_MENU_TITLE), LocalizationController.S(Entries.Inventory.INV_AMOUNT_MENU_SUBTITLE));
		MenuController.AddMenu(amountMenu);
		GetAllDrops();
	}

	public static async void Open(bool setDefaults = true)
	{
		if (!DealershipScript.IsInDealership && SpawnScript.HasSpawned)
		{
			Menu.ParentMenu = null;
			if (setDefaults)
			{
				SetDefaultValues();
			}
			Menu.OpenMenu();
			await Instance.RefreshInventoryMenu();
		}
	}

	public static void SetDefaultValues()
	{
		OnItemSelected = Instance.OnInventoryItemUse;
		ItemSelectInstructionalText = LocalizationController.S(Entries.Imenu.BTN_IMENU_INVENTORY_USE);
	}

	private async Task SearchInventoryMenu()
	{
		string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEARCH), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEARCH_TEXT), "", 50);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		Menu menu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEARCH_SUBTITLE, text))
		{
			PlaySelectSound = false
		};
		subMenus["catSearch"] = menu;
		menu.InstructionalButtons.Clear();
		menu.InstructionalButtons.Add((Control)201, ItemSelectInstructionalText);
		menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		menu.InstructionalButtons.Add((Control)214, LocalizationController.S(Entries.Imenu.BTN_IMENU_INVENTORY_DROP));
		menu.ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[1]
		{
			new Menu.ButtonPressHandler((Control)214, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnInventoryItemDrop, disableControl: true)
		});
		menu.OnItemSelect += OnSubmenuItemSelect;
		menu.OnIndexChange += OnCategorySelectedIndexChanged;
		menu.OnMenuOpen += OnCategoryOpen;
		foreach (InventoryEntry item in entriesCache)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(item.ItemId);
			if (itemDefinition == null)
			{
				continue;
			}
			string id = itemDefinition.Id;
			if (id != null && !id.ToLowerInvariant().Contains(text.ToLowerInvariant()))
			{
				string name = itemDefinition.Name;
				if (name != null && !name.ToLowerInvariant().Contains(text.ToLowerInvariant()))
				{
					string description = itemDefinition.Description;
					if (description != null && !description.ToLowerInvariant().Contains(text.ToLowerInvariant()))
					{
						continue;
					}
				}
			}
			MenuItem menuItem = new MenuItem(itemDefinition.Name);
			RefreshMenuInventoryItem(menuItem, item);
			menu.AddMenuItem(menuItem);
		}
		MenuController.CloseAllMenus();
		MenuController.AddSubmenu(Menu, menu);
		menu.OpenMenu();
		Utils.PlaySelectSound();
	}

	public static async Task<IEnumerable<InventoryEntry>> ReloadInventory()
	{
		string cachedJob = Gtacnr.Client.API.Jobs.CachedJob;
		Instance.jobCache = Gtacnr.Data.Jobs.GetJobData(cachedJob);
		InventoryMenuScript instance = Instance;
		instance.entriesCache = await Inventories.GetPrimaryInventory();
		return Instance.entriesCache;
	}

	private async Task RefreshInventoryMenu(bool forceReload = false)
	{
		if (forceReload || ShouldRefreshCache)
		{
			lastRefreshTimestamp = DateTime.UtcNow;
			Menu.ClearMenuItems();
			Menu.AddLoadingMenuItem();
			Menu.CounterPreText = "";
			await ReloadInventory();
		}
		float num = 0f;
		subMenus.Clear();
		Menu.ClearMenuItems();
		int num2 = 0;
		foreach (InventoryEntry item in from e in entriesCache
			where Gtacnr.Data.Items.IsItemDefined(e.ItemId)
			orderby Gtacnr.Data.Items.GetItemDefinition(e.ItemId).Category, e.Position
			select e)
		{
			if (!(item.Amount <= 0f))
			{
				num2++;
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(item.ItemId);
				num += itemDefinition.Weight * item.Amount;
				if (!subMenus.ContainsKey($"cat{itemDefinition.Category}"))
				{
					string text = Gtacnr.Utils.ResolveLocalization(Gtacnr.Utils.GetDescription(itemDefinition.Category));
					Dictionary<string, MenuItem> dictionary = menuItems;
					string key = $"cat{itemDefinition.Category}";
					MenuItem obj = new MenuItem(text)
					{
						Label = "›"
					};
					MenuItem menuItem = obj;
					dictionary[key] = obj;
					MenuItem menuItem2 = menuItem;
					Menu.AddMenuItem(menuItem2);
					Menu menu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY), text)
					{
						PlaySelectSound = false
					};
					subMenus[$"cat{itemDefinition.Category}"] = menu;
					MenuController.AddSubmenu(Menu, menu);
					MenuController.BindMenuItem(Menu, menu, menuItem2);
					menu.InstructionalButtons.Clear();
					menu.InstructionalButtons.Add((Control)201, ItemSelectInstructionalText);
					menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
					menu.InstructionalButtons.Add((Control)214, LocalizationController.S(Entries.Imenu.BTN_IMENU_INVENTORY_DROP));
					menu.ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[1]
					{
						new Menu.ButtonPressHandler((Control)214, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnInventoryItemDrop, disableControl: true)
					});
					menu.OnItemSelect += OnSubmenuItemSelect;
					menu.OnIndexChange += OnCategorySelectedIndexChanged;
					menu.OnMenuOpen += OnCategoryOpen;
				}
				Menu menu2 = subMenus[$"cat{itemDefinition.Category}"];
				MenuItem menuItem3 = new MenuItem(itemDefinition.Name);
				RefreshMenuInventoryItem(menuItem3, item);
				menu2.AddMenuItem(menuItem3);
			}
		}
		if (jobCache.HasJobInventory)
		{
			menuItems["sellableItems"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_STOCK), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_STOCK_DESCR))
			{
				Label = "›"
			};
			Menu.AddMenuItem(menuItems["sellableItems"]);
			MenuController.AddSubmenu(Menu, StockMenuScript.Menu);
			MenuController.BindMenuItem(Menu, StockMenuScript.Menu, menuItems["sellableItems"]);
			num2++;
		}
		if (num2 == 0)
		{
			Menu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_EMPTY), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_EMPTY_DESCR)));
		}
		totalWeight = num;
		RefreshMenuPreText();
		Menu.InstructionalButtons.Clear();
		Menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Main.BTN_SELECT));
		Menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		Menu.InstructionalButtons.Add((Control)206, LocalizationController.S(Entries.Main.BTN_SEARCH));
		Menu.InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
		Menu.ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[2]
		{
			new Menu.ButtonPressHandler((Control)206, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnInventorySearch, disableControl: true),
			new Menu.ButtonPressHandler((Control)166, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnInventoryRefresh, disableControl: true)
		});
	}

	private void RefreshMenuInventoryItem(MenuItem menuItem, InventoryEntry entry)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
		menuItem.ItemData = entry;
		menuItem.Label = string.Format("{0:0.##}{1}", entry.Amount, (itemDefinition.Limit > 0f) ? "" : itemDefinition.Unit);
		if (itemDefinition.Limit > 0f)
		{
			menuItem.Label = menuItem.Label + " " + LocalizationController.S(Entries.Inventory.INV_AMOUNT_OF_MAX_AMOUNT, $"{itemDefinition.Limit}{itemDefinition.Unit}");
		}
		menuItem.Description = "";
		if (!string.IsNullOrWhiteSpace(itemDefinition.Description))
		{
			menuItem.Description = menuItem.Description + itemDefinition.Description + "\n";
		}
		if (itemDefinition.CanUse)
		{
			menuItem.Description = menuItem.Description + "~y~/use " + (itemDefinition.Alias ?? itemDefinition.Id) + ((itemDefinition.UseAmounts.Count > 1) ? " (amount)" : "") + "\n";
		}
		if (itemDefinition.IsStolen)
		{
			menuItem.Description = menuItem.Description + "~r~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_STOLEN) + " ";
		}
		else if (itemDefinition.IsIllegal)
		{
			menuItem.Description = menuItem.Description + "~r~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_ILLEGAL) + " ";
		}
		string text = itemDefinition.Rarity.ToMenuItemDescription();
		if (!string.IsNullOrEmpty(text))
		{
			menuItem.Description = menuItem.Description + text + " ";
		}
		menuItem.Description = menuItem.Description.Trim();
	}

	private void RefreshMenuPreText()
	{
		float num = totalWeight / 1000f;
		float num2 = Constants.GetInventoryCapacityByType(InventoryType.Primary) / 1000f;
		string text = string.Format("{0}{1:0.##}kg / {2:0}kg", (num >= 25f) ? "~r~" : ((num >= 20f) ? "~o~" : ""), num, num2);
		Menu.CounterPreText = text;
		foreach (Menu value in subMenus.Values)
		{
			value.CounterPreText = text;
			if (!value.Visible)
			{
				continue;
			}
			MenuItem currentMenuItem = value.GetCurrentMenuItem();
			if (currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry inventoryEntry)
			{
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(inventoryEntry.ItemId);
				float num3 = inventoryEntry.Amount * itemDefinition.Weight;
				if (num3 > 1000f)
				{
					float num4 = num3 / 1000f;
					value.CounterPreText = $"{num4:0.##}kg            " + text;
				}
				else
				{
					value.CounterPreText = $"{num3:0.##}g          " + text;
				}
			}
		}
	}

	private bool OnRefreshKeyPress(Control control, KeyEventType eventType, InputType inputType)
	{
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard)
		{
			Open();
			return true;
		}
		return false;
	}

	private async void OnMainMenuItemSelect(Menu menu, MenuItem selectedItem, int itemIndex)
	{
		if (selectedItem == MainMenuScript.MainMenuItems["inventory"])
		{
			SetDefaultValues();
			Menu.ParentMenu = MainMenuScript.MainMenu;
			await RefreshInventoryMenu();
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		jobCache = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId);
	}

	private async void OnSubmenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (isUsingItem)
		{
			Utils.PlayErrorSound();
			return;
		}
		isUsingItem = true;
		try
		{
			await (OnItemSelected?.Invoke(menu, menuItem));
			if (menu.GetMenuItems().Count == 0)
			{
				menu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_NO_ITEMS_IN_CATEGORY, Entries.Imenu.IMENU_INVENTORY_NO_ITEMS_IN_CATEGORY_DESCR)));
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isUsingItem = false;
		}
	}

	private async Task OnInventoryItemUse(Menu menu, MenuItem menuItem)
	{
		if (menuItem == null)
		{
			return;
		}
		object itemData = menuItem.ItemData;
		if (!(itemData is InventoryEntry entry))
		{
			return;
		}
		string itemId = entry.ItemId;
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(itemId);
		float selectedAmount = 1f;
		if (itemInfo.UseAmounts.Count > 1)
		{
			amountMenu.ParentMenu = menu;
			amountMenu.PlaySelectSound = false;
			menu.CloseMenu();
			amountMenu.OpenMenu();
			amountMenu.ClearMenuItems();
			foreach (float useAmount in itemInfo.UseAmounts)
			{
				amountMenu.AddMenuItem(new MenuItem($"{useAmount}{itemInfo.Unit}")
				{
					ItemData = useAmount,
					Enabled = (entry.Amount >= useAmount || useAmount == itemInfo.UseAmounts.First())
				});
			}
			bool wasAmountSelected = false;
			Menu.ItemSelectEvent handler = null;
			handler = delegate(Menu m, MenuItem mI, int iI)
			{
				selectedAmount = (float)mI.ItemData;
				wasAmountSelected = true;
				amountMenu.OnItemSelect -= handler;
				amountMenu.GoBack();
			};
			amountMenu.OnItemSelect += handler;
			while (amountMenu.Visible)
			{
				await BaseScript.Delay(0);
			}
			if (!wasAmountSelected)
			{
				return;
			}
		}
		else
		{
			selectedAmount = itemInfo.UseAmounts.FirstOrDefault();
		}
		if (selectedAmount == 0f)
		{
			selectedAmount = 1f;
		}
		float amount = Math.Min(selectedAmount, entry.Amount);
		UseItemResponse useItemResponse = await Inventories.UseItem(itemId, amount);
		if (useItemResponse == UseItemResponse.Success)
		{
			entry.Amount -= amount;
			if (entry.Amount == 0f)
			{
				menu.RemoveMenuItem(menuItem);
			}
			else
			{
				RefreshMenuInventoryItem(menuItem, entry);
			}
			totalWeight -= amount * itemInfo.Weight;
			RefreshMenuPreText();
			Utils.PlaySelectSound();
			return;
		}
		if (useItemResponse == UseItemResponse.ClientScriptCanceled && itemInfo.EquipWithoutUsing)
		{
			Utils.PlayNavSound();
		}
		else
		{
			Utils.PlayErrorSound();
		}
		switch (useItemResponse)
		{
		case UseItemResponse.CannotUse:
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_CANT_USE));
			break;
		case UseItemResponse.RateLimited:
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_RATE_LIMIT));
			break;
		}
	}

	public async Task OnInventoryItemGive(Menu menu, MenuItem menuItem)
	{
		if (PlayerMenuScript.MenuTargetPlayer == (Player)null)
		{
			Utils.PlayErrorSound();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		InventoryItem itemInfo;
		if (((Vector3)(ref position)).DistanceToSquared(((Entity)PlayerMenuScript.MenuTargetPlayer.Character).Position) > 25f)
		{
			Utils.PlayErrorSound();
		}
		else
		{
			if (menuItem == null)
			{
				return;
			}
			object itemData = menuItem.ItemData;
			if (!(itemData is InventoryEntry entry))
			{
				return;
			}
			string itemId = entry.ItemId;
			float amount = Math.Min(1f, entry.Amount);
			itemInfo = Gtacnr.Data.Items.GetItemDefinition(itemId);
			PlayerState targetInfo = LatentPlayers.Get(PlayerMenuScript.MenuTargetPlayer);
			string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT), LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT_DESCR, itemInfo.Name, targetInfo.NameAndId), $"{amount:0.##}", 12, "number");
			if (text == "")
			{
				text = $"{amount:0.##}";
			}
			if (text == null || !float.TryParse(text, out amount) || amount <= 0f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_INVALID_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			if (!itemInfo.IsFractional && !int.TryParse(text, out var _))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			GiveItemResponse giveItemResponse = await Inventories.GiveItem(PlayerMenuScript.MenuTargetPlayer, itemId, amount);
			if (giveItemResponse == GiveItemResponse.Success)
			{
				entry.Amount -= amount;
				if (entry.Amount == 0f)
				{
					menu.RemoveMenuItem(menuItem);
				}
				else
				{
					RefreshMenuInventoryItem(menuItem, entry);
				}
				totalWeight -= amount * itemInfo.Weight;
				RefreshMenuPreText();
				Utils.PlaySelectSound();
				Animate();
				if (itemInfo.Unit == null)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_ITEM_SENT, targetInfo.ColorNameAndId, $"{amount:0.##}", itemInfo.Name));
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_ITEM_SENT_UNIT, targetInfo.ColorNameAndId, $"{amount:0.##}", itemInfo.Unit, itemInfo.Name));
				}
				return;
			}
			Utils.PlayErrorSound();
			if (giveItemResponse == GiveItemResponse.CannotGive)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_CANT_GIVE_ITEM));
			}
			if (giveItemResponse == GiveItemResponse.InsufficientAmount)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_AMOUNT, itemInfo.Name));
			}
			if (giveItemResponse == GiveItemResponse.NoSpaceLeft)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_SPACE, targetInfo.ColorNameAndId));
			}
			if (giveItemResponse == GiveItemResponse.ItemLimitReached)
			{
				if (itemInfo.Unit == null)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_LIMIT_REACHED, targetInfo.ColorNameAndId, $"{amount:0.##}", itemInfo.Name));
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_LIMIT_REACHED_UNIT, targetInfo.ColorNameAndId, $"{amount:0.##}", itemInfo.Unit, itemInfo.Name));
				}
			}
		}
		async void Animate()
		{
			if (!Game.PlayerPed.IsInVehicle())
			{
				string animDict = "mp_ped_interaction";
				string animName = "handshake_guy_a";
				int duration = 1500;
				int bone = 57005;
				API.RequestAnimDict(animDict);
				while (!API.HasAnimDictLoaded(animDict))
				{
					await BaseScript.Delay(0);
				}
				Prop prop = await World.CreateProp(new Model(itemInfo.Model ?? "prop_paper_bag_small"), ((Entity)Game.PlayerPed).Position, false, false);
				Game.PlayerPed.Task.PlayAnimation(animDict, animName, 4f, duration, (AnimationFlags)51);
				API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, bone), 0.145f, 0f, -0.075f, 312.3f, -0.5f, 0f, true, true, false, true, 1, true);
				await BaseScript.Delay(duration);
				Game.PlayerPed.Task.ClearAnimation(animDict, animName);
				((PoolObject)prop).Delete();
			}
		}
	}

	private async void OnInventoryItemDrop(Menu menu, Control control)
	{
		if (CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsInCustody || SurrenderScript.IsSurrendered || Game.PlayerPed.IsBeingStunned)
		{
			Utils.PlayErrorSound();
			return;
		}
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem == null)
		{
			Utils.PlayErrorSound();
		}
		else if (!(currentMenuItem.ItemData is InventoryEntry entry))
		{
			Utils.PlayErrorSound();
		}
		else
		{
			Drop(entry, 0f, menu, currentMenuItem);
		}
	}

	[EventHandler("gtacnr:inventories:entryAdded")]
	private void OnEntryAdded(string jEntry, int iType, string job)
	{
		InventoryEntry inventoryEntry = jEntry.Unjson<InventoryEntry>();
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(inventoryEntry.ItemId);
		if (itemBaseDefinition == null)
		{
			return;
		}
		Debug.WriteLine(string.Format("Inventory ({0}{1}): {2:+0.##;-0.##}{3} {4}", (InventoryType)iType, string.IsNullOrEmpty(job) ? "" : ("/" + job), inventoryEntry.Amount, itemBaseDefinition.Unit, itemBaseDefinition.Name));
		if (iType != 1)
		{
			return;
		}
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu != null)
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			if ((currentMenu.ParentMenu == Menu && currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry) || currentMenu == ScratchCardScript.Menu)
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

	[EventHandler("gtacnr:inventories:receivedItem")]
	private void OnItemReceived(int senderId, string itemId, float amount)
	{
		PlayerState playerState = LatentPlayers.Get(senderId);
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		string text = ((itemDefinition.Unit == null) ? "" : (itemDefinition.Unit + " " + LocalizationController.S(Entries.Imenu.ITEM_NAME_PREPOSITION)));
		Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_RECEIVED_ITEM, playerState.ColorNameAndId, $"{amount:0.##}", text, itemDefinition.Name));
	}

	private async void OnInventorySearch(Menu menu, Control control)
	{
		await SearchInventoryMenu();
	}

	private async void OnInventoryRefresh(Menu menu, Control control)
	{
		if (!Gtacnr.Utils.CheckTimePassed(manualRefreshTimestamp, 2000.0))
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_REFRESH));
			Utils.PlayErrorSound();
		}
		else
		{
			manualRefreshTimestamp = DateTime.UtcNow;
			await RefreshInventoryMenu(forceReload: true);
		}
	}

	private void OnCategoryOpen(Menu menu)
	{
		RefreshMenuPreText();
	}

	private void OnCategorySelectedIndexChanged(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		RefreshMenuPreText();
	}

	private void RegisterCommandSuggestions()
	{
		Chat.AddSuggestion("/use", "Uses an item from your inventory.", new ChatParamSuggestion("item", "The id of the item you want to use."), new ChatParamSuggestion("amount", "The amount to use (optional)."));
		Chat.AddSuggestion("/give", "Gives an item from your inventory to a player.", new ChatParamSuggestion("player", "The id of the player you want to give the item to."), new ChatParamSuggestion("item", "The id of the item you want to give."), new ChatParamSuggestion("amount", "The amount to give (optional). Type 'max' to give the maximum amount."));
		Chat.AddSuggestion("/drop", "Drops an item from your inventory to the ground.", new ChatParamSuggestion("item", "The id of the item you want to drop."), new ChatParamSuggestion("amount", "The amount to drop (optional). Type 'max' to drop the maximum amount."));
	}

	[Command("use")]
	private async void UseCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /use [item id] (amount)");
			return;
		}
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu != null)
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			if (currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "You cannot use this command while the inventory menu is open.");
				return;
			}
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
			Utils.PlayErrorSound();
			return;
		}
		string inputItemIdOrAlias = args[0];
		float inputAmount = 1f;
		if (args.Length >= 2)
		{
			float.TryParse(args[1], out inputAmount);
		}
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(inputItemIdOrAlias);
		if (itemInfo == null)
		{
			itemInfo = Gtacnr.Data.Items.GetAllItemDefinitions().FirstOrDefault((InventoryItem i) => i.Alias == inputItemIdOrAlias);
		}
		if (itemInfo == null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, inputItemIdOrAlias + " is not a valid item.");
			return;
		}
		switch (await Inventories.UseItem(itemInfo.Id, inputAmount))
		{
		case UseItemResponse.Success:
		{
			string text = $"{inputAmount:0.##}";
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Inventory.INV_USED_ITEM, text, itemInfo.Name.ToLowerInvariant()));
			Utils.PlaySelectSound();
			return;
		}
		case UseItemResponse.InsufficientAmount:
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Inventory.INV_NOT_ENOUGH_ITEM, itemInfo.Name.ToLowerInvariant()));
			Utils.PlayErrorSound();
			return;
		case UseItemResponse.RateLimited:
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Inventory.INV_RATE_LIMITED));
			Utils.PlayErrorSound();
			return;
		case UseItemResponse.ClientScriptCanceled:
			if (itemInfo.EquipWithoutUsing)
			{
				Utils.PlayNavSound();
				return;
			}
			break;
		}
		Utils.PlayErrorSound();
	}

	[Command("give")]
	private async void GiveCommand(string[] args)
	{
		if (args.Length < 2)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.CMD_USAGE, "/give [player id] [item id] (amount)"));
			return;
		}
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu != null)
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			if (currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Inventory.INV_CMD_MENU_OPEN));
				return;
			}
		}
		Chat.AddMessage(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
		Utils.PlayErrorSound();
	}

	[Command("drop")]
	private async void DropCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.CMD_USAGE, "/drop [item id] (amount)"));
			return;
		}
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu != null)
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			if (currentMenuItem != null && currentMenuItem.ItemData is InventoryEntry)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Inventory.INV_CMD_MENU_OPEN));
				return;
			}
		}
		string inputItemIdOrAlias = args[0];
		float result = 1f;
		if (args.Length >= 2)
		{
			float.TryParse(args[1], out result);
		}
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(inputItemIdOrAlias);
		if (itemInfo == null)
		{
			itemInfo = Gtacnr.Data.Items.GetAllItemDefinitions().FirstOrDefault((InventoryItem i) => i.Alias == inputItemIdOrAlias);
		}
		if (itemInfo == null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Inventory.INV_INVALID_ITEM, inputItemIdOrAlias));
			return;
		}
		InventoryEntry entry = Cache.FirstOrDefault((InventoryEntry e) => e.ItemId == itemInfo.Id);
		Drop(entry, result);
	}

	private async void GetAllDrops()
	{
		droppedItems = (await TriggerServerEventAsync<string>("gtacnr:inventories:getAllDrops", new object[0])).Unjson<Dictionary<string, DropInfo>>();
	}

	[EventHandler("gtacnr:inventories:dropAdded")]
	private void OnDropAdded(string dropId, string jData)
	{
		if (droppedItems.ContainsKey(dropId))
		{
			Print("[Drops] Warning: " + dropId + " already exists, but the server requested to add it.");
			droppedItems.Remove(dropId);
		}
		droppedItems[dropId] = jData.Unjson<DropInfo>();
	}

	[EventHandler("gtacnr:inventories:dropUpdated")]
	private void OnDropUpdated(string dropId, string jData)
	{
		if (!droppedItems.ContainsKey(dropId))
		{
			Print("[Drops] Warning: " + dropId + " didn't exist, but the server requested to update it.");
		}
		droppedItems[dropId] = jData.Unjson<DropInfo>();
		if (lastPickedUpDropId == dropId)
		{
			wasLastPickedUpDropIdUpdated = true;
		}
	}

	[EventHandler("gtacnr:inventories:dropDeleted")]
	private void OnDropDeleted(string dropId)
	{
		if (!droppedItems.Remove(dropId))
		{
			Print("[Drops] Warning: Drop id " + dropId + " didn't exist, but the server requested to delete it.");
		}
		if (lastPickedUpDropId == dropId)
		{
			wasLastPickedUpDropIdUpdated = true;
		}
	}

	[Update]
	private async Coroutine DropsTask()
	{
		await BaseScript.Delay(500);
		DropInfo dropInfo = null;
		float num = 4f;
		foreach (DropInfo item in droppedItems.Values.ToList())
		{
			if (!API.NetworkDoesEntityExistWithNetworkId(item.PropId))
			{
				continue;
			}
			Entity obj = Entity.FromNetworkId(item.PropId);
			Prop val = (Prop)(object)((obj is Prop) ? obj : null);
			if (val != null && !((Entity)(object)val == (Entity)null) && val.Exists())
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)val).Position);
				if (num2 < num)
				{
					dropInfo = item;
					num = num2;
				}
			}
		}
		if (targetDrop == null && dropInfo != null)
		{
			base.Update += DrawDropTask;
			KeysScript.AttachListener((Control)29, OnPickUpKeyPress, 100);
			Utils.AddInstructionalButton("pickUp", new InstructionalButton(LocalizationController.S(Entries.Imenu.BTN_IMENU_INVENTORY_PICKUP), 2, (Control)29));
		}
		else if (targetDrop != null && dropInfo == null)
		{
			base.Update -= DrawDropTask;
			KeysScript.DetachListener((Control)29, OnPickUpKeyPress);
			Utils.RemoveInstructionalButton("pickUp");
		}
		targetDrop = dropInfo;
	}

	private bool OnPickUpKeyPress(Control control, KeyEventType eventType, InputType inputType)
	{
		if (eventType == KeyEventType.JustPressed)
		{
			if (targetDrop == null)
			{
				return false;
			}
			if (!API.NetworkDoesEntityExistWithNetworkId(targetDrop.PropId))
			{
				Utils.PlayErrorSound();
				return false;
			}
			Entity obj = Entity.FromNetworkId(targetDrop.PropId);
			Prop val = (Prop)(object)((obj is Prop) ? obj : null);
			if (val == null)
			{
				Utils.PlayErrorSound();
				return false;
			}
			if ((Entity)(object)val == (Entity)null || !val.Exists())
			{
				Utils.PlayErrorSound();
				return false;
			}
			if (isPickingUpItem)
			{
				Utils.PlayErrorSound();
				return true;
			}
			PickUpItem();
			return true;
		}
		return false;
	}

	private async void PickUpItem()
	{
		if (isPickingUpItem || targetDrop == null || !API.NetworkDoesEntityExistWithNetworkId(targetDrop.PropId))
		{
			return;
		}
		Entity val = Entity.FromNetworkId(targetDrop.PropId);
		Prop prop = (Prop)(object)((val is Prop) ? val : null);
		if (prop == null || (Entity)(object)prop == (Entity)null || !prop.Exists())
		{
			return;
		}
		try
		{
			isPickingUpItem = true;
			await Utils.LoadAnimDictionary("random@domestic");
			await Game.PlayerPed.Task.PlayAnimation("random@domestic", "pickup_low", 4f, 4f, 1250, (AnimationFlags)51, 1f);
			await BaseScript.Delay(400);
			InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(targetDrop.Entry.ItemId);
			float dropAmount = targetDrop.Entry.Amount;
			lastPickedUpDropId = targetDrop.Id;
			PickUpItemResponse pickUpItemResponse = (PickUpItemResponse)(await TriggerServerEventAsync<int>("gtacnr:inventories:pickUpItem", new object[4]
			{
				targetDrop.Id,
				1,
				null,
				((Entity)prop).NetworkId
			}));
			switch (pickUpItemResponse)
			{
			case PickUpItemResponse.NoSpaceLeft:
				Utils.SendNotification(LocalizationController.S(Entries.Main.NOT_ENOUGH_INVENTORY_SPACE));
				return;
			case PickUpItemResponse.ItemLimitReached:
				Utils.SendNotification(LocalizationController.S(Entries.Main.ITEM_LIMIT_REACHED, itemInfo.Name));
				return;
			case PickUpItemResponse.ConcurrentPickUp:
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_PICKUP_CONCURRENT));
				return;
			default:
				Utils.DisplayErrorMessage(49, (int)pickUpItemResponse);
				return;
			case PickUpItemResponse.Success:
				break;
			}
			while (!wasLastPickedUpDropIdUpdated)
			{
				await BaseScript.Delay(0);
			}
			float num = dropAmount;
			if (droppedItems.TryGetValue(lastPickedUpDropId, out DropInfo value))
			{
				num -= value.Entry.Amount;
			}
			lastPickedUpDropId = null;
			wasLastPickedUpDropIdUpdated = false;
			Utils.PlayPurchaseSound();
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_PICKUP_SUCCESS, $"{num:0.##}", itemInfo.Unit, (itemInfo.Unit == null) ? "" : (LocalizationController.S(Entries.Imenu.ITEM_NAME_PREPOSITION) + " "), itemInfo.Name));
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isPickingUpItem = false;
		}
	}

	private async Coroutine DrawDropTask()
	{
		if (targetDrop != null && API.NetworkDoesEntityExistWithNetworkId(targetDrop.PropId))
		{
			Entity obj = Entity.FromNetworkId(targetDrop.PropId);
			Prop val = (Prop)(object)((obj is Prop) ? obj : null);
			if (val != null && !((Entity)(object)val == (Entity)null) && val.Exists())
			{
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(targetDrop.Entry.ItemId);
				Utils.Draw3DText(string.Format("{0:0.##}{1} {2}~b~{3}", targetDrop.Entry.Amount, itemDefinition.Unit, (itemDefinition.Unit == null) ? "" : "of ", itemDefinition.Name), ((Entity)val).Position + new Vector3(0f, 0f, 1f));
			}
		}
	}

	private async Task Drop(InventoryEntry entry, float amountToDrop = 0f, Menu menu = null, MenuItem menuItem = null)
	{
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
		if (itemInfo == null)
		{
			Utils.PlayErrorSound();
			return;
		}
		if (!itemInfo.CanGive || !itemInfo.CanDrop)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_CANT_DROP));
			Utils.PlayErrorSound();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = (((Entity)Game.PlayerPed).Heading + 50f).ToRadians();
		float num2 = -0.7f;
		position.X += (float)((double)num2 * Math.Cos(num));
		position.Y += (float)((double)num2 * Math.Sin(num));
		float num3 = 0f;
		API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref num3, false);
		if (num3 > position.Z + 2f || num3 < position.Z - 20f)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_DROP_INVALID_POSITION));
			Utils.PlayErrorSound();
			return;
		}
		position.Z = num3;
		if (amountToDrop <= 0f)
		{
			string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_DROP, itemInfo.Name), LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_DROP_TEXT, $"{entry.Amount:0.##}", itemInfo.Unit, ((itemInfo.Unit == null) ? "" : (LocalizationController.S(Entries.Imenu.ITEM_NAME_PREPOSITION) + " ")) ?? "", itemInfo.Name), "", 15);
			if (text == null || !float.TryParse(text, out var result) || result <= 0f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_INVALID_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			if (!itemInfo.IsFractional && !int.TryParse(text, out var _))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			amountToDrop = result;
		}
		if (amountToDrop > entry.Amount)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_AMOUNT, itemInfo.Name));
			Utils.PlayErrorSound();
			return;
		}
		await Utils.LoadAnimDictionary("anim@am_hold_up@male");
		await Game.PlayerPed.Task.PlayAnimation("anim@am_hold_up@male", "shoplift_mid", 4f, 4f, 1000, (AnimationFlags)51, 1f);
		await BaseScript.Delay(650);
		Prop prop = await World.CreateProp(Model.op_Implicit(API.GetHashKey(itemInfo.Model ?? "prop_paper_bag_small")), position, false, false);
		API.SetEntityCollision(((PoolObject)prop).Handle, false, true);
		API.SetEntityAsMissionEntity(((PoolObject)prop).Handle, true, true);
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)prop);
		DropItemResponse dropItemResponse = (DropItemResponse)(await TriggerServerEventAsync<int>("gtacnr:inventories:dropItem", new object[5]
		{
			entry.ItemId,
			amountToDrop,
			1,
			null,
			((Entity)prop).NetworkId
		}));
		if (dropItemResponse != DropItemResponse.Success)
		{
			switch (dropItemResponse)
			{
			case DropItemResponse.TooManyDrops:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_DROP_TOO_MANY));
				break;
			case DropItemResponse.InsufficientAmount:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_AMOUNT, itemInfo.Name));
				break;
			default:
				Utils.DisplayErrorMessage(50, (int)dropItemResponse);
				break;
			}
			((PoolObject)prop).Delete();
			return;
		}
		Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_DROP_SUCCESS, $"{amountToDrop:0.##}", itemInfo.Unit, ((itemInfo.Unit == null) ? "" : (LocalizationController.S(Entries.Imenu.ITEM_NAME_PREPOSITION) + " ")) ?? "", itemInfo.Name));
		entry.Amount -= amountToDrop;
		if (menu != null)
		{
			if (entry.Amount == 0f)
			{
				menu.RemoveMenuItem(menuItem);
			}
			else
			{
				RefreshMenuInventoryItem(menuItem, entry);
			}
			totalWeight -= amountToDrop * itemInfo.Weight;
			RefreshMenuPreText();
		}
	}
}
