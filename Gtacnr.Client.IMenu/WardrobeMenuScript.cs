using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Premium;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class WardrobeMenuScript : Script
{
	private static DateTime lastWardrobeRemove = DateTime.MinValue;

	private static HashSet<string> wardrobeCache = new HashSet<string>();

	private static Dictionary<string, HashSet<string>> extraClothingItems = new Dictionary<string, HashSet<string>>();

	private static Dictionary<string, string> itemMottos = new Dictionary<string, string>();

	public static Menu Menu { get; private set; }

	private static Dictionary<string, Menu> subMenus { get; } = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems { get; } = new Dictionary<string, MenuItem>();

	public static ReadonlyHashSet<string> RegisteredClothingItems => wardrobeCache.AsReadOnly();

	public WardrobeMenuScript()
	{
		StaffLevelScript.StaffLevelInitializedOrChanged += OnStaffLevelInitializedOrChanged;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChanged;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Player.MENU_WARDROBE_TITLE), LocalizationController.S(Entries.Player.MENU_WARDROBE_SUBTITLE));
		MenuController.AddSubmenu(MainMenuScript.MainMenu, Menu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, Menu, MainMenuScript.MainMenuItems["wardrobe"]);
		Menu.OnMenuOpen += OnMenuOpen;
		Menu.OnItemSelect += OnMenuItemSelect;
		MainMenuScript.MainMenu.OnItemSelect += OnMainMenuItemSelect;
	}

	public static void CloseAllMenus()
	{
		Menu.CloseMenu();
		foreach (Menu value in subMenus.Values)
		{
			value.CloseMenu();
		}
	}

	public static void ClearOutfitExceptTattoosAndHair()
	{
		List<string> list = new List<string>();
		string text = "";
		foreach (string item in Clothes.CurrentApparel.Items)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(item);
			if (clothingItemDefinition.Type == ClothingItemType.Tattoos)
			{
				list.Add(clothingItemDefinition.Id);
			}
			if (clothingItemDefinition.Type == ClothingItemType.Hairstyles)
			{
				text = clothingItemDefinition.Id;
			}
		}
		Clothes.CurrentApparel = Apparel.GetUnderwear();
		foreach (string item2 in list)
		{
			Clothes.CurrentApparel.Add(item2);
		}
		if (text != "")
		{
			Clothes.CurrentApparel.Add(text);
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (!(menuItem.ItemData as string == "clear"))
		{
			return;
		}
		if (CuffedScript.IsBeingCuffed || CuffedScript.IsCuffed)
		{
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
			return;
		}
		ClearOutfitExceptTattoosAndHair();
		Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		if (jobData.SeparateOutfit)
		{
			Sex freemodePedSex = Utils.GetFreemodePedSex(Game.PlayerPed);
			foreach (string item in from i in jobData.DefaultOutfits[freemodePedSex].Values.SelectMany((List<string> o) => o)
				where wardrobeCache.Contains(i)
				select i)
			{
				ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(item);
				if (clothingItemDefinition != null)
				{
					Utils.PreviewClothingItem(clothingItemDefinition);
				}
			}
		}
		Utils.StoreCurrentOutfit();
		await Clothes.SaveApparel();
	}

	private async void OnMenuOpen(Menu menu)
	{
		if (menu != Menu)
		{
			return;
		}
		if (CuffedScript.IsBeingCuffed || CuffedScript.IsCuffed)
		{
			Menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
			return;
		}
		int hash = ((Entity)Game.PlayerPed).Model.Hash;
		if (hash != API.GetHashKey("mp_m_freemode_01") && hash != API.GetHashKey("mp_f_freemode_01"))
		{
			Menu.Visible = false;
			Utils.DisplayHelpText("~r~You can't use the wardrobe when you're not the GTA:O character.");
		}
	}

	private async void OnMainMenuItemSelect(Menu menu, MenuItem selectedItem, int itemIndex)
	{
		if (selectedItem == MainMenuScript.MainMenuItems["wardrobe"])
		{
			RefreshMenu();
		}
	}

	private async void RefreshMenu()
	{
		Menu.ClearMenuItems();
		Menu.AddLoadingMenuItem();
		Menu.CounterPreText = LocalizationController.S(Entries.Main.LOADING) + "...";
		Utils.StoreCurrentOutfit();
		string job = Gtacnr.Client.API.Jobs.CachedJob;
		if (!Gtacnr.Data.Jobs.GetJobData(job).SeparateOutfit)
		{
			job = "none";
		}
		wardrobeCache = await Clothes.GetAllOwned(job);
		if (extraClothingItems.ContainsKey(""))
		{
			wardrobeCache.UnionWith(extraClothingItems[""]);
		}
		if (extraClothingItems.ContainsKey(job))
		{
			wardrobeCache.UnionWith(extraClothingItems[job]);
		}
		Menu.ClearMenuItems();
		subMenus.Clear();
		Sex sex = ((!(((Entity)Game.PlayerPed).Model == Model.op_Implicit("mp_m_freemode_01"))) ? Sex.Female : Sex.Male);
		int num = 0;
		List<string> list = new List<string>();
		foreach (string item3 in from e in wardrobeCache
			where Gtacnr.Data.Items.IsClothingItemDefined(e)
			orderby Gtacnr.Data.Items.GetClothingItemDefinition(e).Type
			select e)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(item3);
			if (!clothingItemDefinition.HasSex(sex) || list.Contains(item3) || clothingItemDefinition.Type == ClothingItemType.Tattoos)
			{
				continue;
			}
			num++;
			if (!subMenus.ContainsKey($"cat{clothingItemDefinition.Type}"))
			{
				string text = Gtacnr.Utils.ResolveLocalization(Gtacnr.Utils.GetDescription(clothingItemDefinition.Type));
				if (clothingItemDefinition.Type == ClothingItemType.Staff)
				{
					text = "~g~" + text;
				}
				MenuItem menuItem = (menuItems[$"cat{clothingItemDefinition.Type}"] = new MenuItem(text)
				{
					Label = "›",
					ItemData = clothingItemDefinition.Type
				});
				MenuItem menuItem3 = menuItem;
				Menu.AddMenuItem(menuItem3);
				Menu menu = new Menu(LocalizationController.S(Entries.Player.MENU_WARDROBE_TITLE), text)
				{
					PlaySelectSound = false
				};
				subMenus[$"cat{clothingItemDefinition.Type}"] = menu;
				MenuController.AddSubmenu(Menu, menu);
				MenuController.BindMenuItem(Menu, menu, menuItem3);
				menu.InstructionalButtons.Clear();
				menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Player.BTN_WARDROBE_WEAR));
				menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
				menu.ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[2]
				{
					new Menu.ButtonPressHandler((Control)204, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnWardrobeItemGive, disableControl: true),
					new Menu.ButtonPressHandler((Control)214, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnWardrobeItemDiscard, disableControl: true)
				});
				menu.OnItemSelect += OnWardrobeItemWear;
				menu.OnIndexChange += OnWardrobeItemChanged;
				menu.OnMenuOpen += OnWardrobeCategoryOpened;
				menu.OnMenuClose += OnWardrobeCategoryClosed;
			}
			Menu menu2 = subMenus[$"cat{clothingItemDefinition.Type}"];
			MenuItem item = clothingItemDefinition.ToMenuItem();
			menu2.AddMenuItem(item);
			list.Add(item3);
		}
		Menu.CounterPreText = $"{num} items";
		MenuItem item2 = new MenuItem(LocalizationController.S(Entries.Player.MENU_WARDROBE_CLEAR_TEXT), LocalizationController.S(Entries.Player.MENU_WARDROBE_CLEAR_DESCRIPTION))
		{
			ItemData = "clear"
		};
		Menu.AddMenuItem(item2);
		Menu.InstructionalButtons.Clear();
		Menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Player.BTN_WARDROBE_BROWSE));
		Menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		Menu.InstructionalButtons.Add((Control)204, LocalizationController.S(Entries.Player.BTN_WARDROBE_REMOVE));
		Menu.ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[2]
		{
			new Menu.ButtonPressHandler((Control)204, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnWardrobeRemove, disableControl: true),
			new Menu.ButtonPressHandler((Control)206, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnWardrobeSearch, disableControl: true)
		});
		Menu.InstructionalButtons.Add((Control)206, LocalizationController.S(Entries.Main.BTN_SEARCH));
	}

	private void OnWardrobeItemChanged(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		Utils.RestoreOutfit();
		if (CuffedScript.IsBeingCuffed || CuffedScript.IsCuffed)
		{
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
		}
		else if (newItem != null && newItem.ItemData is ClothingItem clothingItem)
		{
			Utils.StoreCurrentOutfit();
			Utils.PreviewClothingItem(clothingItem);
		}
	}

	private void OnWardrobeCategoryOpened(Menu menu)
	{
		if (CuffedScript.IsBeingCuffed || CuffedScript.IsCuffed)
		{
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
		}
		else if (menu.GetCurrentMenuItem() != null && menu.GetCurrentMenuItem().ItemData is ClothingItem clothingItem)
		{
			Utils.StoreCurrentOutfit();
			Utils.PreviewClothingItem(clothingItem);
		}
	}

	private void OnWardrobeCategoryClosed(Menu menu, MenuClosedEventArgs e)
	{
		Utils.RestoreOutfit();
	}

	private async void OnWardrobeItemWear(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (CuffedScript.IsBeingCuffed || CuffedScript.IsCuffed)
		{
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
			return;
		}
		object itemData = menuItem.ItemData;
		if (!(itemData is ClothingItem clothingItem))
		{
			return;
		}
		if (clothingItem.Disabled)
		{
			Utils.PlayErrorSound();
			return;
		}
		Utils.PreviewClothingItem(clothingItem);
		Utils.StoreCurrentOutfit();
		Clothes.CurrentApparel.Replace(clothingItem.Id);
		await Clothes.SaveApparel();
		Utils.PlaySelectSound();
		if (itemMottos.ContainsKey(clothingItem.Id))
		{
			Utils.DisplaySubtitle(itemMottos[clothingItem.Id]);
		}
	}

	private void OnWardrobeItemGive(Menu menu, Control control)
	{
		Utils.PlayErrorSound();
	}

	private void OnWardrobeItemDiscard(Menu menu, Control control)
	{
		Utils.PlayErrorSound();
	}

	private async void OnWardrobeRemove(Menu menu, Control control)
	{
		if (CuffedScript.IsBeingCuffed || CuffedScript.IsCuffed)
		{
			menu.Visible = false;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
		}
		else if (Gtacnr.Utils.CheckTimePassed(lastWardrobeRemove, 50.0))
		{
			if (menu.GetCurrentMenuItem().ItemData is ClothingItemType itemType && Clothes.CurrentApparel.Remove(itemType))
			{
				lastWardrobeRemove = DateTime.UtcNow;
				Utils.StoreCurrentOutfit();
				await Clothes.SaveApparel();
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
	}

	private async void OnWardrobeSearch(Menu menu, Control control)
	{
		await SearchWardrobe();
	}

	public static void AddExtraClothingItem(string job, string itemName, string motto)
	{
		if (!extraClothingItems.ContainsKey(job))
		{
			extraClothingItems[job] = new HashSet<string>();
		}
		if (!extraClothingItems[job].Contains(itemName))
		{
			extraClothingItems[job].Add(itemName);
		}
		itemMottos[itemName] = motto;
	}

	public static bool RemoveExtraClothingItem(string job, string itemName)
	{
		if (!extraClothingItems.ContainsKey(job))
		{
			return false;
		}
		return extraClothingItems[job].Remove(itemName);
	}

	private void OnStaffLevelInitializedOrChanged(object sender, StaffLevelArgs e)
	{
		if ((int)e.PreviousStaffLevel >= 10)
		{
			foreach (ClothingItem item in from ci in Gtacnr.Data.Items.GetAllClothingItemDefinitions()
				where (int)e.PreviousStaffLevel >= (int)ci.RequiredStaffLevel && (int)ci.RequiredStaffLevel > 0
				select ci)
			{
				RemoveExtraClothingItem("", item.Id);
			}
		}
		if ((int)e.NewStaffLevel < 10)
		{
			return;
		}
		foreach (ClothingItem item2 in from ci in Gtacnr.Data.Items.GetAllClothingItemDefinitions()
			where (int)e.NewStaffLevel >= (int)ci.RequiredStaffLevel && (int)ci.RequiredStaffLevel > 0
			select ci)
		{
			AddExtraClothingItem("", item2.Id, "The ~g~CnR Staff ~s~is on ~r~TOP~s~!");
		}
	}

	private async void OnJobChanged(object sender, JobArgs e)
	{
		while (!CustomScript.DataLoaded)
		{
			await BaseScript.Delay(0);
		}
		Menu.Visible = false;
		foreach (Menu value in subMenus.Values)
		{
			value.Visible = false;
		}
		wardrobeCache = await Clothes.GetAllOwned(e.CurrentJobId);
		foreach (string item in wardrobeCache)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(item);
			if (clothingItemDefinition != null && clothingItemDefinition.Type == ClothingItemType.Tattoos)
			{
				Clothes.CurrentApparel.Add(item);
			}
		}
		foreach (string item2 in Clothes.CurrentApparel.Items.ToList())
		{
			if (!wardrobeCache.Contains(item2) && !HasExtraClothingItem(e.CurrentJobId, item2))
			{
				Clothes.CurrentApparel.Remove(item2);
			}
		}
	}

	public static bool HasExtraClothingItem(string job, string itemName)
	{
		if (extraClothingItems.ContainsKey("") && extraClothingItems[""].Contains(itemName))
		{
			return true;
		}
		if (!extraClothingItems.ContainsKey(job))
		{
			return false;
		}
		return extraClothingItems[job].Contains(itemName);
	}

	private async Task SearchWardrobe()
	{
		string text = await Utils.GetUserInput(LocalizationController.S(Entries.Player.INPUT_WARDROBE_SEARCH_TITLE), LocalizationController.S(Entries.Player.INPUT_WARDROBE_SEARCH_TEXT), "", 50);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		Menu menu = new Menu(LocalizationController.S(Entries.Player.MENU_WARDROBE_TITLE), LocalizationController.S(Entries.Player.MENU_WARDROBE_SEARCH_SUBTITLE, text))
		{
			PlaySelectSound = false
		};
		subMenus["catSearch"] = menu;
		menu.InstructionalButtons.Clear();
		menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Player.BTN_WARDROBE_WEAR));
		menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		menu.ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[2]
		{
			new Menu.ButtonPressHandler((Control)204, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnWardrobeItemGive, disableControl: true),
			new Menu.ButtonPressHandler((Control)214, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnWardrobeItemDiscard, disableControl: true)
		});
		menu.OnItemSelect += OnWardrobeItemWear;
		menu.OnIndexChange += OnWardrobeItemChanged;
		menu.OnMenuOpen += OnWardrobeCategoryOpened;
		menu.OnMenuClose += OnWardrobeCategoryClosed;
		List<string> list = new List<string>();
		Sex sex = ((!(((Entity)Game.PlayerPed).Model == Model.op_Implicit("mp_m_freemode_01"))) ? Sex.Female : Sex.Male);
		foreach (string item2 in wardrobeCache)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(item2);
			if (clothingItemDefinition == null || !clothingItemDefinition.HasSex(sex) || list.Contains(item2))
			{
				continue;
			}
			string id = clothingItemDefinition.Id;
			if (id != null && !id.ToLowerInvariant().Contains(text.ToLowerInvariant()))
			{
				string name = clothingItemDefinition.Name;
				if (name != null && !name.ToLowerInvariant().Contains(text.ToLowerInvariant()))
				{
					string description = clothingItemDefinition.Description;
					if (description != null && !description.ToLowerInvariant().Contains(text.ToLowerInvariant()))
					{
						continue;
					}
				}
			}
			MenuItem item = clothingItemDefinition.ToMenuItem();
			menu.AddMenuItem(item);
		}
		MenuController.CloseAllMenus();
		MenuController.AddSubmenu(Menu, menu);
		menu.OpenMenu();
		Utils.PlaySelectSound();
	}
}
