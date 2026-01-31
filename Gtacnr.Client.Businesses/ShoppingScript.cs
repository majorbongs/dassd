using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.HUD;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Inventory;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Premium;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Businesses;

public class ShoppingScript : Script
{
	[Flags]
	private enum SpecialAttributes
	{
		None = 0,
		New = 1,
		Limited = 2
	}

	private static Dictionary<BusinessSupplyType, float> salesTaxRates = Gtacnr.Utils.LoadJson<Dictionary<BusinessSupplyType, float>>("data/businesses/taxes.json");

	private static Dictionary<string, List<BusinessSupply>> businessSupplies;

	private static Dictionary<string, List<BusinessSupply>> businessDemands;

	private static Menu mainMenu;

	private bool canPlayerSteal = true;

	private bool purchaseInProgress;

	private bool isBusy;

	private bool purchaseItemTipShown;

	private bool purchaseStockTipShown;

	private bool purchaseClothingTipShown;

	private bool purchaseArmoryTipShown;

	private bool purchaseIllegalGunTipShown;

	private int cameraHandle;

	private bool isCamTaskAttached;

	private float startHealth;

	private int previewPropHandle;

	private float previewPropRotSpeed;

	private int lastPropHash;

	private int lastWeaponHash;

	private int lastAttachmentHash;

	private Vector4 prevPedPos;

	private bool isInPropView;

	private bool isInClothesView;

	private Job jobData;

	private JobsEnum jobsEnum;

	private long moneyCache;

	private int levelCache;

	private HashSet<string> wardrobeCache;

	private Dictionary<string, float> supplyStockCache;

	private DateTime shopliftTimestamp;

	private Dictionary<string, DateTime> shopliftBusinessTimestamp = new Dictionary<string, DateTime>();

	private readonly TimeSpan SHOPLIFT_COOLDOWN = TimeSpan.FromSeconds(10.0);

	private readonly TimeSpan SHOPLIFT_CAUGHT_COOLDOWN = TimeSpan.FromMinutes(10.0);

	private static ShoppingScript script;

	private static Business ActiveBusiness;

	private int bizCamTaskTickCount;

	private float lastRotZ;

	private static Dictionary<BusinessType, List<MenuItem>> externalMenuItemsBefore = new Dictionary<BusinessType, List<MenuItem>>();

	private static Dictionary<BusinessType, List<MenuItem>> externalMenuItemsAfter = new Dictionary<BusinessType, List<MenuItem>>();

	private static Dictionary<BusinessType, List<Action<Menu>>> externalMenuOpenHandlers = new Dictionary<BusinessType, List<Action<Menu>>>();

	private static List<Action<Menu, MenuItem, int>> externalItemSelectHandlers = new List<Action<Menu, MenuItem, int>>();

	private static List<Action<Menu, MenuItem, int, int>> externalListItemSelectHandlers = new List<Action<Menu, MenuItem, int, int>>();

	public static Dictionary<string, List<BusinessSupply>> Supplies => businessSupplies;

	public static Dictionary<string, List<BusinessSupply>> Demands => businessDemands;

	public static bool IsInPropPreview { get; private set; }

	public static event EventHandler MenuOpening;

	public static event EventHandler ItemPurchased;

	public static float GetTax(BusinessSupplyType type)
	{
		if (!salesTaxRates.ContainsKey(type))
		{
			return 0f;
		}
		return salesTaxRates[type];
	}

	public ShoppingScript()
	{
		script = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override async void OnStarted()
	{
		mainMenu = new Menu("", "")
		{
			PlaySelectSound = false,
			MaxDistance = 10f
		};
		MenuController.AddMenu(mainMenu);
		RefreshInstructionalButtons(mainMenu, canPurchase: true, canSteal: true);
		RefreshMenuItemActions(mainMenu, canPurchase: true, canSteal: true);
		mainMenu.OnMenuOpen += OnMenuOpened;
		mainMenu.OnMenuClose += OnMenuClosed;
		mainMenu.OnIndexChange += OnMenuIndexChanged;
		LoadSuppliesAndDemands();
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		jobData = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId);
		jobsEnum = e.CurrentJobEnum;
		canPlayerSteal = !jobsEnum.IsPublicService();
	}

	private void LoadSuppliesAndDemands()
	{
		LoadSupplies();
		LoadDemands();
		static void LoadDemands()
		{
			List<string> list = Gtacnr.Utils.LoadJson<List<string>>("data/businesses/demands/files.json");
			businessDemands = new Dictionary<string, List<BusinessSupply>>();
			foreach (string item in list)
			{
				foreach (KeyValuePair<string, List<BusinessSupply>> item2 in Gtacnr.Utils.LoadJson<Dictionary<string, List<BusinessSupply>>>("data/businesses/demands/" + item))
				{
					string key = item2.Key;
					List<BusinessSupply> value = item2.Value;
					if (!businessDemands.ContainsKey(key))
					{
						businessDemands[key] = new List<BusinessSupply>();
					}
					businessDemands[key].AddRange(value);
				}
			}
		}
		void LoadSupplies()
		{
			List<string> list = Gtacnr.Utils.LoadJson<List<string>>("data/businesses/supplies/files.json");
			businessSupplies = new Dictionary<string, List<BusinessSupply>>();
			foreach (string item3 in list)
			{
				foreach (KeyValuePair<string, List<BusinessSupply>> item4 in Gtacnr.Utils.LoadJson<Dictionary<string, List<BusinessSupply>>>("data/businesses/supplies/" + item3))
				{
					string key = item4.Key;
					List<BusinessSupply> value = item4.Value;
					AddSalesTax(value);
					if (!businessSupplies.ContainsKey(key))
					{
						businessSupplies[key] = new List<BusinessSupply>();
					}
					businessSupplies[key].AddRange(value);
				}
			}
		}
	}

	private void LoadSpecialEventsSuppliesFile(string eventName)
	{
		Print("Loading special event supplies file `" + eventName + "`");
		foreach (KeyValuePair<string, List<BusinessSupply>> item in Gtacnr.Utils.LoadJson<Dictionary<string, List<BusinessSupply>>>("data/specialEvents/" + eventName + "/supplies.json"))
		{
			AddSalesTax(item.Value);
			if (!businessSupplies.ContainsKey(item.Key))
			{
				businessSupplies[item.Key] = new List<BusinessSupply>();
			}
			businessSupplies[item.Key].InsertRange(0, item.Value);
		}
	}

	private void AddSalesTax(IEnumerable<BusinessSupply> supplies)
	{
		foreach (BusinessSupply supply in supplies)
		{
			if (salesTaxRates.ContainsKey(supply.Type))
			{
				supply.SalesTax = salesTaxRates[supply.Type];
			}
		}
	}

	[EventHandler("gtacnr:halloween:initialize")]
	private void OnHalloween()
	{
		LoadSpecialEventsSuppliesFile("halloween");
	}

	[EventHandler("gtacnr:christmas:initialize")]
	private void OnChristmas()
	{
		LoadSpecialEventsSuppliesFile("xmas");
	}

	private void OpenMenu()
	{
		Menus.CloseAll();
		if (!mainMenu.Visible)
		{
			Utils.StoreCurrentOutfit();
			MoneyDisplayScript.ForceMoneyDisplay = true;
			mainMenu.OpenMenu();
		}
	}

	private void RefreshMenuActions(Menu menu, MenuItem menuItem)
	{
		bool canSteal = false;
		bool flag = false;
		if (menuItem.ItemData is BusinessSupply businessSupply)
		{
			BusinessTypeMetadata businessTypeMetadata = BusinessScript.BusinessTypes[BusinessScript.ClosestBusiness.Type.ToString()];
			flag = true;
			canSteal = flag && canPlayerSteal && businessTypeMetadata.CanShoplift && !businessSupply.IsJobSupply;
		}
		RefreshInstructionalButtons(menu, flag, canSteal);
		RefreshMenuItemActions(menu, flag, canSteal);
	}

	private void RefreshInstructionalButtons(Menu menu, bool canPurchase, bool canSteal)
	{
		menu.InstructionalButtons.Clear();
		if (canPurchase)
		{
			menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Businesses.MENU_STORE_BTN_PURCHASE));
		}
		else
		{
			menu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Businesses.MENU_STORE_BTN_BROWSE));
		}
		menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		if (canSteal)
		{
			menu.InstructionalButtons.Add((Control)29, LocalizationController.S(Entries.Businesses.MENU_STORE_BTN_STEAL));
		}
		if (isInClothesView)
		{
			menu.InstructionalButtons.Add((Control)37, LocalizationController.S(Entries.Businesses.MENU_STORE_BTN_ROTATE));
		}
	}

	private void RefreshMenuItemActions(Menu menu, bool canPurchase, bool canSteal)
	{
		menu.ButtonPressHandlers.Clear();
		menu.OnItemSelect -= OnItemSelect;
		menu.OnListItemSelect -= OnListItemSelect;
		menu.OnListIndexChange -= OnListIndexChange;
		menu.OnItemSelect += OnItemSelect;
		menu.OnListItemSelect += OnListItemSelect;
		menu.OnListIndexChange += OnListIndexChange;
		if (canSteal)
		{
			menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)29, Menu.ControlPressCheckType.JUST_PRESSED, OnStoreShoplift, disableControl: true));
		}
	}

	private void OnListIndexChange(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		RefreshSupplyMenuItem(listItem);
	}

	private void RefreshAllSupplyMenuItems(Menu menu)
	{
		menu.ShowWeaponStatsPanel = false;
		foreach (MenuItem menuItem in menu.GetMenuItems())
		{
			RefreshSupplyMenuItem(menuItem);
		}
	}

	private async void RefreshSupplyMenuItem(MenuItem menuItem)
	{
		Menu parentMenu = menuItem.ParentMenu;
		MenuItem currentMenuItem = parentMenu.GetCurrentMenuItem();
		MembershipTier currentMembershipTier = MembershipScript.GetCurrentMembershipTier();
		Business activeBusiness = ActiveBusiness;
		BusinessSupply supply = null;
		bool isCategory = false;
		if (menuItem.ItemData is BusinessSupply businessSupply)
		{
			supply = businessSupply;
		}
		else if (menuItem.ItemData is Tuple<BusinessSupply> tuple)
		{
			supply = tuple.Item1;
			isCategory = true;
		}
		if (supply == null)
		{
			return;
		}
		InventoryItemBase itemInfo = Gtacnr.Data.Items.GetItemBaseDefinition(supply.Item);
		float multiplier = ActiveBusiness.GetMultiplier(itemInfo.Category.ToString());
		bool flag = levelCache >= itemInfo.RequiredLevel;
		bool flag2 = (int)currentMembershipTier >= (int)itemInfo.RequiredMembership;
		bool flag3 = itemInfo.IsPoliceOnly && !jobsEnum.IsPolice();
		bool flag4 = !itemInfo.CanBeUsedByEMS && jobsEnum.IsEMSOrFD();
		menuItem.Description = itemInfo.Description;
		menuItem.Enabled = (flag && flag2 && !flag3 && !flag4) || isCategory;
		menuItem.RightIcon = ((!menuItem.Enabled) ? MenuItem.Icon.LOCK : MenuItem.Icon.NONE);
		SpecialAttributes specialAttributes = AddSpecialAttributes(menuItem, supply);
		if (menuItem.Text.Length > 32)
		{
			bool flag5 = specialAttributes.HasFlag(SpecialAttributes.New);
			bool flag6 = specialAttributes.HasFlag(SpecialAttributes.Limited);
			int num = 0;
			if (flag5)
			{
				num += LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_NEW).Length + 1;
			}
			if (flag6)
			{
				num += LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_LIMITED).Length + 1;
			}
			menuItem.Text = itemInfo.Name.Substring(0, Math.Min(itemInfo.Name.Length, 29 - num)) + "...";
			menuItem.Description = itemInfo.Name ?? "";
			if (!API.IsStringNullOrEmpty(itemInfo.Description))
			{
				MenuItem menuItem2 = menuItem;
				menuItem2.Description = menuItem2.Description + "\n" + itemInfo.Description;
			}
			if (flag5)
			{
				MenuItem menuItem3 = menuItem;
				menuItem3.Text = menuItem3.Text + " ~y~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_NEW);
			}
			if (flag6)
			{
				MenuItem menuItem4 = menuItem;
				menuItem4.Text = menuItem4.Text + " ~r~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_LIMITED);
			}
		}
		int price = supply.CalculateFinalPrice(activeBusiness.PriceMultiplier * multiplier);
		bool flag7 = moneyCache >= price;
		menuItem.Label = (isCategory ? Utils.MENU_ARROW : price.ToPriceTagString(moneyCache));
		if (!flag7 && !isCategory)
		{
			menuItem.Enabled = false;
		}
		if (itemInfo.RequiredLevel > 1)
		{
			MenuItem menuItem5 = menuItem;
			menuItem5.Description = menuItem5.Description + NewLineIfDescNotEmpty() + "~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, itemInfo.RequiredLevel);
		}
		if ((int)itemInfo.RequiredMembership > 0)
		{
			if ((int)currentMembershipTier < (int)itemInfo.RequiredMembership)
			{
				MenuItem menuItem6 = menuItem;
				menuItem6.Description = menuItem6.Description + NewLineIfDescNotEmpty() + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(itemInfo.RequiredMembership), ExternalLinks.Collection.Store);
			}
			else
			{
				MenuItem menuItem7 = menuItem;
				menuItem7.Description = menuItem7.Description + NewLineIfDescNotEmpty() + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(currentMembershipTier));
				if ((int)currentMembershipTier > (int)itemInfo.RequiredMembership)
				{
					MenuItem menuItem8 = menuItem;
					menuItem8.Description = menuItem8.Description + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REQUIRES_LOWER_TIER, Gtacnr.Utils.GetDescription(itemInfo.RequiredMembership));
				}
			}
			if (flag7)
			{
				menuItem.Label = ChangeColor(menuItem.Label, "~p~");
			}
		}
		if (!isCategory)
		{
			if (itemInfo.CreationDate > default(DateTime) && DateTime.UtcNow < itemInfo.CreationDate)
			{
				menuItem.Enabled = false;
			}
			if (itemInfo.DisabledDate < DateTime.MaxValue && DateTime.UtcNow > itemInfo.DisabledDate)
			{
				menuItem.Enabled = false;
			}
			if (itemInfo.Credits != null)
			{
				MenuItem menuItem9 = menuItem;
				menuItem9.Description = menuItem9.Description + NewLineIfDescNotEmpty() + "~y~Credits: ~s~" + itemInfo.Credits;
			}
			if (itemInfo.IsIllegal || (itemInfo.IsIllegalForSelling && supply.IsJobSupply))
			{
				MenuItem menuItem10 = menuItem;
				menuItem10.Description = menuItem10.Description + NewLineIfDescNotEmpty() + "~r~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_ILLEGAL);
			}
			if (itemInfo.Rarity > ItemRarity.Common)
			{
				MenuItem menuItem11 = menuItem;
				menuItem11.Description = menuItem11.Description + NewLineIfDescNotEmpty() + itemInfo.Rarity.ToMenuItemDescription();
			}
			if (flag3)
			{
				MenuItem menuItem12 = menuItem;
				menuItem12.Description = menuItem12.Description + NewLineIfDescNotEmpty() + "~y~Only police officers can obtain this item.";
			}
			if (flag4)
			{
				MenuItem menuItem13 = menuItem;
				menuItem13.Description = menuItem13.Description + NewLineIfDescNotEmpty() + "~y~Paramedics cannot obtain this item.";
			}
			if (activeBusiness.LimitedStock && supplyStockCache.ContainsKey(supply.Item))
			{
				if (supplyStockCache[supply.Item] <= 0f)
				{
					MenuItem menuItem14 = menuItem;
					menuItem14.Description = menuItem14.Description + NewLineIfDescNotEmpty() + LocalizationController.S(Entries.Businesses.STP_ITEM_NO_STOCK) + "~s~";
					menuItem.Enabled = false;
				}
				else if (supplyStockCache[supply.Item] <= 5f)
				{
					MenuItem menuItem15 = menuItem;
					menuItem15.Description = menuItem15.Description + NewLineIfDescNotEmpty() + LocalizationController.S(Entries.Businesses.STP_ITEM_LOW_STOCK, supplyStockCache[supply.Item].ToString("0.##")) + "~s~";
				}
				else
				{
					MenuItem menuItem16 = menuItem;
					menuItem16.Description = menuItem16.Description + NewLineIfDescNotEmpty() + LocalizationController.S(Entries.Businesses.STP_ITEM_IN_STOCK, supplyStockCache[supply.Item].ToString("0.##")) + "~s~";
				}
			}
		}
		if (supply.Type == BusinessSupplyType.Item)
		{
			InventoryEntry inventoryEntry = (supply.IsJobSupply ? StockMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemInfo.Id) : InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemInfo.Id));
			float num2 = ((!supply.IsJobSupply) ? itemInfo.Limit : ((itemInfo.JobLimits?.ContainsKey(jobData.Id) ?? false) ? itemInfo.JobLimits[jobData.Id] : 0f));
			if (!isCategory && inventoryEntry != null && num2 > 0f)
			{
				if (inventoryEntry.Amount + 1f > num2)
				{
					menuItem.Enabled = false;
					menuItem.RightIcon = MenuItem.Icon.ARMOR;
					menuItem.Label = ChangeColor(menuItem.Label);
					if (menuItem is MenuListItem menuListItem)
					{
						menuListItem.DisableDrawList = true;
					}
				}
				if (menuItem is MenuListItem { ListIndex: var listIndex } menuListItem2)
				{
					float num3 = ((supply.PurchaseAmounts.Count > 1) ? supply.PurchaseAmounts[listIndex] : ((supply.PurchaseSupplies.Count > 1) ? supply.PurchaseSupplies[listIndex].Amount : 0f));
					if (inventoryEntry.Amount + num3 > num2)
					{
						if (!menuListItem2.ListItems[listIndex].StartsWith("~"))
						{
							menuListItem2.ListItems[listIndex] = "~m~" + menuListItem2.ListItems[listIndex];
						}
					}
					else if (menuListItem2.ListItems[listIndex].Contains("~m~"))
					{
						menuListItem2.ListItems[listIndex] = menuListItem2.ListItems[listIndex].Replace("~m~", "");
					}
					int price2 = ((supply.PurchaseAmounts.Count > 1) ? supply.CalculateFinalPrice(activeBusiness.PriceMultiplier * multiplier) : ((supply.PurchaseSupplies.Count > 1) ? supply.PurchaseSupplies[listIndex].CalculateFinalUnitPrice(activeBusiness.PriceMultiplier * multiplier) : 0));
					string text = LocalizationController.S(Entries.Businesses.MENU_STORE_UNIT_PRICE, price2.ToPriceTagString(moneyCache), itemInfo.Unit ?? "piece");
					MenuItem menuItem17 = menuItem;
					menuItem17.Description = menuItem17.Description + NewLineIfDescNotEmpty() + "~s~" + text;
				}
			}
		}
		else if (supply.Type == BusinessSupplyType.Weapon)
		{
			uint hashKey = (uint)API.GetHashKey(supply.Item);
			WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(supply.Item);
			bool flag8 = true;
			if (supply.Extra != null && Gtacnr.Data.Items.IsAmmoDefined((string)supply.Extra))
			{
				AmmoDefinition ammoDefinition = Gtacnr.Data.Items.GetAmmoDefinition((string)supply.Extra);
				int hashKey2 = API.GetHashKey((string)supply.Extra);
				int pedAmmoByType = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, hashKey2);
				if (pedAmmoByType > 0)
				{
					MenuItem menuItem18 = menuItem;
					menuItem18.Description = menuItem18.Description + $"{NewLineIfDescNotEmpty()}~g~{pedAmmoByType}" + ((ammoDefinition.Limit > 0f) ? $"/{ammoDefinition.Limit}" : "") + " Owned";
				}
				if (!isCategory && ammoDefinition.Limit > 0f)
				{
					if ((float)(pedAmmoByType + 1) > ammoDefinition.Limit)
					{
						menuItem.Enabled = false;
						menuItem.RightIcon = MenuItem.Icon.AMMO;
						menuItem.Label = ChangeColor(menuItem.Label);
						if (menuItem is MenuListItem menuListItem3)
						{
							menuListItem3.DisableDrawList = true;
						}
					}
					if (menuItem is MenuListItem { ListIndex: var listIndex2 } menuListItem4)
					{
						float f = ((supply.PurchaseAmounts.Count > 1) ? supply.PurchaseAmounts[listIndex2] : ((supply.PurchaseSupplies.Count > 1) ? supply.PurchaseSupplies[listIndex2].Amount : 0f));
						if ((float)(pedAmmoByType + f.ToInt()) > ammoDefinition.Limit)
						{
							if (!menuListItem4.ListItems[listIndex2].StartsWith("~"))
							{
								menuListItem4.ListItems[listIndex2] = "~m~" + menuListItem4.ListItems[listIndex2];
							}
						}
						else if (menuListItem4.ListItems[listIndex2].Contains("~m~"))
						{
							menuListItem4.ListItems[listIndex2] = menuListItem4.ListItems[listIndex2].Replace("~m~", "");
						}
						int price3 = ((supply.PurchaseAmounts.Count > 1) ? supply.CalculateFinalPrice(activeBusiness.PriceMultiplier * multiplier) : ((supply.PurchaseSupplies.Count > 1) ? supply.PurchaseSupplies[listIndex2].CalculateFinalUnitPrice(activeBusiness.PriceMultiplier * multiplier) : 0));
						string text2 = LocalizationController.S(Entries.Businesses.MENU_STORE_UNIT_PRICE, price3.ToPriceTagString(moneyCache), itemInfo.Unit ?? "piece");
						MenuItem menuItem19 = menuItem;
						menuItem19.Description = menuItem19.Description + NewLineIfDescNotEmpty() + "~s~" + text2;
					}
				}
				flag8 = false;
			}
			if (menuItem == currentMenuItem && weaponDefinition.Category != WeaponCategory.Other)
			{
				parentMenu.ShowWeaponStatsPanel = true;
				WeaponHudStats val = default(WeaponHudStats);
				Game.GetWeaponHudStats(hashKey, ref val);
				parentMenu.SetWeaponStats((float)val.hudDamage / 100f, (float)val.hudSpeed / 100f, (float)val.hudAccuracy / 100f, (float)val.hudRange / 100f);
				parentMenu.SetWeaponComponentStats(0f, 0f, 0f, 0f);
			}
			bool flag9 = ArmoryScript.HasWeapon((WeaponHash)hashKey);
			if (flag8 && flag9)
			{
				MenuItem menuItem20 = menuItem;
				menuItem20.Description = menuItem20.Description + NewLineIfDescNotEmpty() + "~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_OWNED);
				if (!isCategory)
				{
					menuItem.Enabled = false;
					menuItem.RightIcon = MenuItem.Icon.GUN;
					menuItem.Label = ChangeColor(menuItem.Label);
				}
			}
		}
		else if (supply.Type == BusinessSupplyType.Attachment && !isCategory)
		{
			Gtacnr.Data.Items.GetWeaponComponentDefinition(supply.Item);
			uint hashKey3 = (uint)API.GetHashKey(supply.Item);
			int hashKey4 = API.GetHashKey((string)supply.Extra);
			bool flag10 = ArmoryScript.HasWeapon((WeaponHash)hashKey4);
			bool flag11 = ArmoryScript.HasAttachment((WeaponHash)hashKey4, (WeaponComponentHash)hashKey3);
			if (menuItem == currentMenuItem)
			{
				parentMenu.ShowWeaponStatsPanel = true;
				WeaponComponentHudStats val2 = default(WeaponComponentHudStats);
				Game.GetWeaponComponentHudStats(hashKey3, ref val2);
				parentMenu.SetWeaponComponentStats((float)val2.hudDamage / 100f, (float)val2.hudSpeed / 100f, (float)val2.hudAccuracy / 100f, (float)val2.hudRange / 100f);
			}
			if (!isCategory && flag11)
			{
				MenuItem menuItem21 = menuItem;
				menuItem21.Description = menuItem21.Description + NewLineIfDescNotEmpty() + "~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_OWNED);
				menuItem.Enabled = false;
				menuItem.RightIcon = MenuItem.Icon.GUN;
				menuItem.Label = ChangeColor(menuItem.Label);
			}
			if (!isCategory && !flag10)
			{
				MenuItem menuItem22 = menuItem;
				menuItem22.Description = menuItem22.Description + NewLineIfDescNotEmpty() + "~y~" + LocalizationController.S(Entries.Businesses.MENU_STORE_NO_GUN_FOR_ATTACHMENT) + "~s~";
				menuItem.Enabled = false;
			}
		}
		else if (supply.Type == BusinessSupplyType.Ammo && !isCategory)
		{
			AmmoDefinition ammoInfo = Gtacnr.Data.Items.GetAmmoDefinition(supply.Item);
			bool flag12 = (from w in Gtacnr.Data.Items.GetAllWeaponDefinitions()
				where ArmoryScript.HasWeapon((WeaponHash)w.Hash)
				select w).Any((WeaponDefinition w) => API.GetPedAmmoTypeFromWeapon(((PoolObject)Game.PlayerPed).Handle, (uint)w.Hash) == ammoInfo.Hash);
			int pedAmmoByType2 = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, ammoInfo.Hash);
			MenuItem menuItem22 = menuItem;
			menuItem22.Description = menuItem22.Description + $"{NewLineIfDescNotEmpty()}~g~{pedAmmoByType2}" + ((ammoInfo.Limit > 0f) ? $"/{ammoInfo.Limit}" : "") + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_OWNED) + "~s~";
			if (!isCategory && !flag12)
			{
				menuItem22 = menuItem;
				menuItem22.Description = menuItem22.Description + NewLineIfDescNotEmpty() + "~y~" + LocalizationController.S(Entries.Businesses.MENU_STORE_NO_GUN_FOR_AMMO_TYPE) + "~s~";
				menuItem.Enabled = false;
			}
			if (!isCategory && ammoInfo.Limit > 0f)
			{
				int pedAmmoByType3 = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, API.GetHashKey(supply.Item));
				if ((float)(pedAmmoByType3 + 1) > ammoInfo.Limit)
				{
					menuItem.Enabled = false;
					menuItem.RightIcon = MenuItem.Icon.AMMO;
					menuItem.Label = ChangeColor(menuItem.Label);
					if (menuItem is MenuListItem menuListItem5)
					{
						menuListItem5.DisableDrawList = true;
					}
				}
				if (menuItem is MenuListItem { ListIndex: var listIndex3 } menuListItem6)
				{
					float f2 = ((supply.PurchaseAmounts.Count > 1) ? supply.PurchaseAmounts[listIndex3] : ((supply.PurchaseSupplies.Count > 1) ? supply.PurchaseSupplies[listIndex3].Amount : 0f));
					if ((float)(pedAmmoByType3 + f2.ToInt()) > ammoInfo.Limit)
					{
						if (!menuListItem6.ListItems[listIndex3].StartsWith("~"))
						{
							menuListItem6.ListItems[listIndex3] = "~m~" + menuListItem6.ListItems[listIndex3];
						}
					}
					else if (menuListItem6.ListItems[listIndex3].Contains("~m~"))
					{
						menuListItem6.ListItems[listIndex3] = menuListItem6.ListItems[listIndex3].Replace("~m~", "");
					}
					int price4 = ((supply.PurchaseAmounts.Count > 1) ? supply.CalculateFinalPrice(activeBusiness.PriceMultiplier * multiplier) : ((supply.PurchaseSupplies.Count > 1) ? supply.PurchaseSupplies[listIndex3].CalculateFinalUnitPrice(activeBusiness.PriceMultiplier * multiplier) : 0));
					string text3 = LocalizationController.S(Entries.Businesses.MENU_STORE_UNIT_PRICE, price4.ToPriceTagString(moneyCache), itemInfo.Unit ?? "piece");
					MenuItem menuItem23 = menuItem;
					menuItem23.Description = menuItem23.Description + NewLineIfDescNotEmpty() + "~s~" + text3;
				}
			}
		}
		else if (supply.Type == BusinessSupplyType.Clothing && !isCategory)
		{
			if (wardrobeCache == null && jobData != null)
			{
				wardrobeCache = await Clothes.GetAllOwned(jobData.SeparateOutfit ? jobData.Id : "none");
			}
			if (!isCategory && wardrobeCache != null && wardrobeCache.Contains(supply.Item))
			{
				if (Gtacnr.Data.Items.GetClothingItemDefinition(supply.Item).Type == ClothingItemType.Tattoos)
				{
					menuItem.RightIcon = MenuItem.Icon.TATTOO;
					menuItem.Label = LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REMOVE_FOR, price.ToPriceTagString(moneyCache));
				}
				else
				{
					menuItem.Enabled = false;
					menuItem.RightIcon = MenuItem.Icon.CLOTHING;
					MenuItem menuItem24 = menuItem;
					menuItem24.Description = menuItem24.Description + NewLineIfDescNotEmpty() + "~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_OWNED);
					menuItem.Label = ChangeColor(menuItem.Label);
				}
			}
		}
		if (string.IsNullOrEmpty(menuItem.Description))
		{
			menuItem.Description = " ";
		}
		RefreshMenuItemLimitedTimer(menuItem);
		static string ChangeColor(string input, string colorString = "~c~")
		{
			return colorString + input.RemoveGta5TextFormatting_();
		}
		string NewLineIfDescNotEmpty()
		{
			if (!string.IsNullOrEmpty(menuItem.Description))
			{
				return "~n~";
			}
			return "";
		}
	}

	private async Coroutine BusinessCameraTask()
	{
		bizCamTaskTickCount++;
		if (isInClothesView)
		{
			Game.DisableControlThisFrame(0, (Control)31);
			Game.DisableControlThisFrame(0, (Control)268);
			Game.DisableControlThisFrame(0, (Control)32);
			Game.DisableControlThisFrame(0, (Control)269);
			Game.DisableControlThisFrame(0, (Control)33);
			Game.DisableControlThisFrame(0, (Control)30);
			Game.DisableControlThisFrame(0, (Control)266);
			Game.DisableControlThisFrame(0, (Control)34);
			Game.DisableControlThisFrame(0, (Control)267);
			Game.DisableControlThisFrame(0, (Control)35);
			Game.DisableControlThisFrame(0, (Control)44);
			Game.DisableControlThisFrame(0, (Control)22);
			Game.DisableControlThisFrame(0, (Control)36);
			Game.DisableControlThisFrame(0, (Control)21);
			Game.DisableControlThisFrame(0, (Control)37);
			if (Game.IsDisabledControlJustPressed(0, (Control)37))
			{
				Ped playerPed = Game.PlayerPed;
				((Entity)playerPed).Heading = ((Entity)playerPed).Heading + 90f;
			}
		}
		if (previewPropHandle != 0)
		{
			Vector3 entityRotation = API.GetEntityRotation(previewPropHandle, 0);
			lastRotZ = entityRotation.Z + previewPropRotSpeed;
			API.SetEntityRotation(previewPropHandle, entityRotation.X, entityRotation.Y, lastRotZ, 0, false);
		}
		if (bizCamTaskTickCount % 10 != 0)
		{
			return;
		}
		if ((float)((Entity)Game.PlayerPed).Health < startHealth || CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsCuffed || !MenuController.IsAnyMenuOpen())
		{
			MenuController.CloseAllMenus();
			StopPropPreview();
			StopClothesPreview();
			return;
		}
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (!isInClothesView || !(player == Game.Player))
			{
				((Entity)player.Character).Opacity = 0;
			}
		}
	}

	private void CleanupPreviewProp()
	{
		if (previewPropHandle != 0 && API.DoesEntityExist(previewPropHandle))
		{
			API.DeleteEntity(ref previewPropHandle);
		}
	}

	private void StartPropPreview(Business business)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		if (!IsInPropPreview)
		{
			Vector3 val = ((business.PropPreviewData.ObjectPos_ != null) ? (business.PropPreviewData.ObjectPos + new Vector3(0f, 0f, business.PropPreviewData.ZOffset)) : (((Entity)BusinessScript.ClosestBusinessEmployee.State.Ped).Position + ((Entity)BusinessScript.ClosestBusinessEmployee.State.Ped).ForwardVector * business.PropPreviewData.ObjectDistance + new Vector3(0f, 0f, business.PropPreviewData.ZOffset)));
			Vector3 val2 = ((business.PropPreviewData.CameraPos_ != null) ? (business.PropPreviewData.CameraPos + new Vector3(0f, 0f, business.PropPreviewData.ZOffset + 0.25f)) : (((Entity)BusinessScript.ClosestBusinessEmployee.State.Ped).Position + ((Entity)BusinessScript.ClosestBusinessEmployee.State.Ped).ForwardVector * business.PropPreviewData.CameraDistance + new Vector3(0f, 0f, business.PropPreviewData.ZOffset + 0.25f)));
			previewPropRotSpeed = business.PropPreviewData.RotationSpeed;
			API.DestroyAllCams(true);
			cameraHandle = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", val2.X, val2.Y, val2.Z, 0f, 0f, 0f, 45f, false, 0);
			API.SetCamActive(cameraHandle, true);
			API.PointCamAtCoord(cameraHandle, val.X, val.Y, val.Z);
			API.RenderScriptCams(true, false, 2000, true, true);
			startHealth = ((Entity)Game.PlayerPed).Health;
			IsInPropPreview = true;
			((Entity)Game.PlayerPed).Opacity = 0;
			API.FreezeEntityPosition(((PoolObject)Game.PlayerPed).Handle, true);
			if (!isCamTaskAttached)
			{
				isCamTaskAttached = true;
				base.Update += BusinessCameraTask;
			}
		}
	}

	private void StopPropPreview()
	{
		if (!IsInPropPreview)
		{
			return;
		}
		if (cameraHandle != 0)
		{
			API.SetCamActive(cameraHandle, false);
			API.DestroyCam(cameraHandle, false);
			API.RenderScriptCams(false, false, 0, true, false);
			cameraHandle = 0;
		}
		CleanupPreviewProp();
		lastWeaponHash = 0;
		lastAttachmentHash = 0;
		if (isCamTaskAttached)
		{
			isCamTaskAttached = false;
			base.Update -= BusinessCameraTask;
		}
		((Entity)Game.PlayerPed).Opacity = 255;
		API.FreezeEntityPosition(((PoolObject)Game.PlayerPed).Handle, false);
		IsInPropPreview = false;
		foreach (Player player in ((BaseScript)this).Players)
		{
			((Entity)player.Character).Opacity = 255;
		}
	}

	private async void PreviewProp(BusinessSupply supply)
	{
		Business activeBusiness = ActiveBusiness;
		bool flag = false;
		string propName = "";
		int? propHash = null;
		int? weaponHash = null;
		int? attachmentHash = null;
		if (supply != null)
		{
			if (supply.Type == BusinessSupplyType.Item || supply.Type == BusinessSupplyType.Ammo)
			{
				InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(supply.Item);
				if (itemBaseDefinition != null && !string.IsNullOrEmpty(itemBaseDefinition.Model))
				{
					propName = itemBaseDefinition.Model;
					propHash = API.GetHashKey(propName);
					flag = true;
				}
			}
			else if (supply.Type == BusinessSupplyType.Weapon)
			{
				weaponHash = API.GetHashKey(supply.Item);
				flag = true;
			}
			else if (supply.Type == BusinessSupplyType.Attachment)
			{
				attachmentHash = API.GetHashKey(supply.Item);
				weaponHash = API.GetHashKey((string)supply.Extra);
				flag = true;
			}
		}
		if (!flag)
		{
			CleanupPreviewProp();
		}
		Vector3 previewCoords = ((activeBusiness.PropPreviewData.ObjectPos_ != null) ? (activeBusiness.PropPreviewData.ObjectPos + new Vector3(0f, 0f, activeBusiness.PropPreviewData.ZOffset)) : (((Entity)BusinessScript.ClosestBusinessEmployee.State.Ped).Position + ((Entity)BusinessScript.ClosestBusinessEmployee.State.Ped).ForwardVector * activeBusiness.PropPreviewData.ObjectDistance + new Vector3(0f, 0f, activeBusiness.PropPreviewData.ZOffset)));
		if (propHash.HasValue)
		{
			lastPropHash = propHash.Value;
			CleanupPreviewProp();
			API.ClearAreaOfObjects(previewCoords.X, previewCoords.Y, previewCoords.Z, 0.1f, 0);
			try
			{
				using DisposableModel propModel = new DisposableModel(Model.op_Implicit(propHash.Value));
				await propModel.Load();
				CleanupPreviewProp();
				previewPropHandle = API.CreateObject(propHash.Value, previewCoords.X, previewCoords.Y, previewCoords.Z, false, false, false);
				API.SetEntityCoords(previewPropHandle, previewCoords.X, previewCoords.Y, previewCoords.Z, false, false, false, false);
				API.SetEntityRotation(previewPropHandle, 0f, 0f, lastRotZ, 0, false);
			}
			catch (ArgumentException exception)
			{
				Print($"Prop preview: invalid prop model ({propName} | {propHash.Value:X})");
				Print(exception);
			}
		}
		int modelHash;
		if (weaponHash.HasValue && (weaponHash.Value != lastWeaponHash || !attachmentHash.HasValue))
		{
			lastWeaponHash = weaponHash.Value;
			if (previewPropHandle != 0)
			{
				CleanupPreviewProp();
				lastAttachmentHash = 0;
			}
			API.ClearAreaOfObjects(previewCoords.X, previewCoords.Y, previewCoords.Z, 0.1f, 0);
			modelHash = API.GetWeapontypeModel((uint)weaponHash.Value);
			try
			{
				using DisposableModel propModel = new DisposableModel(Model.op_Implicit(modelHash));
				await propModel.Load();
				CleanupPreviewProp();
				previewPropHandle = API.CreateWeaponObject((uint)weaponHash.Value, 0, previewCoords.X, previewCoords.Y, previewCoords.Z, true, 1f, 0);
				API.SetEntityCoords(previewPropHandle, previewCoords.X, previewCoords.Y, previewCoords.Z, false, false, false, false);
				API.SetEntityRotation(previewPropHandle, 0f, 0f, lastRotZ, 0, false);
				WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(supply.Item);
				if (weaponDefinition != null && weaponDefinition.PreviewRotation != default(Vector3))
				{
					API.SetEntityRotation(previewPropHandle, weaponDefinition.PreviewRotation.X, weaponDefinition.PreviewRotation.Y, weaponDefinition.PreviewRotation.Z + lastRotZ, 0, false);
				}
			}
			catch (ArgumentException exception2)
			{
				Print($"Weapon preview: invalid weapon model ({weaponHash.Value} | {modelHash:X})");
				Print(exception2);
			}
		}
		if (!attachmentHash.HasValue)
		{
			return;
		}
		if (lastAttachmentHash != 0)
		{
			API.RemoveWeaponComponentFromWeaponObject(previewPropHandle, lastAttachmentHash);
		}
		lastAttachmentHash = attachmentHash.Value;
		modelHash = API.GetWeaponComponentTypeModel((uint)attachmentHash.Value);
		try
		{
			using DisposableModel propModel = new DisposableModel(Model.op_Implicit(modelHash));
			await propModel.Load();
			if (supply.Item.Contains("VARMOD"))
			{
				CleanupPreviewProp();
				previewPropHandle = API.CreateWeaponObject((uint)lastWeaponHash, 0, previewCoords.X, previewCoords.Y, previewCoords.Z, true, 1f, modelHash);
				API.SetEntityCoords(previewPropHandle, previewCoords.X, previewCoords.Y, previewCoords.Z, false, false, false, false);
				API.SetEntityRotation(previewPropHandle, 0f, 0f, lastRotZ, 0, false);
				WeaponComponentDefinition weaponComponentDefinition = Gtacnr.Data.Items.GetWeaponComponentDefinition(supply.Item);
				if (weaponComponentDefinition != null && weaponComponentDefinition.PreviewRotation != default(Vector3))
				{
					API.SetEntityRotation(previewPropHandle, weaponComponentDefinition.PreviewRotation.X, weaponComponentDefinition.PreviewRotation.Y, weaponComponentDefinition.PreviewRotation.Z + lastRotZ, 0, false);
				}
			}
			else
			{
				API.GiveWeaponComponentToWeaponObject(previewPropHandle, (uint)attachmentHash.Value);
			}
		}
		catch (ArgumentException exception3)
		{
			Print($"Invalid weapon attachment model ({modelHash:X})");
			Print(exception3);
		}
	}

	private void StartClothesPreview(Business business)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		if (!isInClothesView)
		{
			startHealth = ((Entity)Game.PlayerPed).Health;
			prevPedPos = new Vector4(((Entity)Game.PlayerPed).Position, ((Entity)Game.PlayerPed).Heading);
			Menu menu = mainMenu;
			Vector3 menuCoords = (((Entity)Game.PlayerPed).PositionNoOffset = business.ClothingPreviewData.PedPos.XYZ());
			menu.MenuCoords = menuCoords;
			((Entity)Game.PlayerPed).Heading = business.ClothingPreviewData.PedPos.W;
			((Entity)Game.PlayerPed).IsPositionFrozen = true;
			Game.PlayerPed.Task.ClearAllImmediately();
			Vector3 val2 = ((business.ClothingPreviewData.CameraPos_ != null) ? business.ClothingPreviewData.CameraPos : (((Entity)Game.PlayerPed).Position + ((Entity)Game.PlayerPed).ForwardVector * 2.22f + new Vector3(0f, 0f, 0.5f)));
			Vector3 val3 = business.ClothingPreviewData.PedPos.XYZ();
			API.DestroyAllCams(true);
			cameraHandle = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", val2.X, val2.Y, val2.Z, 0f, 0f, 0f, 45f, false, 0);
			API.SetCamActive(cameraHandle, true);
			API.PointCamAtCoord(cameraHandle, val3.X, val3.Y, val3.Z);
			API.RenderScriptCams(true, false, 2000, true, true);
			if (business.Type == BusinessType.TattooShop)
			{
				Utils.StoreCurrentOutfit("tattooShop");
				WardrobeMenuScript.ClearOutfitExceptTattoosAndHair();
				Utils.StoreCurrentOutfit();
			}
			isInClothesView = true;
			if (!isCamTaskAttached)
			{
				isCamTaskAttached = true;
				base.Update += BusinessCameraTask;
			}
		}
	}

	private void StopClothesPreview()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (!isInClothesView)
		{
			return;
		}
		if (cameraHandle != 0)
		{
			API.SetCamActive(cameraHandle, false);
			API.DestroyCam(cameraHandle, false);
			API.RenderScriptCams(false, false, 0, true, false);
			cameraHandle = 0;
		}
		if (isCamTaskAttached)
		{
			isCamTaskAttached = false;
			base.Update -= BusinessCameraTask;
		}
		Menu menu = mainMenu;
		Vector3 menuCoords = (((Entity)Game.PlayerPed).PositionNoOffset = prevPedPos.XYZ());
		menu.MenuCoords = menuCoords;
		((Entity)Game.PlayerPed).Heading = prevPedPos.W;
		((Entity)Game.PlayerPed).IsPositionFrozen = false;
		isInClothesView = false;
		foreach (Player player in ((BaseScript)this).Players)
		{
			((Entity)player.Character).Opacity = 255;
		}
		Business activeBusiness = ActiveBusiness;
		if (activeBusiness != null && activeBusiness.Type == BusinessType.TattooShop)
		{
			Utils.RestoreOutfit("tattooShop");
		}
	}

	private void OnMenuOpened(Menu menu)
	{
		List<MenuItem> menuItems = menu.GetMenuItems();
		if (menuItems.Count > 0)
		{
			RefreshMenuActions(menu, menuItems[0]);
			RefreshAllSupplyMenuItems(menu);
		}
		if (menu == mainMenu)
		{
			if (ActiveBusiness.PropPreviewData != null)
			{
				StopClothesPreview();
				StartPropPreview(ActiveBusiness);
			}
			else if (ActiveBusiness.ClothingPreviewData != null)
			{
				StopPropPreview();
				StartClothesPreview(ActiveBusiness);
			}
			if (externalMenuOpenHandlers.TryGetValue(ActiveBusiness.Type, out List<Action<Menu>> value))
			{
				foreach (Action<Menu> item in value)
				{
					item(menu);
				}
			}
		}
		else
		{
			MenuItem currentMenuItem = menu.GetCurrentMenuItem();
			if (currentMenuItem != null && currentMenuItem.ItemData is BusinessSupply businessSupply)
			{
				ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(businessSupply.Item);
				if (clothingItemDefinition != null)
				{
					Utils.RestoreOutfit();
					Utils.StoreCurrentOutfit();
					Utils.PreviewClothingItem(clothingItemDefinition);
				}
			}
		}
		if (ActiveBusiness.PropPreviewData != null)
		{
			MenuItem currentMenuItem2 = menu.GetCurrentMenuItem();
			if (currentMenuItem2 != null)
			{
				if (currentMenuItem2.ItemData is BusinessSupply supply)
				{
					PreviewProp(supply);
				}
				else if (currentMenuItem2.ItemData is Tuple<BusinessSupply> tuple)
				{
					PreviewProp(tuple.Item1);
				}
				else if (currentMenuItem2.ItemData is Tuple<int, BusinessSupply> tuple2)
				{
					PreviewProp(tuple2.Item2);
				}
			}
		}
		if (ActiveBusiness.ClothingPreviewData != null)
		{
			MenuItem currentMenuItem3 = menu.GetCurrentMenuItem();
			if (currentMenuItem3 != null)
			{
				BusinessSupply businessSupply2 = null;
				if (currentMenuItem3.ItemData is BusinessSupply businessSupply3)
				{
					businessSupply2 = businessSupply3;
				}
				else if (currentMenuItem3.ItemData is Tuple<BusinessSupply> tuple3)
				{
					businessSupply2 = tuple3.Item1;
				}
				else if (currentMenuItem3.ItemData is Tuple<int, BusinessSupply> tuple4)
				{
					businessSupply2 = tuple4.Item2;
				}
				if (businessSupply2 != null && Gtacnr.Data.Items.IsClothingItemDefined(businessSupply2.Item))
				{
					StopPropPreview();
					StartClothesPreview(ActiveBusiness);
				}
			}
		}
		MoneyDisplayScript.ForceMoneyDisplay = true;
	}

	private void OnMenuClosed(Menu menu, MenuClosedEventArgs e)
	{
		if (menu == mainMenu && !MenuController.IsAnyMenuOpen())
		{
			BusinessEmployee closestBusinessEmployee = BusinessScript.ClosestBusinessEmployee;
			if (closestBusinessEmployee != null && closestBusinessEmployee.HasMenu && (Entity)(object)closestBusinessEmployee.State?.Ped != (Entity)null)
			{
				closestBusinessEmployee.State.Ped.PlayAmbientSpeech(closestBusinessEmployee.HasShopDialog ? "SHOP_SELL" : "", (SpeechModifier)3);
			}
			MoneyDisplayScript.ForceMoneyDisplay = false;
		}
		if (menu.GetMenuItems().Count > 0 && menu.GetMenuItems().First().ItemData is BusinessSupply)
		{
			Utils.RestoreOutfit();
		}
	}

	private void OnMenuIndexChanged(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		RefreshMenuActions(menu, newItem);
		RefreshAllSupplyMenuItems(menu);
		Utils.RestoreOutfit();
		if (newItem != null && newItem.ItemData is BusinessSupply businessSupply)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(businessSupply.Item);
			if (clothingItemDefinition != null)
			{
				Utils.StoreCurrentOutfit();
				Utils.PreviewClothingItem(clothingItemDefinition);
			}
		}
		if (ActiveBusiness.PropPreviewData != null && newItem != null)
		{
			if (newItem.ItemData is BusinessSupply supply)
			{
				PreviewProp(supply);
			}
			else if (newItem.ItemData is Tuple<BusinessSupply> tuple)
			{
				PreviewProp(tuple.Item1);
			}
			else if (newItem.ItemData is Tuple<int, BusinessSupply> tuple2)
			{
				PreviewProp(tuple2.Item2);
			}
		}
		if (menu != mainMenu)
		{
			menu.CounterPreText = $"{newIndex + 1}/{menu.GetMenuItems().Count}";
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		try
		{
			if (isBusy)
			{
				return;
			}
			isBusy = true;
			if (menuItem.ItemData is InventoryEntry inventoryEntry)
			{
				if (Gtacnr.Data.Items.GetItemDefinition(inventoryEntry.ItemId).ExtraData?.ContainsKey("GiftCardType") ?? false)
				{
					await RedeemGiftCard(inventoryEntry, menuItem);
				}
			}
			else if (menuItem.ItemData is BusinessSupply)
			{
				OnStorePurchase(menu, menuItem);
			}
			else if (menuItem.ItemData is Tuple<int, BusinessSupply> tuple)
			{
				_ = tuple.Item2;
				OnStoreSell(menu, menuItem);
			}
			else
			{
				Utils.PlaySelectSound();
			}
			foreach (Action<Menu, MenuItem, int> externalItemSelectHandler in externalItemSelectHandlers)
			{
				externalItemSelectHandler(menu, menuItem, itemIndex);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isBusy = false;
		}
	}

	private void OnListItemSelect(Menu menu, MenuListItem listItem, int selectedIndex, int itemIndex)
	{
		try
		{
			if (isBusy)
			{
				return;
			}
			isBusy = true;
			if (listItem.ItemData is BusinessSupply)
			{
				OnStorePurchase(menu, listItem);
			}
			else if (listItem.ItemData is Tuple<int, BusinessSupply> tuple)
			{
				_ = tuple.Item2;
				OnStoreSell(menu, listItem);
			}
			else
			{
				Utils.PlaySelectSound();
			}
			foreach (Action<Menu, MenuItem, int, int> externalListItemSelectHandler in externalListItemSelectHandlers)
			{
				externalListItemSelectHandler(menu, listItem, selectedIndex, itemIndex);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isBusy = false;
		}
	}

	private async Task RedeemGiftCard(InventoryEntry entry, MenuItem menuItem)
	{
		if (entry.Amount < 1f)
		{
			Utils.PlayErrorSound();
		}
		else if (await TriggerServerEventAsync<int>("gtacnr:businesses:redeemGiftCard", new object[2] { ActiveBusiness.Id, entry.ItemId }) == 1)
		{
			Utils.PlayContinueSound();
			if (entry.Amount == 0f)
			{
				menuItem.ParentMenu.RemoveMenuItem(menuItem);
			}
			else
			{
				menuItem.Label = $"{entry.Amount:0}";
			}
			await UpdateGiftCardBalance();
		}
		else
		{
			Utils.PlayErrorSound();
		}
	}

	private async Task UpdateGiftCardBalance()
	{
		mainMenu.CounterPreText = "";
		string text = null;
		if (ActiveBusiness.Type == BusinessType.GunStore || ActiveBusiness.Type == BusinessType.GunStoreWithShootingRange)
		{
			text = AccountType.AmmunationGiftCard;
		}
		if (text != null)
		{
			long num = await Money.GetCachedBalanceOrFetch(text);
			if (num != 0L)
			{
				mainMenu.CounterPreText = LocalizationController.S(Entries.Businesses.MENU_STORE_GIFT_CARD_BALANCE, num.ToCurrencyString());
			}
		}
	}

	private bool ShouldShowSupply(BusinessSupply supply)
	{
		if (supply == null || !supply.IsVisible)
		{
			return false;
		}
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(supply.Item);
		if (itemBaseDefinition == null)
		{
			if (supply.Type != BusinessSupplyType.Service)
			{
				Print("^3Warning: Item `" + supply.Item + "` is undefined. Skipping.");
			}
			return false;
		}
		Sex freemodePedSex = Utils.GetFreemodePedSex(Game.PlayerPed);
		if (supply.Job != null && supply.Job != jobData.Id)
		{
			return false;
		}
		if (supply.Department != null)
		{
			if (BusinessScript.ClosestBusiness.PoliceStation != null && BusinessScript.ClosestBusiness.PoliceStation.Department != supply.Department)
			{
				return false;
			}
			if (BusinessScript.ClosestBusiness.Hospital != null && BusinessScript.ClosestBusiness.Hospital.Department != supply.Department)
			{
				return false;
			}
		}
		if (itemBaseDefinition.CreationDate > default(DateTime) && DateTime.UtcNow < itemBaseDefinition.CreationDate)
		{
			return false;
		}
		if (itemBaseDefinition.DisabledDate > default(DateTime) && DateTime.UtcNow > itemBaseDefinition.DisabledDate)
		{
			return false;
		}
		if (supply.Type == BusinessSupplyType.Clothing)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(supply.Item);
			if (clothingItemDefinition == null)
			{
				return false;
			}
			if (!clothingItemDefinition.HasSex(freemodePedSex))
			{
				return false;
			}
			if (jobData.SeparateOutfit && !clothingItemDefinition.AvailableAcrossAllJobs && supply.Job != jobData.Id)
			{
				return false;
			}
		}
		return true;
	}

	private List<BusinessSupply> GetBusinessSupplies(string menuId)
	{
		if (!businessSupplies.ContainsKey(menuId))
		{
			return new List<BusinessSupply>();
		}
		return businessSupplies[menuId].Where((BusinessSupply s) => ShouldShowSupply(s)).ToList();
	}

	private List<BusinessSupply> GetBusinessSupplies(BusinessType businessType)
	{
		return GetBusinessSupplies(businessType.ToString());
	}

	private async Task<Dictionary<string, float>> GetSupplyStock(Business business)
	{
		if (!business.LimitedStock)
		{
			return new Dictionary<string, float>();
		}
		return (await TriggerServerEventAsync<string>("gtacnr:businesses:getSupplyStock", new object[1] { business.Id })).Unjson<Dictionary<string, float>>();
	}

	private List<BusinessSupply> GetBusinessDemands(string menuId)
	{
		if (!businessDemands.ContainsKey(menuId))
		{
			return new List<BusinessSupply>();
		}
		return businessDemands[menuId].ToList();
	}

	private List<BusinessSupply> GetBusinessDemands(BusinessType businessType)
	{
		return GetBusinessDemands(businessType.ToString());
	}

	public static void AddExternalMenuItem(BusinessType businessType, MenuItem menuItem, bool before = false)
	{
		Dictionary<BusinessType, List<MenuItem>> dictionary = (before ? externalMenuItemsBefore : externalMenuItemsAfter);
		if (!dictionary.ContainsKey(businessType))
		{
			dictionary[businessType] = new List<MenuItem>();
		}
		dictionary[businessType].Add(menuItem);
	}

	public static bool RemoveExternalMenuItem(BusinessType businessType, MenuItem menuItem)
	{
		if (externalMenuItemsBefore.ContainsKey(businessType))
		{
			return externalMenuItemsBefore[businessType].Remove(menuItem);
		}
		if (externalMenuItemsAfter.ContainsKey(businessType))
		{
			return externalMenuItemsAfter[businessType].Remove(menuItem);
		}
		return false;
	}

	public static void ClearExternalMenuItems(BusinessType businessType)
	{
		if (externalMenuItemsBefore.ContainsKey(businessType))
		{
			externalMenuItemsBefore[businessType].Clear();
		}
		if (externalMenuItemsAfter.ContainsKey(businessType))
		{
			externalMenuItemsAfter[businessType].Clear();
		}
	}

	public static void BindExternalMenuItem(Menu menu, MenuItem menuItem)
	{
		MenuController.BindMenuItem(mainMenu, menu, menuItem);
	}

	public static void AddExternalMenuOpenHandler(BusinessType businessType, Action<Menu> openHandler)
	{
		if (!externalMenuOpenHandlers.ContainsKey(businessType))
		{
			externalMenuOpenHandlers[businessType] = new List<Action<Menu>>();
		}
		externalMenuOpenHandlers[businessType].Add(openHandler);
	}

	public static bool RemoveExternalMenuOpenHandler(BusinessType businessType, Action<Menu> openHandler)
	{
		return externalMenuOpenHandlers[businessType].Remove(openHandler);
	}

	public static void AddExternalItemSelectHandler(Action<Menu, MenuItem, int> selectHandler)
	{
		externalItemSelectHandlers.Add(selectHandler);
	}

	public static void RemoveExternalItemSelectHandler(Action<Menu, MenuItem, int> selectHandler)
	{
		externalItemSelectHandlers.Remove(selectHandler);
	}

	public static void AddExternalListItemSelectHandler(Action<Menu, MenuItem, int, int> selectHandler)
	{
		externalListItemSelectHandlers.Add(selectHandler);
	}

	public static void RemoveExternalListItemSelectHandler(Action<Menu, MenuItem, int, int> selectHandler)
	{
		externalListItemSelectHandlers.Remove(selectHandler);
	}

	public static async void OpenShopMenu(Business business, Menu parentMenu = null)
	{
		script.OpenBusinessMenuInternal(business, parentMenu);
	}

	private async void OpenBusinessMenuInternal(Business business, Menu parentMenu = null)
	{
		if (isBusy)
		{
			return;
		}
		isBusy = true;
		try
		{
			IEnumerable<BusinessEmployee> enumerable = business.Employees.Where((BusinessEmployee e) => e.HasMenu);
			if (enumerable == null || enumerable.Count() == 0 || !enumerable.Any(delegate(BusinessEmployee e)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0013: Unknown result type (might be due to invalid IL or missing references)
				Vector3 val = e.Location.XYZ();
				return ((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 25f;
			}) || BusinessScript.ClosestBusiness != business)
			{
				Utils.PlayErrorSound();
				return;
			}
			Ped ped = BusinessScript.ClosestBusinessEmployee.State.Ped;
			if (((Entity)ped).IsDead)
			{
				Utils.PlayErrorSound();
				return;
			}
			ActiveBusiness = business;
			if (business.IsBeingRobbed || business.EmployeesAssaulted)
			{
				Utils.PlayErrorSound();
				if ((Entity)(object)ped != (Entity)null)
				{
					ped.PlayAmbientSpeech("GENERIC_INSULT_HIGH", (SpeechModifier)3);
				}
				return;
			}
			TimeSpan timeSpan = TimeSpan.FromMinutes(5.0);
			if (!Gtacnr.Utils.CheckTimePassed(business.ShopliftDateTime, timeSpan))
			{
				TimeSpan cooldownTimeLeft = Gtacnr.Utils.GetCooldownTimeLeft(business.ShopliftDateTime, timeSpan);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_SHOPLIFTING_COOLDOWN, Gtacnr.Utils.FormatTimeSpanString(cooldownTimeLeft)), playSound: false);
				Utils.PlayErrorSound();
				if ((Entity)(object)ped != (Entity)null)
				{
					ped.PlayAmbientSpeech("GENERIC_INSULT_HIGH", (SpeechModifier)3);
				}
				return;
			}
			if (Gtacnr.Client.API.Crime.CachedWantedLevel == 5 && (business.Type == BusinessType.GunStore || business.Type == BusinessType.GunStoreWithShootingRange))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_BOUNTY), playSound: false);
				Utils.PlayErrorSound();
				if ((Entity)(object)ped != (Entity)null)
				{
					string text = ((((Entity)ped).Model == Model.op_Implicit((PedHash)(-1643617475))) ? "s_m_y_ammucity_01_white_01" : "s_m_m_ammucountry_01_white_01");
					API.PlayAmbientSpeechWithVoice(((PoolObject)ped).Handle, "GUNSH_COPS", text, "SPEECH_PARAMS_FORCE", false);
				}
				return;
			}
			if (business.IsIllegal && jobsEnum.IsPublicService())
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_DUTY), playSound: false);
				Utils.PlayErrorSound();
				return;
			}
			if (business.IsPoliceOnly && !jobsEnum.IsPolice())
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_COPS_ONLY), playSound: false);
				Utils.PlayErrorSound();
				return;
			}
			levelCache = Gtacnr.Utils.GetLevelByXP(Users.CachedXP);
			if (business.RequiredLevel > 0 && levelCache < business.RequiredLevel)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_LEVEL, business.RequiredLevel), playSound: false);
				Utils.PlayErrorSound();
				return;
			}
			if ((Entity)(object)ped != (Entity)null)
			{
				bool flag = Utils.GetFreemodePedSex(ped) == Sex.Male;
				switch (business.Type)
				{
				case BusinessType.PoliceStation:
				{
					string text2 = (flag ? "s_m_y_cop_01_white_full_01" : "s_f_y_cop_01_white_full_01");
					API.PlayAmbientSpeechWithVoice(((PoolObject)ped).Handle, "CHAT_STATE", text2, "SPEECH_PARAMS_FORCE", false);
					break;
				}
				case BusinessType.Mechanic:
				{
					string text2 = (flag ? "a_m_y_eastsa_02_latino_full_01" : "a_f_y_eastsa_03_latino_full_01");
					API.PlayAmbientSpeechWithVoice(((PoolObject)ped).Handle, "CHAT_STATE", text2, "SPEECH_PARAMS_FORCE", false);
					break;
				}
				default:
					ped.PlayAmbientSpeech(BusinessScript.ClosestBusinessEmployee.HasShopDialog ? "SHOP_BROWSE" : "CHAT_STATE", (SpeechModifier)3);
					break;
				}
			}
			mainMenu.ParentMenu = parentMenu;
			mainMenu.ClearMenuItems();
			OpenMenu();
			mainMenu.AddLoadingMenuItem();
			string menuTitle = LocalizationController.S(Entries.Businesses.MENU_STORE_TITLE);
			string menuSubtitle = LocalizationController.S(Entries.Businesses.MENU_STORE_SUBTITLE);
			if (business != null)
			{
				string key = business.Type.ToString();
				menuTitle = (BusinessScript.BusinessTypes.ContainsKey(key) ? BusinessScript.BusinessTypes[key].Name : LocalizationController.S(Entries.Businesses.MENU_STORE_TITLE));
				menuSubtitle = business.Name;
				if (business.MenuHeaderDictionary != null && business.MenuHeaderTexture != null)
				{
					mainMenu.HeaderTexture = new KeyValuePair<string, string>(business.MenuHeaderDictionary, business.MenuHeaderTexture);
				}
				else
				{
					mainMenu.HeaderTexture = default(KeyValuePair<string, string>);
				}
			}
			mainMenu.MenuTitle = menuTitle;
			mainMenu.MenuSubtitle = menuSubtitle;
			string menuName = ActiveBusiness.MenuOverride ?? ActiveBusiness.Type.ToString();
			List<BusinessSupply> supplies = GetBusinessSupplies(menuName) ?? new List<BusinessSupply>();
			string wardrobeJob = ((!(jobData?.SeparateOutfit ?? false)) ? "none" : jobData?.Id);
			moneyCache = await Money.GetCachedBalanceOrFetch(AccountType.Cash);
			wardrobeCache = await Clothes.GetAllOwned(wardrobeJob);
			supplyStockCache = await GetSupplyStock(business);
			bool isGunStore = business.Type == BusinessType.GunStore || business.Type == BusinessType.GunStoreWithShootingRange;
			Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();
			if (InventoryMenuScript.ShouldRefreshCache)
			{
				await InventoryMenuScript.ReloadInventory();
			}
			if (StockMenuScript.ShouldRefreshCache() && (jobData?.CanStockItems ?? false))
			{
				await StockMenuScript.ReloadStock();
			}
			await UpdateGiftCardBalance();
			mainMenu.ClearMenuItems();
			ShoppingScript.MenuOpening?.Invoke(this, new EventArgs());
			if ((!externalMenuItemsBefore.ContainsKey(business.Type) || externalMenuItemsBefore[business.Type].Count <= 0) && (!externalMenuItemsAfter.ContainsKey(business.Type) || externalMenuItemsAfter[business.Type].Count <= 0) && supplies.Count == 0)
			{
				mainMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STORE_NO_ITEMS_TEXT), LocalizationController.S(Entries.Businesses.MENU_STORE_NO_ITEMS_DESC)));
				return;
			}
			IEnumerable<InventoryEntry> enumerable2 = InventoryMenuScript.Cache?.Where(delegate(InventoryEntry e)
			{
				if (e == null || e.Amount < 1f)
				{
					return false;
				}
				InventoryItem itemDefinition2 = Gtacnr.Data.Items.GetItemDefinition(e.ItemId);
				if (itemDefinition2 == null || itemDefinition2.ExtraData == null)
				{
					return false;
				}
				return itemDefinition2.ExtraData.ContainsKey("GiftCardType") && (string)itemDefinition2.ExtraData["GiftCardType"] == "ammunation";
			});
			if (isGunStore && enumerable2 != null && enumerable2.Count() > 0)
			{
				string key2 = "gift_cards";
				subMenus[key2] = new Menu(menuTitle, LocalizationController.S(Entries.Businesses.MENU_STORE_REDEEM_GIFT_CARDS))
				{
					PlaySelectSound = false,
					MaxDistance = 10f
				};
				subMenus[key2].OnItemSelect += OnItemSelect;
				MenuItem menuItem = new MenuItem("~b~" + LocalizationController.S(Entries.Businesses.MENU_STORE_REDEEM_GIFT_CARDS))
				{
					Label = Utils.MENU_ARROW
				};
				mainMenu.AddMenuItem(menuItem);
				MenuController.AddSubmenu(mainMenu, subMenus[key2]);
				MenuController.BindMenuItem(mainMenu, subMenus[key2], menuItem);
				foreach (InventoryEntry item3 in enumerable2)
				{
					MenuItem item = new MenuItem(Gtacnr.Data.Items.GetItemDefinition(item3.ItemId).Name ?? "")
					{
						Label = $"{item3.Amount:0.##}",
						ItemData = item3
					};
					subMenus[key2].AddMenuItem(item);
				}
			}
			List<BusinessSupply> list = GetBusinessDemands(menuName) ?? new List<BusinessSupply>();
			if (list.Count > 0)
			{
				Menu menu = new Menu(mainMenu.MenuTitle, LocalizationController.S(Entries.Businesses.MENU_STORE_SELL_ITEMS))
				{
					PlaySelectSound = false
				};
				MenuItem menuItem2 = new MenuItem("~b~" + LocalizationController.S(Entries.Businesses.MENU_STORE_SELL_ITEMS), LocalizationController.S(Entries.Businesses.MENU_STORE_SELL_ITEMS_DESC))
				{
					Label = Utils.MENU_ARROW
				};
				menu.OnItemSelect += OnItemSelect;
				menu.OnListItemSelect += OnListItemSelect;
				mainMenu.AddMenuItem(menuItem2);
				MenuController.BindMenuItem(mainMenu, menu, menuItem2);
				foreach (BusinessSupply demand in list)
				{
					InventoryEntry inventoryEntry = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry e) => e.ItemId == demand.Item);
					if (inventoryEntry != null && inventoryEntry.Amount > 0f)
					{
						int num = inventoryEntry.Amount.ToInt();
						InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(inventoryEntry.ItemId);
						int num2 = demand.CalculateFinalDemandPayout(ActiveBusiness);
						List<string> list2 = new List<string>();
						for (int num3 = 1; num3 <= num; num3++)
						{
							list2.Add(LocalizationController.S(Entries.Businesses.MENU_STORE_SELL_FOR, num3, (num3 * num2).ToCurrencyString()));
						}
						string text3 = itemDefinition.Name;
						string text4 = itemDefinition.Description;
						if (text3.Length > 24)
						{
							text3 = text3.Substring(0, 21) + "...";
							text4 = itemDefinition.Name + "\n" + text4;
						}
						MenuListItem item2 = new MenuListItem(text3, list2, list2.Count - 1, text4)
						{
							ItemData = Tuple.Create(-1, demand)
						};
						menu.AddMenuItem(item2);
					}
				}
				if (menu.GetMenuItems().Count == 0)
				{
					menu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STORE_NO_ITEMS_TEXT), LocalizationController.S(Entries.Businesses.MENU_STORE_NO_ITEMS_SELL_DESC))
					{
						Enabled = false
					});
				}
			}
			if (supplies.Any((BusinessSupply s) => s.IsJobSupply && s.Job == jobData.Id))
			{
				subMenus["__job__"] = new Menu(menuTitle, LocalizationController.S(Entries.Businesses.MENU_STORE_JOB_SUPPLIES, jobData.Name))
				{
					PlaySelectSound = false,
					MaxDistance = 10f
				};
				subMenus["__job__"].OnIndexChange += OnMenuIndexChanged;
				subMenus["__job__"].OnMenuOpen += OnMenuOpened;
				subMenus["__job__"].OnMenuClose += OnMenuClosed;
				MenuItem menuItem3 = new MenuItem("~b~" + LocalizationController.S(Entries.Businesses.MENU_STORE_JOB_SUPPLIES, jobData.Name))
				{
					Label = Utils.MENU_ARROW
				};
				mainMenu.AddMenuItem(menuItem3);
				MenuController.AddSubmenu(mainMenu, subMenus["__job__"]);
				MenuController.BindMenuItem(mainMenu, subMenus["__job__"], menuItem3);
			}
			if (externalMenuItemsBefore.ContainsKey(business.Type))
			{
				foreach (MenuItem item4 in externalMenuItemsBefore[business.Type])
				{
					mainMenu.AddMenuItem(item4);
				}
			}
			foreach (BusinessSupply item5 in supplies.Where((BusinessSupply s) => s.IsVisible && (s.Job == null || s.Job == jobData.Id)))
			{
				Menu menu2 = null;
				string text5 = "";
				if (item5.IsJobSupply && item5.Job == jobData.Id)
				{
					menu2 = subMenus["__job__"];
					text5 = "__job__";
				}
				if (!string.IsNullOrWhiteSpace(item5.Path))
				{
					string key3 = text5 + item5.Path;
					if (!subMenus.ContainsKey(key3))
					{
						string[] array = item5.Path.Split('/');
						if (array.Length > 1)
						{
							string text6 = "";
							MenuItem itemData = null;
							string[] array2 = array;
							foreach (string text7 in array2)
							{
								text6 = (text6 + "/" + text7).Trim('/');
								string key4 = text5 + text6;
								if (!subMenus.ContainsKey(key4))
								{
									string text8 = Gtacnr.Utils.ResolveLocalization(text7);
									subMenus[key4] = new Menu(menuTitle, text8)
									{
										PlaySelectSound = false,
										MaxDistance = 10f
									};
									subMenus[key4].OnIndexChange += OnMenuIndexChanged;
									subMenus[key4].OnMenuOpen += OnMenuOpened;
									subMenus[key4].OnMenuClose += OnMenuClosed;
									if (menu2 == null)
									{
										menu2 = mainMenu;
									}
									MenuItem menuItem4 = new MenuItem(text8)
									{
										Label = Utils.MENU_ARROW
									};
									menu2.AddMenuItem(menuItem4);
									MenuController.AddSubmenu(menu2, subMenus[key4]);
									MenuController.BindMenuItem(menu2, subMenus[key4], menuItem4);
									if (text7 == array.Last())
									{
										menuItem4.ItemData = Tuple.Create(item5);
									}
									else
									{
										menuItem4.ItemData = itemData;
									}
									itemData = menuItem4;
									AddSpecialAttributes(menuItem4, item5, isFolder: true);
								}
								menu2 = subMenus[key4];
							}
						}
						else
						{
							string text9 = Gtacnr.Utils.ResolveLocalization(array[0]);
							subMenus[key3] = new Menu(menuTitle, text9)
							{
								PlaySelectSound = false,
								MaxDistance = 10f
							};
							subMenus[key3].OnIndexChange += OnMenuIndexChanged;
							subMenus[key3].OnMenuOpen += OnMenuOpened;
							subMenus[key3].OnMenuClose += OnMenuClosed;
							if (menu2 == null)
							{
								menu2 = mainMenu;
							}
							MenuItem menuItem5 = new MenuItem(text9)
							{
								Label = Utils.MENU_ARROW
							};
							menu2.AddMenuItem(menuItem5);
							MenuController.AddSubmenu(menu2, subMenus[key3]);
							MenuController.BindMenuItem(menu2, subMenus[key3], menuItem5);
							AddSpecialAttributes(menuItem5, item5, isFolder: true);
							menu2 = subMenus[key3];
						}
					}
					else
					{
						menu2 = subMenus[key3];
						MenuItem mItem;
						for (mItem = MenuController.MenuButtons.FirstOrDefault<KeyValuePair<MenuItem, Menu>>((KeyValuePair<MenuItem, Menu> mb) => mb.Value == menu2).Key; mItem != null; mItem = MenuController.MenuButtons.FirstOrDefault<KeyValuePair<MenuItem, Menu>>((KeyValuePair<MenuItem, Menu> mb) => mb.Value == mItem.ParentMenu).Key)
						{
							AddSpecialAttributes(mItem, item5, isFolder: true);
						}
					}
				}
				AddItemToMenu(menu2, item5);
			}
			if (externalMenuItemsAfter.ContainsKey(business.Type))
			{
				foreach (MenuItem item6 in externalMenuItemsAfter[business.Type])
				{
					mainMenu.AddMenuItem(item6);
				}
			}
			mainMenu.RefreshIndex();
			if (mainMenu.GetMenuItems().Count > 0)
			{
				MenuItem menuItem6 = mainMenu.GetMenuItems().First();
				RefreshMenuActions(mainMenu, menuItem6);
			}
		}
		catch (Exception ex)
		{
			Print(ex);
			mainMenu.ClearMenuItems();
			mainMenu.AddErrorMenuItem(ex);
		}
		finally
		{
			List<MenuItem> menuItems = mainMenu.GetMenuItems();
			if (menuItems.Count > 0)
			{
				RefreshMenuActions(mainMenu, menuItems[0]);
				RefreshAllSupplyMenuItems(mainMenu);
			}
			isBusy = false;
		}
		void AddItemToMenu(Menu menu3, BusinessSupply supply)
		{
			if (menu3 == null)
			{
				menu3 = mainMenu;
			}
			InventoryItemBase itemInfo = Gtacnr.Data.Items.GetItemBaseDefinition(supply.Item);
			if (itemInfo != null)
			{
				MenuItem menuItem7 = null;
				float multiplier = ActiveBusiness.GetMultiplier(itemInfo.Category.ToString());
				if (supply.PurchaseAmounts.Count > 1 || supply.PurchaseSupplies.Count > 1)
				{
					List<string> list3 = new List<string>();
					int num5 = supply.CalculateFinalPrice(ActiveBusiness.PriceMultiplier * multiplier);
					float num6 = 0f;
					if (supply.AutoSelectAmount && itemInfo.Limit > 0f)
					{
						num6 = itemInfo.Limit;
						if (Gtacnr.Data.Items.IsItemDefined(itemInfo.Id))
						{
							InventoryEntry inventoryEntry2 = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemInfo.Id);
							if (inventoryEntry2 != null)
							{
								num6 = itemInfo.Limit - inventoryEntry2.Amount;
							}
						}
						else if (Gtacnr.Data.Items.IsAmmoDefined(itemInfo.Id))
						{
							AmmoDefinition ammoDefinition = Gtacnr.Data.Items.GetAmmoDefinition(itemInfo.Id);
							int pedAmmoByType = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, ammoDefinition.Hash);
							num6 = itemInfo.Limit - (float)pedAmmoByType;
						}
					}
					int num7 = 0;
					int index = 0;
					if (supply.PurchaseAmounts.Count > 1)
					{
						foreach (float purchaseAmount in supply.PurchaseAmounts)
						{
							int price = Convert.ToInt32(Math.Ceiling((float)num5 * purchaseAmount));
							list3.Add(LocalizationController.S(Entries.Businesses.MENU_STORE_AMOUNT_FOR_PRICE, purchaseAmount.ToString("0.#"), price.ToPriceTagString(moneyCache)) + "~s~");
							if (purchaseAmount == num6)
							{
								index = num7;
							}
							num7++;
						}
					}
					else if (supply.PurchaseSupplies.Count > 1)
					{
						foreach (PurchaseSupply purchaseSupply in supply.PurchaseSupplies)
						{
							int price2 = purchaseSupply.CalculateFinalPrice(ActiveBusiness.PriceMultiplier * multiplier);
							list3.Add(LocalizationController.S(Entries.Businesses.MENU_STORE_AMOUNT_FOR_PRICE, purchaseSupply.Amount.ToString("0.#"), price2.ToPriceTagString(moneyCache)) + "~s~");
							if (purchaseSupply.Amount == num6)
							{
								index = num7;
							}
							num7++;
						}
					}
					menuItem7 = new MenuListItem(itemInfo.Name, list3, index)
					{
						ItemData = supply,
						Description = itemInfo.Description
					};
					menu3.AddMenuItem(menuItem7);
				}
				else
				{
					int price3 = supply.CalculateFinalPrice(ActiveBusiness.PriceMultiplier * multiplier);
					menuItem7 = new MenuItem(itemInfo.Name, itemInfo.Description)
					{
						ItemData = supply,
						Label = (price3.ToPriceTagString(moneyCache) ?? "")
					};
					menu3.AddMenuItem(menuItem7);
				}
				if (menuItem7 != null)
				{
					RefreshSupplyMenuItem(menuItem7);
				}
			}
		}
	}

	private async void OnStorePurchase(Menu menu, MenuItem menuItem)
	{
		if (purchaseInProgress || menuItem.ItemData == null)
		{
			return;
		}
		purchaseInProgress = true;
		try
		{
			object itemData = menuItem.ItemData;
			if (!(itemData is BusinessSupply supply))
			{
				return;
			}
			Business business = ActiveBusiness;
			string key = business.MenuOverride ?? business.Type.ToString();
			int supplyIndex = businessSupplies[key].IndexOf(supply);
			if (supplyIndex < 0)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.INVALID_ITEM));
				return;
			}
			float purchaseAmount = 1f;
			if ((float)supply.PurchaseAmounts.Count > 1f && menuItem is MenuListItem { ListIndex: var listIndex })
			{
				purchaseAmount = supply.PurchaseAmounts[listIndex];
			}
			else if ((float)supply.PurchaseSupplies.Count > 1f && menuItem is MenuListItem { ListIndex: var listIndex2 })
			{
				purchaseAmount = supply.PurchaseSupplies[listIndex2].Amount;
			}
			string name;
			if (supply.Type == BusinessSupplyType.Item && Gtacnr.Data.Items.IsItemBaseDefined(supply.Item))
			{
				InventoryItemBase itemInfo = Gtacnr.Data.Items.GetItemBaseDefinition(supply.Item);
				name = itemInfo.Name;
				InventoryEntry inventoryEntry = (supply.IsJobSupply ? StockMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemInfo.Id) : InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemInfo.Id));
				float num = ((!supply.IsJobSupply) ? itemInfo.Limit : ((itemInfo.JobLimits?.ContainsKey(jobData.Id) ?? false) ? itemInfo.JobLimits[jobData.Id] : 0f));
				if (inventoryEntry != null && num > 0f && inventoryEntry.Amount + purchaseAmount > num)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Main.CANT_PURCHASE_AMOUNT, $"{purchaseAmount:0.##}"));
					Utils.PlayErrorSound();
					return;
				}
				float num2 = (supply.IsJobSupply ? StockMenuScript.Cache.Sum((InventoryEntry i) => i.Amount * (Gtacnr.Data.Items.GetItemBaseDefinition(i.ItemId)?.Weight ?? 0f)) : InventoryMenuScript.Cache.Sum((InventoryEntry i) => i.Amount * (Gtacnr.Data.Items.GetItemBaseDefinition(i.ItemId)?.Weight ?? 0f)));
				float num3 = purchaseAmount * itemInfo.Weight;
				float inventoryCapacityByType = Constants.GetInventoryCapacityByType((!supply.IsJobSupply) ? InventoryType.Primary : InventoryType.Job, jobData.Id);
				if (inventoryCapacityByType > 0f && num2 + num3 > inventoryCapacityByType)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Main.NOT_ENOUGH_INVENTORY_SPACE) + " " + LocalizationController.S(Entries.Main.INVENTORY_SPACE_HINT));
					Utils.PlayErrorSound();
					return;
				}
			}
			else if (supply.Type == BusinessSupplyType.Weapon && Gtacnr.Data.Items.IsWeaponDefined(supply.Item))
			{
				WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(supply.Item);
				name = weaponDefinition.Name;
				uint hashKey = (uint)API.GetHashKey(weaponDefinition.Id);
				bool flag = false;
				if (supply.Extra != null && Gtacnr.Data.Items.IsAmmoDefined((string)supply.Extra))
				{
					flag = false;
					AmmoDefinition ammoDefinition = Gtacnr.Data.Items.GetAmmoDefinition((string)supply.Extra);
					if (ammoDefinition.Limit > 0f)
					{
						int pedAmmoByType = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, API.GetHashKey(ammoDefinition.Id));
						if ((float)(pedAmmoByType + purchaseAmount.ToInt()) > ammoDefinition.Limit)
						{
							purchaseAmount = ammoDefinition.Limit - (float)pedAmmoByType;
							if (purchaseAmount == 0f)
							{
								Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_STORE_CANT_PURCHASE_MORE, ammoDefinition.Name));
								Utils.PlayErrorSound();
								return;
							}
						}
					}
				}
				bool flag2 = ArmoryScript.HasWeapon((WeaponHash)hashKey);
				if (flag && flag2)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.ALREADY_OWN_WEAPON), playSound: false);
					return;
				}
			}
			else if (supply.Type == BusinessSupplyType.Attachment && Gtacnr.Data.Items.IsWeaponComponentDefined(supply.Item))
			{
				WeaponComponentDefinition weaponComponentDefinition = Gtacnr.Data.Items.GetWeaponComponentDefinition(supply.Item);
				name = weaponComponentDefinition.Name;
				uint hashKey2 = (uint)API.GetHashKey(weaponComponentDefinition.Id);
				uint weaponHash = (uint)API.GetHashKey((string)supply.Extra);
				if (ArmoryScript.HasAttachment((WeaponHash)weaponHash, (WeaponComponentHash)hashKey2))
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.ALREADY_OWN_ATTACHMENT), playSound: false);
					return;
				}
				foreach (WeaponComponentDefinition item in from c in Gtacnr.Data.Items.GetAllWeaponComponentDefinitions()
					where ArmoryScript.HasAttachment((WeaponHash)weaponHash, (WeaponComponentHash)API.GetHashKey(c.Id))
					select c)
				{
					if (item.Type == weaponComponentDefinition.Type)
					{
						if (!(await Utils.ShowConfirm(LocalizationController.S(Entries.Businesses.MENU_STORE_ATTACHMENT_WARNING, item.Name), LocalizationController.S(Entries.Main.ATTENTION))))
						{
							return;
						}
						break;
					}
				}
			}
			else if (supply.Type == BusinessSupplyType.Ammo && Gtacnr.Data.Items.IsAmmoDefined(supply.Item))
			{
				AmmoDefinition ammoDefinition2 = Gtacnr.Data.Items.GetAmmoDefinition(supply.Item);
				if (ammoDefinition2.Limit > 0f)
				{
					int pedAmmoByType2 = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, API.GetHashKey(ammoDefinition2.Id));
					if ((float)(pedAmmoByType2 + purchaseAmount.ToInt()) > ammoDefinition2.Limit)
					{
						purchaseAmount = ammoDefinition2.Limit - (float)pedAmmoByType2;
						if (purchaseAmount == 0f)
						{
							Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_STORE_CANT_PURCHASE_MORE, ammoDefinition2.Name));
							Utils.PlayErrorSound();
							return;
						}
					}
				}
				name = ammoDefinition2.Name;
			}
			else
			{
				if (supply.Type != BusinessSupplyType.Clothing || !Gtacnr.Data.Items.IsClothingItemDefined(supply.Item))
				{
					Utils.PlayErrorSound();
					return;
				}
				ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(supply.Item);
				name = clothingItemDefinition.Name;
			}
			string text = null;
			long giftCardBalance = 0L;
			BusinessType type = business.Type;
			if ((uint)(type - 5) <= 1u)
			{
				text = AccountType.AmmunationGiftCard;
			}
			if (text != null)
			{
				giftCardBalance = await Money.GetCachedBalanceOrFetch(text);
			}
			float num4 = 1f;
			if (supply.Type == BusinessSupplyType.Item)
			{
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(supply.Item);
				if (itemDefinition != null)
				{
					num4 = ActiveBusiness.GetMultiplier(itemDefinition.Category.ToString());
				}
			}
			int num5 = supply.CalculateFinalPrice(ActiveBusiness.PriceMultiplier * num4);
			int price = Convert.ToInt32(Math.Ceiling((float)num5 * purchaseAmount));
			if (supply.PurchaseSupplies.Count > 0)
			{
				PurchaseSupply purchaseSupply = supply.PurchaseSupplies.FirstOrDefault((PurchaseSupply ps) => ps.Amount == purchaseAmount);
				if (purchaseSupply != null)
				{
					price = purchaseSupply.CalculateFinalPrice(business.PriceMultiplier * num4);
				}
			}
			if (moneyCache + giftCardBalance < price)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				Utils.PlayErrorSound();
				return;
			}
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:purchaseItem", business.Id, supplyIndex, purchaseAmount);
			switch (responseCode)
			{
			case ResponseCode.Success:
			{
				Utils.PlayPurchaseSound();
				ShoppingScript.ItemPurchased?.Invoke(this, new EventArgs());
				if (BusinessScript.ClosestBusinessEmployee != null)
				{
					string text2 = ((business.Type == BusinessType.BarberShop) ? "SHOP_CUTTING_HAIR" : "SHOP_SELL");
					BusinessScript.ClosestBusinessEmployee.State.Ped.PlayAmbientSpeech(text2, (SpeechModifier)3);
				}
				string text3 = $"~y~{purchaseAmount:0.##}~s~";
				long num6 = price;
				long num7 = 0L;
				if (giftCardBalance > 0)
				{
					num7 = Math.Min(num6, giftCardBalance);
					num6 -= num7;
					UpdateGiftCardBalance();
				}
				if (num7 == 0L)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_STORE_PURCHASED_CASH, text3, name, num6.ToCurrencyString()));
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_STORE_PURCHASED_GIFT_CARD, text3, name, num7.ToCurrencyString(), num6.ToCurrencyString()));
				}
				if (supplyStockCache.ContainsKey(supply.Item))
				{
					supplyStockCache[supply.Item] -= purchaseAmount;
				}
				if (supply.Type == BusinessSupplyType.Weapon || supply.Type == BusinessSupplyType.Attachment || supply.Type == BusinessSupplyType.Ammo)
				{
					InventoryItemBase inventoryItemBase = ((supply.Type == BusinessSupplyType.Weapon) ? Gtacnr.Data.Items.GetWeaponDefinition(supply.Item) : ((supply.Type == BusinessSupplyType.Attachment) ? ((InventoryItemBase)Gtacnr.Data.Items.GetWeaponComponentDefinition(supply.Item)) : ((InventoryItemBase)Gtacnr.Data.Items.GetAmmoDefinition(supply.Item))));
					if (!purchaseIllegalGunTipShown && inventoryItemBase.IsIllegal)
					{
						purchaseIllegalGunTipShown = true;
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DISCLAIMER_ILLEGAL_WEAPONS_AMMO), playSound: false);
					}
					else if (!purchaseArmoryTipShown)
					{
						purchaseArmoryTipShown = true;
						Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.MENU_ACCESS_ARMORY, MainMenuScript.OpenMenuControlString), playSound: false);
					}
					break;
				}
				if (supply.Type == BusinessSupplyType.Clothing)
				{
					if (Gtacnr.Data.Items.GetClothingItemDefinition(supply.Item).Type == ClothingItemType.Tattoos)
					{
						menuItem.Enabled = false;
						menuItem.Label = "...";
						menuItem.RightIcon = MenuItem.Icon.NONE;
						wardrobeCache = await Clothes.GetAllOwned(Gtacnr.Client.API.Jobs.CachedJob, force: true);
						Apparel tempApparel = Utils.GetTempApparel("tattooShop");
						if (wardrobeCache.Contains(supply.Item))
						{
							Clothes.CurrentApparel.Replace(supply.Item);
							tempApparel?.Replace(supply.Item);
						}
						else
						{
							Clothes.CurrentApparel.Remove(supply.Item);
							tempApparel?.Remove(supply.Item);
						}
						Utils.StoreCurrentOutfit();
						RefreshSupplyMenuItem(menuItem);
					}
					else
					{
						if (!purchaseClothingTipShown)
						{
							purchaseClothingTipShown = true;
							Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.MENU_ACCESS_WARDROBE, MainMenuScript.OpenMenuControlString), playSound: false);
						}
						if (wardrobeCache != null)
						{
							wardrobeCache.Add(supply.Item);
						}
						Utils.StoreCurrentOutfit();
						Clothes.CurrentApparel.Replace(supply.Item);
					}
					await Clothes.SaveApparel();
					break;
				}
				if (supply.IsJobSupply)
				{
					if (!purchaseStockTipShown)
					{
						purchaseStockTipShown = true;
						Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.MENU_ACCESS_STOCK, MainMenuScript.OpenMenuControlString));
					}
				}
				else if (!purchaseItemTipShown)
				{
					purchaseItemTipShown = true;
					Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.MENU_ACCESS_INVENTORY, MainMenuScript.OpenMenuControlString), playSound: false);
				}
				InventoryItem itemDefinition2 = Gtacnr.Data.Items.GetItemDefinition(supply.Item);
				if (itemDefinition2 != null && itemDefinition2.GetExtraDataBool("IsPhone"))
				{
					Utils.SetPreference("gtacnr:phoneItem", itemDefinition2.Id);
					Utils.ResetPreference("gtacnr:phoneCase");
				}
				break;
			}
			case ResponseCode.InsufficientMoney:
				Utils.SendNotification(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				Utils.PlayErrorSound();
				break;
			case ResponseCode.NoSpaceLeft:
				Utils.DisplayHelpText("~r~" + LocalizationController.S(Entries.Businesses.SHOP_ERROR_NO_SPACE), playSound: false);
				Utils.PlayErrorSound();
				break;
			case ResponseCode.ScriptCanceled:
				Utils.PlayErrorSound();
				break;
			case ResponseCode.ItemLimitReached:
				Utils.DisplayHelpText("~r~" + LocalizationController.S(Entries.Businesses.SHOP_ERROR_ALREADY_OWNED), playSound: false);
				Utils.PlayErrorSound();
				break;
			case ResponseCode.InsufficientLevel:
				Utils.DisplayHelpText("~r~" + LocalizationController.S(Entries.Businesses.SHOP_ERROR_INSUFFICIENT_LEVEL), playSound: false);
				Utils.PlayErrorSound();
				break;
			case ResponseCode.InsufficientMembershipTier:
				Utils.DisplayHelpText("~r~" + LocalizationController.S(Entries.Businesses.SHOP_ERROR_INSUFFICIENT_MEMBERSHIP), playSound: false);
				Utils.PlayErrorSound();
				break;
			default:
				Utils.DisplayError(responseCode, "", "OnStorePurchase");
				break;
			}
		}
		catch (Exception arg)
		{
			Print($"An exception has occurred: {arg}");
		}
		finally
		{
			purchaseInProgress = false;
			RefreshAllSupplyMenuItems(menu);
		}
	}

	private async void OnStoreShoplift(Menu menu, Control control)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.PUBLIC_OFFICER_CANT_ROB));
			return;
		}
		if (!Gtacnr.Utils.CheckTimePassed(shopliftTimestamp, SHOPLIFT_COOLDOWN))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_SHOPLIFTING_COOLDOWN_SIMPLE));
			return;
		}
		Business activeBusiness = ActiveBusiness;
		if (shopliftBusinessTimestamp.ContainsKey(activeBusiness.Id))
		{
			TimeSpan cooldownTimeLeft = Gtacnr.Utils.GetCooldownTimeLeft(shopliftBusinessTimestamp[activeBusiness.Id], SHOPLIFT_CAUGHT_COOLDOWN);
			if (cooldownTimeLeft > TimeSpan.Zero)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.MENU_STORE_SHOPLIFTING_COOLDOWN, cooldownTimeLeft.TotalSeconds));
				return;
			}
		}
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem == null || currentMenuItem.ItemData == null || purchaseInProgress)
		{
			return;
		}
		shopliftTimestamp = DateTime.UtcNow;
		purchaseInProgress = true;
		try
		{
			if (!(currentMenuItem.ItemData is BusinessSupply businessSupply))
			{
				return;
			}
			string key = activeBusiness.MenuOverride ?? activeBusiness.Type.ToString();
			int num = businessSupplies[key].IndexOf(businessSupply);
			if (num < 0)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.INVALID_ITEM));
				return;
			}
			bool flag = false;
			string name = "";
			if (businessSupply.Type == BusinessSupplyType.Weapon)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_STEAL_WEAPON));
			}
			else if (businessSupply.Type == BusinessSupplyType.Attachment)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_STEAL_WEAPON_ATTACHMENT));
			}
			else if (businessSupply.Type == BusinessSupplyType.Ammo && Gtacnr.Data.Items.IsAmmoDefined(businessSupply.Item))
			{
				AmmoDefinition ammoDefinition = Gtacnr.Data.Items.GetAmmoDefinition(businessSupply.Item);
				name = ammoDefinition.Name;
				flag = true;
			}
			else if (businessSupply.Type == BusinessSupplyType.Clothing && Gtacnr.Data.Items.IsClothingItemDefined(businessSupply.Item))
			{
				ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(businessSupply.Item);
				name = clothingItemDefinition.Name;
				if (clothingItemDefinition.Type == ClothingItemType.Tattoos)
				{
					Utils.PlayErrorSound();
					return;
				}
				flag = true;
			}
			else if (businessSupply.Type == BusinessSupplyType.Item && Gtacnr.Data.Items.IsItemDefined(businessSupply.Item))
			{
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(businessSupply.Item);
				name = itemDefinition.Name;
				flag = true;
			}
			if (!flag)
			{
				return;
			}
			ShopliftResponse shopliftResponse = (ShopliftResponse)(await TriggerServerEventAsync<int>("gtacnr:businesses:shopliftItem", new object[2] { activeBusiness.Id, num }));
			switch (shopliftResponse)
			{
			case ShopliftResponse.Success:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.ROBBERY_SHOPLIFTING_SUCCESS, name));
				Game.PlaySound("ROBBERY_MONEY_TOTAL", "HUD_FRONTEND_CUSTOM_SOUNDSET");
				break;
			case ShopliftResponse.Spotted:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CAUGHT_SHOPLIFTING_REPORTED_POLICE));
				if (BusinessScript.ClosestBusinessEmployee != null)
				{
					BusinessScript.ClosestBusinessEmployee.State.Ped.PlayAmbientSpeech("GENERIC_INSULT_HIGH", (SpeechModifier)3);
				}
				ActiveBusiness.ShopliftDateTime = DateTime.UtcNow;
				MenuController.CloseAllMenus();
				break;
			case ShopliftResponse.Cooldown:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CAUGHT_SHOPLIFTING_WAIT));
				break;
			case ShopliftResponse.NoSpaceLeft:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_INVENTORY_SPACE));
				break;
			case ShopliftResponse.ItemLimitReached:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.CANT_HOLD_MORE_ITEM));
				break;
			case ShopliftResponse.TooFar:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.FAR_BUSINESS_LOC));
				break;
			case ShopliftResponse.ScriptCanceled:
				Utils.PlayErrorSound();
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x2A-{(int)shopliftResponse}"));
				Print($"An error occurred during shoplifting: {shopliftResponse}");
				break;
			}
		}
		catch (Exception arg)
		{
			Print($"An exception has occurred: {arg}");
		}
		finally
		{
			purchaseInProgress = false;
			RefreshAllSupplyMenuItems(menu);
		}
	}

	private async void OnStoreSell(Menu menu, MenuItem menuItem)
	{
		if (purchaseInProgress)
		{
			return;
		}
		purchaseInProgress = true;
		try
		{
			if (!(menuItem.ItemData is Tuple<int, BusinessSupply> { Item2: var demand }))
			{
				Utils.PlayErrorSound();
				return;
			}
			float amount = ((!(menuItem is MenuListItem menuListItem)) ? 1 : (menuListItem.ListIndex + 1));
			Business business = ActiveBusiness;
			string key = business.MenuOverride ?? business.Type.ToString();
			int num = businessDemands[key].IndexOf(demand);
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:sellItem", business.Id, num, amount);
			if (responseCode == ResponseCode.Success)
			{
				Utils.PlayPurchaseSound();
				menu.RemoveMenuItem(menuItem);
				string text = $"~y~{amount:0.##}~s~";
				string name = Gtacnr.Data.Items.GetItemDefinition(demand.Item).Name;
				long amount2 = ((float)demand.CalculateFinalDemandPayout(business) * amount).ToLong();
				Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_STORE_SOLD_CASH, text, name, amount2.ToCurrencyString()));
			}
			else
			{
				Utils.DisplayError(responseCode, "", "OnStoreSell");
				Print($"An error occurred while selling to store: {responseCode}");
			}
		}
		catch (Exception arg)
		{
			Print($"An exception has occurred: {arg}");
		}
		finally
		{
			purchaseInProgress = false;
			RefreshAllSupplyMenuItems(menu);
		}
	}

	private SpecialAttributes AddSpecialAttributes(MenuItem menuItem, BusinessSupply supply, bool isFolder = false)
	{
		SpecialAttributes specialAttributes = SpecialAttributes.None;
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(supply.Item);
		if (itemBaseDefinition != null)
		{
			if (!Gtacnr.Utils.CheckTimePassed(itemBaseDefinition.CreationDate, TimeSpan.FromDays(30.0)))
			{
				string text = "~y~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_NEW);
				if (!menuItem.Text.Contains(text))
				{
					menuItem.Text = menuItem.Text + " " + text;
				}
				specialAttributes |= SpecialAttributes.New;
			}
			if (itemBaseDefinition.DisabledDate != DateTime.MaxValue)
			{
				TimeSpan timeSpan = itemBaseDefinition.DisabledDate - DateTime.UtcNow;
				string text2 = "~r~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_LIMITED);
				if (timeSpan.TotalDays > 0.0 && !menuItem.Text.Contains(text2))
				{
					menuItem.Text = menuItem.Text + " " + text2;
				}
				specialAttributes |= SpecialAttributes.Limited;
			}
		}
		return specialAttributes;
	}

	[EventHandler("gtacnr:time")]
	private void OnTime(int hour, int minute, int dayOfWeek)
	{
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu != null)
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			RefreshMenuItemLimitedTimer(currentMenuItem);
		}
	}

	private void RefreshMenuItemLimitedTimer(MenuItem menuItem)
	{
		if (menuItem == null || !(menuItem.ItemData is BusinessSupply businessSupply))
		{
			return;
		}
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(businessSupply.Item);
		if (itemBaseDefinition == null || itemBaseDefinition.DisabledDate == DateTime.MaxValue || itemBaseDefinition.DisabledDate == default(DateTime))
		{
			return;
		}
		string text;
		if (itemBaseDefinition.DisabledDate < DateTime.UtcNow)
		{
			text = $"{'\u200b'}LAST CHANCE{'\u2009'}";
		}
		else
		{
			TimeSpan timeSpan = itemBaseDefinition.DisabledDate - DateTime.UtcNow;
			text = $"{'\u200b'}{timeSpan.Days:00}:{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}{'\u2009'}";
		}
		if (menuItem.Description != null)
		{
			int num = menuItem.Description.IndexOf('\u200b');
			int num2 = menuItem.Description.IndexOf('\u2009') + 1;
			if (num != -1 && num2 != 0)
			{
				string oldValue = menuItem.Description.Substring(num, num2 - num);
				menuItem.Description = menuItem.Description.Replace(oldValue, text);
			}
			else
			{
				menuItem.Description = menuItem.Description + "\n~r~" + text + "~s~";
			}
		}
	}
}
