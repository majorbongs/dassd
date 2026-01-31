using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Businesses.MechanicShops;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Mechanic;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Premium;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Jobs;

public class SellToPlayersScript : Script
{
	private static SellToPlayersScript instance;

	private readonly Control keyboardControl = (Control)246;

	private readonly Control gamepadControl = (Control)303;

	private readonly string keyboardControlStr = "INPUT_MP_TEXT_CHAT_TEAM";

	private readonly string gamepadControlStr = "INPUT_REPLAY_SCREENSHOT";

	private readonly int offerCooldown = 10000;

	private Player targetPlayer;

	private DateTime lastOfferTimestamp = DateTime.MinValue;

	private bool canOffer;

	private bool isBeingOffered;

	private bool instructionsShown;

	private int currentSellerId = -1;

	private bool isBusy;

	private bool isSameJob;

	private int selectedSupply;

	private static Dictionary<string, Menu> menus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private VehicleModData cachedModData;

	private Vehicle currentVehicle;

	private InventoryEntry selectedEntry;

	private Menu frozenMenu;

	private List<MenuItem> frozenMenuItems = new List<MenuItem>();

	public static List<VehicleModPricingInfo> VehicleMods { get; set; } = Gtacnr.Utils.LoadJson<List<VehicleModPricingInfo>>("data/vehicles/vehicleMods.json");

	public static IReadOnlyDictionary<string, Menu> Menus => menus;

	public SellToPlayersScript()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		instance = this;
	}

	protected override void OnStarted()
	{
		CreateSellerMenu();
		CreateDestinationMenu();
		CreateResprayMenu();
		CreateModsMenu();
		CreateTintsMenu();
	}

	private void CreateSellerMenu()
	{
		menus["seller"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_STP_BUY_TITLE), "")
		{
			PlaySelectSound = false
		};
		MenuController.AddMenu(menus["seller"]);
		menus["seller"].OnListIndexChange += OnMenuListIndexChanged;
		menus["seller"].OnItemSelect += OnMenuItemSelect;
		menus["seller"].OnListItemSelect += OnMenuListItemSelect;
		menus["seller"].OnIndexChange += OnMenuIndexChanged;
		menus["seller"].InstructionalButtons.Clear();
		menus["seller"].InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Businesses.BTN_SELLER_BUY));
		menus["seller"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Businesses.BTN_SELLER_CANCEL));
	}

	private void CreateDestinationMenu()
	{
		menus["dest"] = new Menu("", LocalizationController.S(Entries.Businesses.MENU_STP_SELECT_DEST_SUBTITLE));
		menus["dest"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STP_INVENTORY_TEXT), LocalizationController.S(Entries.Businesses.MENU_STP_INVENTORY_DESCRIPTION))
		{
			ItemData = false,
			LeftIcon = MenuItem.Icon.GTACNR_INVENTORY
		});
		menus["dest"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STP_STOCK_TEXT), LocalizationController.S(Entries.Businesses.MENU_STP_STOCK_DESCRIPTION))
		{
			ItemData = true,
			LeftIcon = MenuItem.Icon.GTACNR_STOCK
		});
		menus["dest"].OnItemSelect += OnMenuItemSelect;
		MenuController.AddMenu(menus["dest"]);
	}

	private void CreateModsMenu()
	{
		menus["mods"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_MODS_SUBTITLE));
		MenuController.AddMenu(menus["mods"]);
	}

	private void CreateTintsMenu()
	{
		menus["tints"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TINTS_SUBTITLE));
		menus["tints"].OnItemSelect += OnMenuItemSelect;
		menus["tints"].OnIndexChange += OnMenuIndexChanged;
		menus["tints"].OnMenuClose += OnModMenuClosed;
		MenuController.AddMenu(menus["tints"]);
	}

	private void CreateResprayMenu()
	{
		menus["respray"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_RESPRAY_SUBTITLE));
		menus["primaryCol"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_PRIMARYCOLOR_SUBTITLE));
		menus["pearlescentCol"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_PEARLESCENTCOLOR_SUBTITLE));
		menus["secondaryCol"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_SECONDARYCOLOR_SUBTITLE));
		menus["trimCol"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TRIMCOLOR_SUBTITLE));
		menus["dashCol"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_DASHBOARDCOLOR_SUBTITLE));
		menus["liveries"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_LIVERIES_SUBTITLE));
		MenuController.AddMenu(menus["respray"]);
		MenuController.AddSubmenu(menus["respray"], menus["primaryCol"]);
		MenuController.AddSubmenu(menus["respray"], menus["pearlescentCol"]);
		MenuController.AddSubmenu(menus["respray"], menus["secondaryCol"]);
		MenuController.AddSubmenu(menus["respray"], menus["trimCol"]);
		MenuController.AddSubmenu(menus["respray"], menus["dashCol"]);
		MenuController.AddSubmenu(menus["respray"], menus["liveries"]);
		Menu menu = menus["respray"];
		MenuItem item = (menuItems["primaryCol"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_PRIMARYCOLOR_SUBTITLE)));
		menu.AddMenuItem(item);
		Menu menu2 = menus["respray"];
		item = (menuItems["pearlescentCol"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_PEARLESCENTCOLOR_SUBTITLE)));
		menu2.AddMenuItem(item);
		Menu menu3 = menus["respray"];
		item = (menuItems["secondaryCol"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_SECONDARYCOLOR_SUBTITLE)));
		menu3.AddMenuItem(item);
		Menu menu4 = menus["respray"];
		item = (menuItems["trimCol"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TRIMCOLOR_SUBTITLE)));
		menu4.AddMenuItem(item);
		Menu menu5 = menus["respray"];
		item = (menuItems["dashCol"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_DASHBOARDCOLOR_SUBTITLE)));
		menu5.AddMenuItem(item);
		Menu menu6 = menus["respray"];
		item = (menuItems["liveries"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_LIVERIES_SUBTITLE)));
		menu6.AddMenuItem(item);
		MenuController.BindMenuItem(menus["respray"], menus["primaryCol"], menuItems["primaryCol"]);
		MenuController.BindMenuItem(menus["respray"], menus["pearlescentCol"], menuItems["pearlescentCol"]);
		MenuController.BindMenuItem(menus["respray"], menus["secondaryCol"], menuItems["secondaryCol"]);
		MenuController.BindMenuItem(menus["respray"], menus["trimCol"], menuItems["trimCol"]);
		MenuController.BindMenuItem(menus["respray"], menus["dashCol"], menuItems["dashCol"]);
		MenuController.BindMenuItem(menus["respray"], menus["liveries"], menuItems["liveries"]);
		menus["primaryCol"].OnItemSelect += OnMenuItemSelect;
		menus["pearlescentCol"].OnItemSelect += OnMenuItemSelect;
		menus["secondaryCol"].OnItemSelect += OnMenuItemSelect;
		menus["trimCol"].OnItemSelect += OnMenuItemSelect;
		menus["dashCol"].OnItemSelect += OnMenuItemSelect;
		menus["liveries"].OnItemSelect += OnMenuItemSelect;
		menus["primaryCol"].OnIndexChange += OnMenuIndexChanged;
		menus["pearlescentCol"].OnIndexChange += OnMenuIndexChanged;
		menus["secondaryCol"].OnIndexChange += OnMenuIndexChanged;
		menus["trimCol"].OnIndexChange += OnMenuIndexChanged;
		menus["dashCol"].OnIndexChange += OnMenuIndexChanged;
		menus["liveries"].OnIndexChange += OnMenuIndexChanged;
		menus["primaryCol"].OnMenuClose += OnModMenuClosed;
		menus["pearlescentCol"].OnMenuClose += OnModMenuClosed;
		menus["secondaryCol"].OnMenuClose += OnModMenuClosed;
		menus["trimCol"].OnMenuClose += OnModMenuClosed;
		menus["dashCol"].OnMenuClose += OnModMenuClosed;
		menus["liveries"].OnMenuClose += OnModMenuClosed;
	}

	private void FreezeMenu(Menu menu)
	{
		if (frozenMenu != null)
		{
			UnfreezeMenu();
		}
		frozenMenu = menu;
		frozenMenuItems.Clear();
		foreach (MenuItem menuItem in frozenMenu.GetMenuItems())
		{
			if (menuItem.Enabled)
			{
				frozenMenuItems.Add(menuItem);
				menuItem.Enabled = false;
			}
		}
		frozenMenu.OnMenuClosing += PreventFrozenMenuFromClosing;
	}

	private void UnfreezeMenu()
	{
		if (frozenMenu == null)
		{
			return;
		}
		foreach (MenuItem frozenMenuItem in frozenMenuItems)
		{
			frozenMenuItem.Enabled = true;
		}
		frozenMenu.OnMenuClosing -= PreventFrozenMenuFromClosing;
		frozenMenuItems.Clear();
		frozenMenu = null;
	}

	private bool PreventFrozenMenuFromClosing(Menu menu)
	{
		return false;
	}

	[Update]
	private async Coroutine UpdateTask()
	{
		Ped playerPed = Game.PlayerPed;
		if (!SpawnScript.HasSpawned || (Entity)(object)playerPed == (Entity)null || !((Entity)playerPed).IsAlive)
		{
			return;
		}
		await Script.Wait(500);
		targetPlayer = null;
		canOffer = false;
		Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		if (!jobData.CanOffer || CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsCuffed || CuffedScript.IsInCustody || SurrenderScript.IsSurrendered || !Gtacnr.Utils.CheckTimePassed(lastOfferTimestamp, offerCooldown) || isBeingOffered)
		{
			return;
		}
		Vector3 position = ((Entity)playerPed).Position;
		float num = 4f;
		bool flag = (Entity)(object)playerPed.CurrentVehicle != (Entity)null;
		if (flag)
		{
			num = 25f;
		}
		foreach (Player item in ((IEnumerable<Player>)((BaseScript)this).Players).ToList())
		{
			if ((Entity)(object)item.Character == (Entity)null || Game.Player.Handle == item.Handle || ((Entity)item.Character).IsDead)
			{
				continue;
			}
			PlayerState playerState = LatentPlayers.Get(item.ServerId);
			if (playerState != null && (jobData.CanOfferToPublicJobs || !playerState.JobEnum.IsPublicService()) && !playerState.AdminDuty && !playerState.GhostMode)
			{
				float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)item.Character).Position);
				if (num2 < num)
				{
					num = num2;
					targetPlayer = item;
				}
			}
		}
		if (targetPlayer == (Player)null || (!API.IsPedFacingPed(API.PlayerPedId(), ((PoolObject)targetPlayer.Character).Handle, 90f) && !flag))
		{
			return;
		}
		PlayerState playerState2 = LatentPlayers.Get(targetPlayer.ServerId);
		if (playerState2 != null)
		{
			bool isCuffed = playerState2.IsCuffed;
			bool isSurrendering = playerState2.IsSurrendering;
			if (!((!jobData.CanOfferToPublicJobs && playerState2.JobEnum.IsPublicService()) || isCuffed || isSurrendering))
			{
				canOffer = true;
			}
		}
	}

	[Update]
	private async Coroutine ControlsTask()
	{
		if (canOffer)
		{
			EnableInstructionalButtons(LocalizationController.S(Entries.Businesses.BTN_STP_OFFER));
			bool flag = Game.IsControlJustPressed(2, keyboardControl) && Utils.IsUsingKeyboard();
			if (!flag)
			{
				flag = await Utils.IsControlHeld(2, gamepadControl) && !Utils.IsUsingKeyboard();
			}
			if (flag)
			{
				Offer();
			}
		}
		else
		{
			DisableInstructionalButtons();
		}
	}

	private void EnableInstructionalButtons(string label)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (!instructionsShown)
		{
			instructionsShown = true;
			Utils.AddInstructionalButton("sellerOffer", new InstructionalButton(Utils.IsUsingKeyboard() ? label : LocalizationController.S(Entries.Main.BTN_HOLD, label), 2, Utils.IsUsingKeyboard() ? keyboardControl : gamepadControl));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionsShown)
		{
			instructionsShown = false;
			Utils.RemoveInstructionalButton("sellerOffer");
		}
	}

	public static void OfferTo(Player player)
	{
		if (instance.canOffer && !(player == (Player)null))
		{
			instance.targetPlayer = player;
			instance.Offer();
		}
	}

	private void Offer()
	{
		BaseScript.TriggerServerEvent("gtacnr:jobs:offer", new object[1] { targetPlayer.ServerId });
		Utils.PlaySelectSound();
		canOffer = false;
		lastOfferTimestamp = DateTime.UtcNow;
		DisableInstructionalButtons();
		PlayerState playerState = LatentPlayers.Get(targetPlayer.ServerId);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_OFFERED, $"{playerState.ColorTextCode}{playerState.Name} ({playerState.Id})"));
	}

	[EventHandler("gtacnr:jobs:onOffered")]
	private async void OnOffered(int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		string text = (Utils.IsUsingKeyboard() ? LocalizationController.S(Entries.Businesses.STP_PRESS, "~" + keyboardControlStr + "~") : LocalizationController.S(Entries.Businesses.STP_HOLD, "~" + gamepadControlStr + "~"));
		string text2 = Gtacnr.Data.Jobs.GetJobData(playerState.Job)?.Name ?? LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_GOT_OFFER, text2, playerState.ColorNameAndId, text));
		isBeingOffered = true;
		DateTime t = DateTime.UtcNow;
		while (!Gtacnr.Utils.CheckTimePassed(t, 7000.0))
		{
			await BaseScript.Delay(0);
			if ((!Utils.IsUsingKeyboard()) ? (await Utils.IsControlHeld(2, gamepadControl)) : Game.IsControlJustPressed(2, keyboardControl))
			{
				Utils.PlaySelectSound();
				Utils.DisplayHelpText();
				OpenSellerMenu(playerId, null);
				break;
			}
		}
		isBeingOffered = false;
	}

	[EventHandler("gtacnr:jobs:onSale")]
	private void OnSale(int playerId, string itemId, float amount, int price)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(itemId);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SOLD, playerState.ColorNameAndId, amount, itemBaseDefinition.Unit, itemBaseDefinition.Name, price.ToCurrencyString()));
	}

	[EventHandler("gtacnr:jobs:onServiceSale")]
	private void OnServiceSale(int playerId, string serviceId, int price, int shopFee, string jExtraData)
	{
		int amount = price - shopFee;
		PlayerState playerState = LatentPlayers.Get(playerId);
		Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		string text = Gtacnr.Data.Items.GetServiceDefinition(serviceId).Name;
		if (!string.IsNullOrEmpty(jExtraData))
		{
			ServiceExtraData serviceExtraData = jExtraData.Unjson<ServiceExtraData>();
			switch (serviceExtraData.Type)
			{
			case "mod":
			{
				VehicleModInfo modInfo = new VehicleModInfo((int)serviceExtraData.Data["Type"], (int)serviceExtraData.Data["Index"]);
				VehicleModPricingInfo vehicleModPricingInfo = VehicleMods.FirstOrDefault((VehicleModPricingInfo item) => item.Id == modInfo.Type);
				if (vehicleModPricingInfo != null)
				{
					text = vehicleModPricingInfo.Name;
				}
				break;
			}
			case "color":
			{
				VehicleColorInfo vehicleColorInfo = new VehicleColorInfo((int)serviceExtraData.Data["Id"], (string)serviceExtraData.Data["Description"]);
				vehicleColorInfo.Type = (string)serviceExtraData.Data["Type"];
				text = vehicleColorInfo.Description + " Respray";
				break;
			}
			case "livery":
			{
				VehicleLiveryInfo vehicleLiveryInfo = new VehicleLiveryInfo((int)serviceExtraData.Data["Index"]);
				text = $"Livery #{vehicleLiveryInfo.Index} Respray";
				break;
			}
			}
		}
		string text2 = LocalizationController.S(Entries.Businesses.STP_SOLD_SERVICE, playerState.ColorNameAndId, text, price.ToCurrencyString());
		if (shopFee > 0)
		{
			text2 = text2 + "~n~" + LocalizationController.S(Entries.Businesses.STP_SOLD_SERVICE_FEE, shopFee.ToCurrencyString(), amount.ToCurrencyString());
		}
		Utils.DisplayHelpText(text2);
	}

	public static void OpenSellerMenu(int playerId, Menu parentMenu = null)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		Player val = ((IEnumerable<Player>)((BaseScript)instance).Players).FirstOrDefault((Player p) => p.ServerId == playerId);
		if (val != (Player)null)
		{
			Vector3 position = ((Entity)val.Character).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 400f)
			{
				PlayerState playerState = LatentPlayers.Get(playerId);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SELLER_TOO_FAR, playerState.ColorNameAndId));
				return;
			}
		}
		menus["seller"].ParentMenu = parentMenu;
		instance.OpenSellerMenu(playerId);
	}

	public static void OpenSellerMenu(Player player, Menu parentMenu = null)
	{
		OpenSellerMenu(player.ServerId, parentMenu);
	}

	private async void OpenSellerMenu(int playerId)
	{
		_ = 3;
		try
		{
			MenuController.CloseAllMenus();
			menus["seller"].Visible = true;
			menus["seller"].ClearMenuItems();
			menus["seller"].AddLoadingMenuItem();
			PlayerState sellerInfo = LatentPlayers.Get(playerId);
			menus["seller"].MenuTitle = Gtacnr.Data.Jobs.GetJobData(sellerInfo.Job)?.Name;
			menus["seller"].MenuSubtitle = LocalizationController.S(Entries.Businesses.MENU_STP_SELLER_SUBTITLE, sellerInfo.ColorNameAndId);
			SellerItemList itemList = (await TriggerServerEventAsync<string>("gtacnr:jobs:getSellerItems", new object[1] { playerId })).Unjson<SellerItemList>();
			long cash = await Money.GetCachedBalanceOrFetch(AccountType.Cash);
			isSameJob = Gtacnr.Client.API.Jobs.CachedJobEnum == sellerInfo.JobEnum;
			MechanicShop closestModShop = MechanicShopScript.GetClosestMechanicShop();
			currentSellerId = playerId;
			menus["seller"].ClearMenuItems();
			currentVehicle = Game.PlayerPed.CurrentVehicle ?? Game.PlayerPed.LastVehicle;
			bool flag = false;
			Vector3 position;
			if ((Entity)(object)currentVehicle == (Entity)null)
			{
				flag = true;
			}
			else
			{
				position = ((Entity)currentVehicle).Position;
				if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 225f)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Vehicle val = null;
				float num = 225f;
				Vehicle[] allVehicles = World.GetAllVehicles();
				foreach (Vehicle val2 in allVehicles)
				{
					position = ((Entity)val2).Position;
					float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
					if (num2 < num)
					{
						num = num2;
						val = val2;
					}
				}
				currentVehicle = val;
			}
			Gtacnr.Data.Jobs.GetJobData(sellerInfo.Job);
			if (itemList.Services != null && itemList.Services.Prices != null)
			{
				await VehiclesMenuScript.EnsureVehicleCache();
				foreach (KeyValuePair<string, float> price in itemList.Services.Prices)
				{
					string key = price.Key;
					float value = price.Value;
					Service serviceDefinition = Gtacnr.Data.Items.GetServiceDefinition(key);
					bool flag2 = itemList.Services.InStock.Contains(key);
					bool flag3 = true;
					if (serviceDefinition == null)
					{
						Print("Warning: ignoring invalid service '" + key + "'.");
						continue;
					}
					if (!string.IsNullOrEmpty(serviceDefinition.License))
					{
						flag3 = itemList.Services.Certifications.Contains(serviceDefinition.License);
					}
					int num3 = 0;
					if ((Entity)(object)currentVehicle != (Entity)null)
					{
						float engineHealth = currentVehicle.EngineHealth;
						float bodyHealth = currentVehicle.BodyHealth;
						float petrolTankHealth = currentVehicle.PetrolTankHealth;
						int num4 = 0;
						if (currentVehicle.Wheels != null)
						{
							for (int j = 0; j < 16; j++)
							{
								num4 += (API.IsVehicleTyreBurst(((PoolObject)currentVehicle).Handle, j, false) ? 1 : 0);
							}
						}
						VehicleClass classType = currentVehicle.ClassType;
						int num5;
						switch (classType - 14)
						{
						case 0:
							if (serviceDefinition.VehicleType != "boat" && serviceDefinition.VehicleType != null)
							{
								continue;
							}
							num5 = 3;
							break;
						case 2:
							if (serviceDefinition.VehicleType != "plane" && serviceDefinition.VehicleType != null)
							{
								continue;
							}
							num5 = 1;
							break;
						case 1:
							if (serviceDefinition.VehicleType != "heli" && serviceDefinition.VehicleType != null)
							{
								continue;
							}
							num5 = 2;
							break;
						default:
							if (serviceDefinition.VehicleType != "land" && serviceDefinition.VehicleType != null)
							{
								continue;
							}
							num5 = 0;
							break;
						}
						switch (serviceDefinition.Type)
						{
						case "fix":
						case "fix_quick":
						case "fix_tires":
						{
							float engineHealth2 = engineHealth;
							float bodyHealth2 = bodyHealth;
							float tankHealth = petrolTankHealth;
							int i = num5;
							uint tiresDamaged = (uint)num4;
							string type = serviceDefinition.Type;
							MechanicRepairtype repairtype = ((!(type == "fix_quick")) ? ((type == "fix_tires") ? MechanicRepairtype.Tires : MechanicRepairtype.Full) : MechanicRepairtype.Quick);
							num3 = Gtacnr.Utils.CalculateRepairPrice(engineHealth2, bodyHealth2, tankHealth, i, tiresDamaged, repairtype);
							break;
						}
						case "wash":
							if (currentVehicle.DirtLevel > 0.5f)
							{
								num3 = Gtacnr.Utils.CalculateWashPrice(num5);
							}
							break;
						case "respray":
							num3 = Gtacnr.Utils.CalculateResprayPrice(num5);
							break;
						}
					}
					else if (serviceDefinition.VehicleType != "land" && serviceDefinition.VehicleType != null)
					{
						continue;
					}
					if (serviceDefinition.Type == "heal")
					{
						num3 = Gtacnr.Utils.CalculateHealPrice();
					}
					else if (serviceDefinition.Type == "cure")
					{
						num3 = Gtacnr.Utils.CalculateCurePrice();
					}
					num3 = Convert.ToInt32(Math.Ceiling((float)num3 * value));
					bool flag4 = cash >= num3;
					MenuItem menuItem;
					if (serviceDefinition.Type == "respray" || serviceDefinition.Type == "mod" || serviceDefinition.Type == "tint")
					{
						menuItem = new MenuItem(serviceDefinition.BuyerName)
						{
							Description = serviceDefinition.BuyerDescription,
							Label = "›"
						};
						switch (serviceDefinition.Type)
						{
						case "respray":
							MenuController.BindMenuItem(menus["seller"], menus["respray"], menuItem);
							break;
						case "mod":
							MenuController.BindMenuItem(menus["seller"], menus["mods"], menuItem);
							break;
						case "tint":
							MenuController.BindMenuItem(menus["seller"], menus["tints"], menuItem);
							break;
						}
					}
					else
					{
						string text = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
						string type2 = serviceDefinition.Type;
						if (!(type2 == "fix"))
						{
							if (type2 == "wash")
							{
								text = LocalizationController.S(Entries.Businesses.STP_SERVICE_WASH_NA);
							}
						}
						else
						{
							text = LocalizationController.S(Entries.Businesses.STP_SERVICE_FIX_NA);
						}
						menuItem = new MenuItem(serviceDefinition.BuyerName)
						{
							Description = serviceDefinition.BuyerDescription,
							Label = ((num3 > 0) ? (num3.ToPriceTagString(cash) ?? "") : text),
							Enabled = (num3 > 0 && flag4),
							ItemData = serviceDefinition
						};
					}
					if (!flag2)
					{
						menuItem.Enabled = false;
						MenuItem menuItem2 = menuItem;
						menuItem2.Description = menuItem2.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NO_TOOLS);
					}
					if (!flag3)
					{
						menuItem.Enabled = false;
						MenuItem menuItem3 = menuItem;
						menuItem3.Description = menuItem3.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NOT_CERTIFIED);
					}
					if (serviceDefinition.NeedsVehicle && (Entity)(object)currentVehicle == (Entity)null)
					{
						menuItem.Enabled = false;
						menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
						MenuItem menuItem4 = menuItem;
						menuItem4.Description = menuItem4.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NO_VEHICLE);
					}
					if (serviceDefinition.NeedsToOwnVehicle && (Entity)(object)currentVehicle != (Entity)null)
					{
						if (ActiveVehicleScript.ActiveVehicleNetId != ((Entity)currentVehicle).NetworkId)
						{
							menuItem.Enabled = false;
							menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
							MenuItem menuItem5 = menuItem;
							menuItem5.Description = menuItem5.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_VEHICLE_NOT_OWNED);
						}
						StoredVehicle storedVehicle = VehiclesMenuScript.VehicleCache.FirstOrDefault((StoredVehicle sv) => sv.Id == ActiveVehicleScript.ActiveVehicleStoredId);
						if (storedVehicle != null && storedVehicle.RentData != null)
						{
							menuItem.Enabled = false;
							menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
							MenuItem menuItem6 = menuItem;
							menuItem6.Description = menuItem6.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_VEHICLE_NOT_OWNED);
						}
					}
					if (serviceDefinition.NeedsToBeAtModShop)
					{
						position = ((Entity)Game.PlayerPed).Position;
						if (((Vector3)(ref position)).DistanceToSquared(closestModShop.ParentBusiness.Location) > 60f.Square() || closestModShop.Type != MechanicType.ModShop)
						{
							menuItem.Enabled = false;
							menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
							MenuItem menuItem7 = menuItem;
							menuItem7.Description = menuItem7.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_MOD_SHOP);
						}
					}
					if (serviceDefinition.WorkAreaType.HasValue)
					{
						MechanicShopWorkArea currentWorkArea = ModShopScript.CurrentWorkArea;
						if (currentWorkArea == null || currentWorkArea.Type != serviceDefinition.WorkAreaType)
						{
							menuItem.Enabled = false;
							menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
							MenuItem menuItem8 = menuItem;
							menuItem8.Description = menuItem8.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_WORK_AREA, ModShopScript.GetWorkAreaTypeDescription(serviceDefinition.WorkAreaType.Value));
						}
					}
					if (serviceDefinition.MustNotBeWanted && await Gtacnr.Client.API.Crime.GetWantedLevel() > 1)
					{
						menuItem.Enabled = false;
						menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
						MenuItem menuItem9 = menuItem;
						menuItem9.Description = menuItem9.Description + "\n" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_WANTED);
					}
					menus["seller"].AddMenuItem(menuItem);
				}
				RefreshModsMenu(cash, itemList.Services);
				RefreshResprayMenu(cash, itemList.Services);
				RefreshTintsMenu(cash, itemList.Services);
				WriteModCache();
			}
			if (itemList.Entries != null)
			{
				Dictionary<string, Menu> dictionary = new Dictionary<string, Menu>();
				foreach (InventoryEntry item in from e in itemList.Entries.Where(delegate(InventoryEntry e)
					{
						InventoryEntryData data = e.Data;
						return data != null && data.Selling != null;
					})
					orderby e.Data.Selling.Path ?? Gtacnr.Data.Items.GetItemBaseDefinition(e.ItemId).DefaultPath, e.Position, Gtacnr.Data.Items.GetItemBaseDefinition(e.ItemId).Name
					select e)
				{
					InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(item.ItemId);
					InventoryEntrySellingData selling = item.Data.Selling;
					string text2 = selling.Path ?? itemBaseDefinition.DefaultPath;
					Menu menu = null;
					if (text2 == string.Empty)
					{
						menu = menus["seller"];
					}
					else if (dictionary.ContainsKey(text2))
					{
						menu = dictionary[text2];
					}
					else
					{
						string[] array = text2.Split('/');
						string text3 = "";
						MenuItem itemData = null;
						string[] array2 = array;
						foreach (string text4 in array2)
						{
							text3 = (text3 + "/" + text4).Trim('/');
							if (!dictionary.ContainsKey(text3))
							{
								dictionary[text3] = new Menu(menus["seller"].MenuTitle, text4 ?? "")
								{
									PlaySelectSound = false,
									MaxDistance = 10f
								};
								dictionary[text3].OnIndexChange += OnMenuIndexChanged;
								dictionary[text3].OnListIndexChange += OnMenuListIndexChanged;
								dictionary[text3].OnItemSelect += OnMenuItemSelect;
								dictionary[text3].OnListItemSelect += OnMenuListItemSelect;
								if (menu == null)
								{
									menu = menus["seller"];
								}
								MenuItem menuItem10 = new MenuItem(text4)
								{
									Label = Utils.MENU_ARROW
								};
								menu.AddMenuItem(menuItem10);
								MenuController.AddSubmenu(menu, dictionary[text3]);
								MenuController.BindMenuItem(menu, dictionary[text3], menuItem10);
								if (text4 == array.Last())
								{
									menuItem10.ItemData = Tuple.Create(item);
								}
								else
								{
									menuItem10.ItemData = itemData;
								}
								itemData = menuItem10;
							}
							menu = dictionary[text3];
						}
					}
					string unit = itemBaseDefinition.Unit ?? LocalizationController.S(Entries.Businesses.STP_ITEM_UNIT_PIECE);
					if (Gtacnr.Data.Items.IsAmmoDefined(itemBaseDefinition.Id))
					{
						unit = "";
					}
					if (selling.Supplies.Count == 0)
					{
						continue;
					}
					MenuItem menuItem11;
					if (selling.Supplies.Count == 1 || (!isSameJob && (Gtacnr.Data.Items.IsWeaponDefined(item.ItemId) || Gtacnr.Data.Items.IsWeaponComponentDefined(item.ItemId))))
					{
						SellableItemSupply sellableItemSupply = selling.Supplies.First();
						bool flag5 = cash >= sellableItemSupply.Price;
						bool flag6 = item.Amount > 0f;
						int amount = Convert.ToInt32(Math.Ceiling((float)sellableItemSupply.Price / sellableItemSupply.Amount));
						if (flag6)
						{
							menuItem11 = new MenuItem(itemBaseDefinition.Name)
							{
								Label = (sellableItemSupply.Price.ToPriceTagString(cash) ?? ""),
								Enabled = (flag5 && flag6),
								ItemData = item
							};
						}
						else
						{
							MenuItem menuItem12 = new MenuItem(itemBaseDefinition.Name);
							menuItem12.Label = LocalizationController.S(Entries.Businesses.STP_ITEM_NO_STOCK);
							menuItem12.Enabled = false;
							menuItem11 = menuItem12;
						}
						menuItem11.Description = LocalizationController.S(Entries.Businesses.STP_ITEM_DESCRIPTION, itemBaseDefinition.Description, amount.ToCurrencyString(), itemBaseDefinition.Unit ?? LocalizationController.S(Entries.Businesses.STP_ITEM_UNIT_PIECE));
					}
					else
					{
						List<string> list = new List<string>();
						SellableItemSupply sellableItemSupply2 = selling.Supplies.First();
						int amount2 = Convert.ToInt32(Math.Ceiling((float)sellableItemSupply2.Price / sellableItemSupply2.Amount));
						if (item.Amount > 0f)
						{
							foreach (SellableItemSupply supply in selling.Supplies)
							{
								bool num6 = cash >= supply.Price;
								bool flag7 = item.Amount >= supply.Amount;
								float num7 = itemBaseDefinition.Limit;
								if (itemBaseDefinition.JobLimits.ContainsKey(Gtacnr.Client.API.Jobs.CachedJob) && isSameJob)
								{
									num7 = itemBaseDefinition.JobLimits[Gtacnr.Client.API.Jobs.CachedJob];
								}
								bool flag8 = supply.Amount <= num7 || num7 == 0f;
								string text5 = ((!num6) ? "~r~" : ((!flag7 || !flag8) ? "~c~" : "~g~"));
								if (isSameJob || flag8)
								{
									list.Add(supply.FormatAmount(unit) + " for " + text5 + supply.Price.ToCurrencyString());
								}
							}
							menuItem11 = new MenuListItem(itemBaseDefinition.Name, list, 0)
							{
								ItemData = item
							};
						}
						else
						{
							MenuItem menuItem12 = new MenuItem(itemBaseDefinition.Name);
							menuItem12.Label = LocalizationController.S(Entries.Businesses.STP_ITEM_NO_STOCK);
							menuItem12.Enabled = false;
							menuItem11 = menuItem12;
						}
						menuItem11.Description = LocalizationController.S(Entries.Businesses.STP_ITEM_DESCRIPTION, itemBaseDefinition.Description, amount2.ToCurrencyString(), itemBaseDefinition.Unit ?? LocalizationController.S(Entries.Businesses.STP_ITEM_UNIT_PIECE));
					}
					if (itemBaseDefinition.RequiredLevel > 0)
					{
						string text6 = "~b~";
						if (Gtacnr.Utils.GetLevelByXP(Users.CachedXP) < itemBaseDefinition.RequiredLevel)
						{
							menuItem11.Enabled = false;
							text6 = "~r~";
						}
						MenuItem menuItem13 = menuItem11;
						menuItem13.Description = menuItem13.Description + text6 + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, itemBaseDefinition.RequiredLevel) + "~s~";
					}
					if ((int)itemBaseDefinition.RequiredMembership > 0)
					{
						if ((int)MembershipScript.MembershipTier < (int)itemBaseDefinition.RequiredMembership)
						{
							menuItem11.Enabled = false;
							MenuItem menuItem14 = menuItem11;
							menuItem14.Description = menuItem14.Description + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(itemBaseDefinition.RequiredMembership)) + "~s~";
						}
						else
						{
							MenuItem menuItem15 = menuItem11;
							menuItem15.Description = menuItem15.Description + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(itemBaseDefinition.RequiredMembership)) + "~s~";
							if ((int)MembershipScript.MembershipTier > (int)itemBaseDefinition.RequiredMembership)
							{
								MenuItem menuItem16 = menuItem11;
								menuItem16.Description = menuItem16.Description + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REQUIRES_LOWER_TIER, Gtacnr.Utils.GetDescription(itemBaseDefinition.RequiredMembership));
							}
						}
					}
					menu.AddMenuItem(menuItem11);
					if (isSameJob)
					{
						string menuTitle = Gtacnr.Data.Jobs.GetJobData(sellerInfo.Job)?.Name ?? LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
						menus["dest"].MenuTitle = menuTitle;
						MenuController.BindMenuItem(menu, menus["dest"], menuItem11);
					}
				}
			}
			if (menus["seller"].GetMenuItems().Count == 0)
			{
				menus["seller"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STORE_NO_ITEMS_TEXT), LocalizationController.S(Entries.Businesses.MENU_STORE_NO_ITEMS_DESC)));
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void WriteModCache()
	{
		if (!((Entity)(object)currentVehicle == (Entity)null))
		{
			cachedModData = VehicleModData.FromVehicle(currentVehicle);
		}
	}

	private void LoadModCache()
	{
		if (!((Entity)(object)currentVehicle == (Entity)null) && cachedModData != null)
		{
			cachedModData.ApplyOnVehicle(currentVehicle);
		}
	}

	private void OnModMenuClosed(Menu menu, MenuClosedEventArgs e)
	{
		LoadModCache();
	}

	private void RefreshModsMenu(long cash = 0L, ServiceData serviceData = null)
	{
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected I4, but got Unknown
		try
		{
			menus["mods"].ClearMenuItems();
			int num = 0;
			if ((Entity)(object)currentVehicle != (Entity)null)
			{
				API.SetVehicleModKit(((PoolObject)currentVehicle).Handle, 0);
				VehicleMod[] allMods = currentVehicle.Mods.GetAllMods();
				if (!serviceData.Prices.TryGetValue("mod_vehicle", out var value))
				{
					value = 1f;
				}
				VehicleMod[] array = allMods;
				foreach (VehicleMod val in array)
				{
					try
					{
						int modType = (int)val.ModType;
						VehicleModPricingInfo vehicleModPricingInfo = VehicleMods.FirstOrDefault((VehicleModPricingInfo vehicleModPricingInfo2) => vehicleModPricingInfo2.Id == modType);
						if (vehicleModPricingInfo == null || vehicleModPricingInfo.BasePrice < 0)
						{
							continue;
						}
						string name = vehicleModPricingInfo.Name;
						if (val.ModCount == 0)
						{
							continue;
						}
						List<int> list = new List<int>();
						bool flag = false;
						DisabledModEntry disabledModEntry = VehicleModRestrictionsScript.GetDisabledModEntry(((Entity)currentVehicle).Model.Hash);
						if (disabledModEntry != null)
						{
							foreach (DisabledModInfo disabledMod in disabledModEntry.DisabledMods)
							{
								if (disabledMod.Type == modType)
								{
									list.Add(disabledMod.Index);
									if (disabledMod.Index == -1)
									{
										flag = true;
										break;
									}
								}
							}
						}
						if (flag)
						{
							continue;
						}
						num++;
						string text = $"mods_{modType}";
						Menu menu = menus["mods"];
						Dictionary<string, MenuItem> dictionary = menuItems;
						MenuItem obj = new MenuItem(name)
						{
							Label = "›"
						};
						MenuItem item = obj;
						dictionary[text] = obj;
						menu.AddMenuItem(item);
						menus[text] = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), name)
						{
							ShowCount = true
						};
						MenuController.BindMenuItem(menus["mods"], menus[text], menuItems[text]);
						int num2 = 0;
						int vehicleMod = API.GetVehicleMod(((PoolObject)currentVehicle).Handle, modType);
						if (vehicleModPricingInfo != null)
						{
							num2 = Convert.ToInt32(Math.Ceiling((float)vehicleModPricingInfo.BasePrice * value));
						}
						Menu menu2 = menus[text];
						item = (menuItems[text + "_stock"] = new MenuItem("Stock " + name)
						{
							ItemData = new VehicleModInfo(modType, -1),
							Label = ((num2 > 0) ? num2.ToPriceTagString(cash) : "N/A"),
							Enabled = (num2 > 0 && vehicleMod != -1),
							RightIcon = ((vehicleMod == -1) ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
						});
						menu2.AddMenuItem(item);
						for (int num3 = 0; num3 < val.ModCount; num3++)
						{
							if (vehicleModPricingInfo.PriceIncrease > 0)
							{
								num2 = Convert.ToInt32(Math.Ceiling((float)(vehicleModPricingInfo.BasePrice + vehicleModPricingInfo.PriceIncrease * (num3 + 1)) * value));
							}
							string key = $"{text}_{num3}";
							string text2 = val.GetLocalizedModName(num3);
							if (string.IsNullOrWhiteSpace(text2))
							{
								text2 = $"#{num3}";
							}
							bool flag2 = false;
							foreach (int item2 in list)
							{
								if (item2 == num3)
								{
									flag2 = true;
								}
							}
							if (!flag2)
							{
								Menu menu3 = menus[text];
								Dictionary<string, MenuItem> dictionary2 = menuItems;
								MenuItem obj2 = new MenuItem(text2)
								{
									ItemData = new VehicleModInfo(modType, num3),
									Label = ((vehicleMod == num3) ? "" : ((num2 > 0) ? num2.ToPriceTagString(cash) : "N/A")),
									Enabled = (num2 > 0 && vehicleMod != num3),
									RightIcon = ((vehicleMod == num3) ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
								};
								item = obj2;
								dictionary2[key] = obj2;
								menu3.AddMenuItem(item);
							}
						}
						menus[text].OnIndexChange += OnMenuIndexChanged;
						menus[text].OnItemSelect += OnMenuItemSelect;
						menus[text].OnMenuClose += OnModMenuClosed;
					}
					catch (Exception exception)
					{
						Print($"Unable to add mod of type {val.ModType} due to an exception:");
						Print(exception);
					}
				}
			}
			if (num == 0)
			{
				menus["mods"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STP_MODS_NONE_TEXT))
				{
					Description = LocalizationController.S(Entries.Businesses.MENU_STP_MODS_NONE_DESCRIPTION),
					Enabled = false
				});
			}
		}
		catch (Exception exception2)
		{
			Print(exception2);
		}
	}

	private void RefreshResprayMenu(long cash = 0L, ServiceData serviceData = null)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected I4, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Invalid comparison between Unknown and I4
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Invalid comparison between Unknown and I4
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Invalid comparison between Unknown and I4
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Invalid comparison between Unknown and I4
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Invalid comparison between Unknown and I4
		try
		{
			int num = 0;
			if (!((Entity)(object)currentVehicle != (Entity)null))
			{
				return;
			}
			int vehicleCategory = 0;
			VehicleClass classType = currentVehicle.ClassType;
			switch (classType - 14)
			{
			case 0:
				vehicleCategory = 3;
				break;
			case 2:
				vehicleCategory = 1;
				break;
			case 1:
				vehicleCategory = 2;
				break;
			}
			int num2 = Gtacnr.Utils.CalculateResprayPrice(vehicleCategory);
			int price = num2 * 3;
			int num3 = num2 * 2;
			menus["primaryCol"].ClearMenuItems();
			menus["pearlescentCol"].ClearMenuItems();
			menus["secondaryCol"].ClearMenuItems();
			menus["trimCol"].ClearMenuItems();
			menus["dashCol"].ClearMenuItems();
			menus["liveries"].ClearMenuItems();
			foreach (VehicleColorInfo value in DealershipScript.VehicleColors.Values)
			{
				bool flag = (int)currentVehicle.Mods.PrimaryColor == value.Id;
				menus["primaryCol"].AddMenuItem(new MenuItem(value.Description ?? "")
				{
					ItemData = value,
					Label = (flag ? "" : num2.ToPriceTagString(cash)),
					Enabled = (num2 > 0 && !flag),
					RightIcon = (flag ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
				});
				flag = (int)currentVehicle.Mods.PearlescentColor == value.Id;
				menus["pearlescentCol"].AddMenuItem(new MenuItem(value.Description ?? "")
				{
					ItemData = value,
					Label = (flag ? "" : num3.ToPriceTagString(cash)),
					Enabled = (num3 > 0 && !flag),
					RightIcon = (flag ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
				});
				flag = (int)currentVehicle.Mods.SecondaryColor == value.Id;
				menus["secondaryCol"].AddMenuItem(new MenuItem(value.Description ?? "")
				{
					ItemData = value,
					Label = (flag ? "" : num2.ToPriceTagString(cash)),
					Enabled = (num2 > 0 && !flag),
					RightIcon = (flag ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
				});
				flag = (int)currentVehicle.Mods.TrimColor == value.Id;
				menus["trimCol"].AddMenuItem(new MenuItem(value.Description ?? "")
				{
					ItemData = value,
					Label = (flag ? "" : num2.ToPriceTagString(cash)),
					Enabled = (num2 > 0 && !flag),
					RightIcon = (flag ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
				});
				flag = (int)currentVehicle.Mods.TrimColor == value.Id;
				menus["dashCol"].AddMenuItem(new MenuItem(value.Description ?? "")
				{
					ItemData = value,
					Label = (flag ? "" : num2.ToPriceTagString(cash)),
					Enabled = (num2 > 0 && !flag),
					RightIcon = (flag ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
				});
			}
			if (!Gtacnr.Utils.IsVehicleModelAPoliceVehicle(((Entity)currentVehicle).Model.Hash))
			{
				IEnumerable<int> collection = ((currentVehicle.Mods.LiveryCount > 0) ? Enumerable.Range(0, currentVehicle.Mods.LiveryCount) : Enumerable.Empty<int>());
				HashSet<int> hashSet = new HashSet<int>(collection);
				hashSet.ExceptWith(CustomScript.GetRestrictedLiveries(((Entity)currentVehicle).Model.Hash));
				foreach (int item in hashSet)
				{
					num++;
					bool flag2 = currentVehicle.Mods.Livery == item;
					menus["liveries"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STP_LIVERIES_TEXT, item))
					{
						ItemData = new VehicleLiveryInfo(item),
						Label = (flag2 ? "" : price.ToPriceTagString(cash)),
						Enabled = (num2 > 0 && !flag2),
						RightIcon = (flag2 ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
					});
				}
			}
			if (num == 0)
			{
				menuItems["liveries"].Enabled = false;
				if (currentVehicle.Mods[(VehicleModType)48].ModCount > 0)
				{
					menuItems["liveries"].Description = LocalizationController.S(Entries.Businesses.MENU_STP_LIVERIES_MODS);
				}
				else
				{
					menuItems["liveries"].Description = LocalizationController.S(Entries.Businesses.MENU_STP_LIVERIES_NONE);
				}
			}
			else
			{
				menuItems["liveries"].Enabled = true;
				menuItems["liveries"].Description = "";
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void RefreshTintsMenu(long cash = 0L, ServiceData serviceData = null)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Invalid comparison between Unknown and I4
		try
		{
			if ((Entity)(object)currentVehicle != (Entity)null)
			{
				menus["tints"].ClearMenuItems();
				string[] array = new string[7] { "Clear", "Pure Black", "Dark Smoke", "Light Smoke", "Stock", "Limo", "Green" };
				for (int i = 0; i < 7; i++)
				{
					int num = Gtacnr.Utils.CalculateTintPrice(i);
					bool flag = (int)currentVehicle.Mods.WindowTint == i;
					menus["tints"].AddMenuItem(new MenuItem(array[i], "Apply a ~y~" + array[i] + " ~s~tint to your windows.")
					{
						ItemData = new VehicleTintInfo(i),
						Label = (flag ? "" : num.ToPriceTagString(cash)),
						Enabled = (num > 0 && !flag),
						RightIcon = (flag ? MenuItem.Icon.CAR : MenuItem.Icon.NONE)
					});
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void OnMenuIndexChanged(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position;
		if (newItem.ItemData is VehicleModInfo vehicleModInfo)
		{
			if (IsSelectedVehicleUnavailable())
			{
				MenuController.CloseAllMenus();
				Utils.SendNotification(LocalizationController.S(Entries.Businesses.STP_VEHICLE_TOO_FAR));
				return;
			}
			position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)currentVehicle).Position) > 225f)
			{
				MenuController.CloseAllMenus();
				return;
			}
			if (vehicleModInfo.Index < 0)
			{
				API.RemoveVehicleMod(((PoolObject)currentVehicle).Handle, vehicleModInfo.Type);
				return;
			}
			bool vehicleModVariation = API.GetVehicleModVariation(((PoolObject)currentVehicle).Handle, 23);
			API.SetVehicleModKit(((PoolObject)currentVehicle).Handle, 0);
			API.SetVehicleMod(((PoolObject)currentVehicle).Handle, vehicleModInfo.Type, vehicleModInfo.Index, vehicleModVariation);
		}
		else if (newItem.ItemData is VehicleColorInfo vehicleColorInfo)
		{
			if (IsSelectedVehicleUnavailable())
			{
				MenuController.CloseAllMenus();
				return;
			}
			position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)currentVehicle).Position) > 225f)
			{
				MenuController.CloseAllMenus();
			}
			else if (menu == menus["primaryCol"])
			{
				currentVehicle.Mods.PrimaryColor = (VehicleColor)vehicleColorInfo.Id;
			}
			else if (menu == menus["pearlescentCol"])
			{
				currentVehicle.Mods.PearlescentColor = (VehicleColor)vehicleColorInfo.Id;
			}
			else if (menu == menus["secondaryCol"])
			{
				currentVehicle.Mods.SecondaryColor = (VehicleColor)vehicleColorInfo.Id;
			}
			else if (menu == menus["trimCol"])
			{
				currentVehicle.Mods.TrimColor = (VehicleColor)vehicleColorInfo.Id;
			}
			else if (menu == menus["dashCol"])
			{
				currentVehicle.Mods.DashboardColor = (VehicleColor)vehicleColorInfo.Id;
			}
		}
		else if (newItem.ItemData is VehicleLiveryInfo vehicleLiveryInfo)
		{
			if (IsSelectedVehicleUnavailable())
			{
				MenuController.CloseAllMenus();
				return;
			}
			position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)currentVehicle).Position) > 225f)
			{
				MenuController.CloseAllMenus();
			}
			else
			{
				currentVehicle.Mods.Livery = vehicleLiveryInfo.Index;
			}
		}
		else if (newItem.ItemData is VehicleTintInfo vehicleTintInfo)
		{
			if (IsSelectedVehicleUnavailable())
			{
				MenuController.CloseAllMenus();
				return;
			}
			position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)currentVehicle).Position) > 225f)
			{
				MenuController.CloseAllMenus();
			}
			else
			{
				currentVehicle.Mods.WindowTint = (VehicleWindowTint)vehicleTintInfo.Index;
			}
		}
		else if (newItem.ItemData is InventoryEntry inventoryEntry && isSameJob)
		{
			MenuItem menuItem = menus["dest"].GetMenuItems().FirstOrDefault((MenuItem i) => !(bool)i.ItemData);
			string text = LocalizationController.S(Entries.Businesses.MENU_STP_INVENTORY_TEXT);
			MenuItem.Icon leftIcon = MenuItem.Icon.GTACNR_INVENTORY;
			if (Gtacnr.Data.Items.IsWeaponDefined(inventoryEntry.ItemId) || Gtacnr.Data.Items.IsAmmoDefined(inventoryEntry.ItemId) || Gtacnr.Data.Items.IsWeaponComponentDefined(inventoryEntry.ItemId))
			{
				text = "Armory";
				leftIcon = MenuItem.Icon.GTACNR_ARMORY;
			}
			menuItem.Text = text;
			menuItem.LeftIcon = leftIcon;
		}
	}

	private void OnMenuListIndexChanged(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		if (listItem.ItemData is InventoryEntry inventoryEntry)
		{
			InventoryEntryData data = inventoryEntry.Data;
			if (data != null && data.Selling != null)
			{
				SellableItemSupply sellableItemSupply = data.Selling.Supplies[newSelectionIndex];
				int amount = Convert.ToInt32(Math.Ceiling((float)sellableItemSupply.Price / sellableItemSupply.Amount));
				InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(inventoryEntry.ItemId);
				listItem.Description = LocalizationController.S(Entries.Businesses.STP_ITEM_DESCRIPTION, itemBaseDefinition.Description, amount.ToCurrencyString(), itemBaseDefinition.Unit ?? LocalizationController.S(Entries.Businesses.STP_ITEM_UNIT_PIECE));
			}
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is InventoryEntry entry)
		{
			if (isSameJob)
			{
				selectedEntry = entry;
				selectedSupply = 0;
			}
			else
			{
				PurchaseItem(entry, selectedSupply);
			}
			return;
		}
		object itemData = menuItem.ItemData;
		if (itemData is Service service)
		{
			if (service.NeedsVehicle)
			{
				if (IsSelectedVehicleUnavailable())
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NO_VEHICLE), playSound: false);
					Utils.PlayErrorSound();
					return;
				}
				if (currentVehicle.Speed > 2f)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_VEHICLE_MOVING), playSound: false);
					Utils.PlayErrorSound();
					return;
				}
				Player val = new Player(API.GetPlayerFromServerId(currentSellerId));
				if (val == (Player)null || (Entity)(object)val.Character == (Entity)null || val.Character.IsInVehicle())
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_SELLER_INSIDE), playSound: false);
					Utils.PlayErrorSound();
					return;
				}
			}
			if (await PurchaseService(service) && (service.Type == "fix" || service.Type == "wash"))
			{
				menuItem.Enabled = false;
				menuItem.Label = ((service.Type == "fix") ? LocalizationController.S(Entries.Businesses.STP_SERVICE_FIX_NA) : LocalizationController.S(Entries.Businesses.STP_SERVICE_WASH_NA));
			}
		}
		else if (menu == menus["dest"])
		{
			menus["dest"].GoBack();
			PurchaseItem(selectedEntry, selectedSupply, (bool)menuItem.ItemData);
		}
		else if (menuItem.ItemData is VehicleModInfo data)
		{
			Service serviceDefinition = Gtacnr.Data.Items.GetServiceDefinition("mod_vehicle");
			if (serviceDefinition == null || !(await PurchaseService(serviceDefinition, new ServiceExtraData
			{
				Type = "mod",
				Data = data
			})))
			{
				return;
			}
			foreach (MenuItem menuItem2 in menu.GetMenuItems())
			{
				if (menuItem2 == menuItem)
				{
					menuItem2.Enabled = false;
					menuItem2.RightIcon = MenuItem.Icon.CAR;
				}
				else
				{
					menuItem2.Enabled = true;
					menuItem2.RightIcon = MenuItem.Icon.NONE;
				}
			}
		}
		else if (menuItem.ItemData is VehicleColorInfo vehicleColorInfo)
		{
			Service serviceDefinition2 = Gtacnr.Data.Items.GetServiceDefinition("respray_vehicle");
			if (serviceDefinition2 == null)
			{
				return;
			}
			if (menu == menus["primaryCol"])
			{
				vehicleColorInfo.Type = "primary";
			}
			else if (menu == menus["pearlescentCol"])
			{
				vehicleColorInfo.Type = "pearlescent";
			}
			else if (menu == menus["secondaryCol"])
			{
				vehicleColorInfo.Type = "secondary";
			}
			else if (menu == menus["trimCol"])
			{
				vehicleColorInfo.Type = "trim";
			}
			else if (menu == menus["dashCol"])
			{
				vehicleColorInfo.Type = "dash";
			}
			if (!(await PurchaseService(serviceDefinition2, new ServiceExtraData
			{
				Type = "color",
				Data = vehicleColorInfo
			})))
			{
				return;
			}
			foreach (MenuItem menuItem3 in menu.GetMenuItems())
			{
				if (menuItem3 == menuItem)
				{
					menuItem3.Enabled = false;
					menuItem3.RightIcon = MenuItem.Icon.CAR;
				}
				else
				{
					menuItem3.Enabled = true;
					menuItem3.RightIcon = MenuItem.Icon.NONE;
				}
			}
		}
		else if (menuItem.ItemData is VehicleLiveryInfo data2)
		{
			Service serviceDefinition3 = Gtacnr.Data.Items.GetServiceDefinition("respray_vehicle");
			if (serviceDefinition3 == null || !(await PurchaseService(serviceDefinition3, new ServiceExtraData
			{
				Type = "livery",
				Data = data2
			})))
			{
				return;
			}
			foreach (MenuItem menuItem4 in menu.GetMenuItems())
			{
				if (menuItem4 == menuItem)
				{
					menuItem4.Enabled = false;
					menuItem4.RightIcon = MenuItem.Icon.CAR;
				}
				else
				{
					menuItem4.Enabled = true;
					menuItem4.RightIcon = MenuItem.Icon.NONE;
				}
			}
		}
		else
		{
			if (!(menuItem.ItemData is VehicleTintInfo vehicleTintInfo))
			{
				return;
			}
			Service serviceDefinition4 = Gtacnr.Data.Items.GetServiceDefinition("window_tint");
			if (serviceDefinition4 == null || !(await PurchaseService(serviceDefinition4, new ServiceExtraData
			{
				Type = "tint",
				Data = vehicleTintInfo.Index
			})))
			{
				return;
			}
			foreach (MenuItem menuItem5 in menu.GetMenuItems())
			{
				if (menuItem5 == menuItem)
				{
					menuItem5.Enabled = false;
					menuItem5.RightIcon = MenuItem.Icon.CAR;
				}
				else
				{
					menuItem5.Enabled = true;
					menuItem5.RightIcon = MenuItem.Icon.NONE;
				}
			}
		}
	}

	private async void OnMenuListItemSelect(Menu menu, MenuListItem menuItem, int selectedIndex, int itemIndex)
	{
		if (menuItem.ItemData is InventoryEntry entry)
		{
			if (isSameJob)
			{
				selectedEntry = entry;
				selectedSupply = selectedIndex;
			}
			else
			{
				PurchaseItem(entry, selectedIndex);
			}
			return;
		}
		object itemData = menuItem.ItemData;
		if (itemData is Service service && await PurchaseService(service) && (service.Type == "fix" || service.Type == "wash"))
		{
			menuItem.Enabled = false;
			menuItem.Label = ((service.Type == "fix") ? LocalizationController.S(Entries.Businesses.STP_SERVICE_FIX_NA) : LocalizationController.S(Entries.Businesses.STP_SERVICE_WASH_NA));
		}
	}

	private async void PurchaseItem(InventoryEntry entry, int supplyIndex, bool useJobInventory = false)
	{
		InventoryEntryData data = entry.Data;
		if (data == null || data.Selling == null || isBusy)
		{
			return;
		}
		isBusy = true;
		try
		{
			FreezeMenu(MenuController.GetCurrentMenu());
			BuyItemResponse buyItemResponse = (BuyItemResponse)(await TriggerServerEventAsync<int>("gtacnr:jobs:buyItem", new object[5] { currentSellerId, entry.ItemId, supplyIndex, useJobInventory, "" }));
			InventoryItemBase itemBaseDefinition = Gtacnr.Data.Items.GetItemBaseDefinition(entry.ItemId);
			SellableItemSupply sellableItemSupply = data.Selling.Supplies[supplyIndex];
			switch (buyItemResponse)
			{
			case BuyItemResponse.Success:
			{
				Utils.PlaySelectSound();
				string text = itemBaseDefinition.Unit ?? "";
				if (Gtacnr.Data.Items.IsAmmoDefined(itemBaseDefinition.Id))
				{
					text = "";
				}
				if (text.Length > 2)
				{
					text = " " + text;
					if (sellableItemSupply.Amount > 1f && LocalizationController.CurrentLanguage.StartsWith("en"))
					{
						text += "s";
					}
				}
				Utils.SendNotification(LocalizationController.S(Entries.Businesses.STP_ITEM_PURCHASED, sellableItemSupply.Amount, text, itemBaseDefinition.Name, sellableItemSupply.Price.ToCurrencyString()));
				break;
			}
			case BuyItemResponse.NoMoney:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY), playSound: false);
				break;
			case BuyItemResponse.NoSpaceLeft:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_INVENTORY_SPACE), playSound: false);
				break;
			case BuyItemResponse.ItemLimitReached:
				Utils.PlayErrorSound();
				if (Gtacnr.Data.Items.IsWeaponDefined(entry.ItemId))
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.ALREADY_OWN_WEAPON), playSound: false);
				}
				else if (Gtacnr.Data.Items.IsWeaponComponentDefined(entry.ItemId))
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.ALREADY_OWN_ATTACHMENT), playSound: false);
				}
				else
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.CANT_PURCHASE_THAT_AMOUNT), playSound: false);
				}
				break;
			case BuyItemResponse.TooFar:
				Utils.PlayErrorSound();
				MenuController.CloseAllMenus();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_ITEM_ERROR_TOO_FAR), playSound: false);
				break;
			case BuyItemResponse.OutOfStock:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_ITEM_ERROR_NO_STOCK, itemBaseDefinition.Name), playSound: false);
				break;
			default:
				Utils.PlayErrorSound();
				MenuController.CloseAllMenus();
				Utils.DisplayErrorMessage(74, (int)buyItemResponse, null, playSound: false);
				Print($"An error occurred during the purchase: {buyItemResponse}");
				break;
			}
		}
		catch (Exception exception)
		{
			MenuController.CloseAllMenus();
			Utils.DisplayErrorMessage(8);
			Print(exception);
		}
		finally
		{
			isBusy = false;
			UnfreezeMenu();
		}
	}

	private async Task<bool> PurchaseService(Service service, ServiceExtraData data = null)
	{
		if (isBusy)
		{
			return false;
		}
		isBusy = true;
		FreezeMenu(MenuController.GetCurrentMenu());
		try
		{
			if (service.NeedsVehicle && IsSelectedVehicleUnavailable())
			{
				return false;
			}
			int num = 0;
			if ((Entity)(object)currentVehicle != (Entity)null)
			{
				VehicleClass classType = currentVehicle.ClassType;
				switch (classType - 14)
				{
				case 0:
					num = 3;
					break;
				case 2:
					num = 1;
					break;
				case 1:
					num = 2;
					break;
				}
			}
			int sellerId = currentSellerId;
			SellToPlayersScript sellToPlayersScript = this;
			object[] obj = new object[5] { sellerId, service.Id, null, null, null };
			Vehicle obj2 = currentVehicle;
			obj[2] = ((obj2 != null) ? new int?(((Entity)obj2).NetworkId) : ((int?)null));
			obj[3] = num;
			obj[4] = data?.Json() ?? "";
			BuyItemResponse buyItemResponse = (BuyItemResponse)(await sellToPlayersScript.TriggerServerEventAsync<int>("gtacnr:jobs:buyService", obj));
			switch (buyItemResponse)
			{
			case BuyItemResponse.Success:
				Utils.PlaySelectSound();
				ApplyPurchasedService(service.Id, currentVehicle, sellerId, data);
				return true;
			case BuyItemResponse.NoMoney:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY), playSound: false);
				break;
			case BuyItemResponse.TooFar:
				Utils.PlayErrorSound();
				MenuController.CloseAllMenus();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_ITEM_ERROR_TOO_FAR), playSound: false);
				break;
			case BuyItemResponse.OutOfStock:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NO_STOCK, service.Name), playSound: false);
				break;
			case BuyItemResponse.NoCertification:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NOT_CERTIFIED_NAMED, service.Name), playSound: false);
				break;
			case BuyItemResponse.VehicleNotOwned:
				Utils.PlayErrorSound();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_RENTED_VEHICLE), playSound: false);
				break;
			default:
				Utils.PlayErrorSound();
				MenuController.CloseAllMenus();
				Utils.DisplayErrorMessage(75, (int)buyItemResponse, null, playSound: false);
				Print($"An error occurred during the purchase: {buyItemResponse}");
				break;
			}
		}
		catch (Exception exception)
		{
			MenuController.CloseAllMenus();
			Utils.DisplayErrorMessage(9);
			Print(exception);
		}
		finally
		{
			isBusy = false;
			UnfreezeMenu();
		}
		return false;
	}

	private void ApplyPurchasedService(string serviceId, Vehicle vehicle, int sellerId, ServiceExtraData extraData = null)
	{
		try
		{
			if (!((Entity)(object)vehicle != (Entity)null))
			{
				return;
			}
			Gtacnr.Data.Jobs.GetJobData(LatentPlayers.Get(sellerId).Job);
			Service serviceDefinition = Gtacnr.Data.Items.GetServiceDefinition(serviceId);
			if (serviceDefinition.Type == "fix")
			{
				for (int i = 0; i < 16; i++)
				{
					try
					{
						API.SetVehicleTyreFixed(((PoolObject)vehicle).Handle, i);
						API.SetVehicleWheelHealth(((PoolObject)vehicle).Handle, i, 1000f);
					}
					catch (Exception exception)
					{
						Print(exception);
						break;
					}
				}
				vehicle.BodyHealth = 1000f;
				vehicle.EngineHealth = 1000f;
				vehicle.PetrolTankHealth = 1000f;
				vehicle.Repair();
				DisableMountedGunsScript.DisableMountedGuns(vehicle);
			}
			else if (serviceDefinition.Type == "wash")
			{
				vehicle.Wash();
			}
			else if (serviceDefinition.Type == "respray" || serviceDefinition.Type == "mod" || serviceDefinition.Type == "tint")
			{
				WriteModCache();
			}
		}
		catch (Exception exception2)
		{
			Utils.DisplayErrorMessage(89);
			Print(exception2);
		}
	}

	private bool IsSelectedVehicleUnavailable()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (!((Entity)(object)currentVehicle == (Entity)null) && currentVehicle.Exists())
		{
			Vector3 position = ((Entity)currentVehicle).Position;
			return ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 225f;
		}
		return true;
	}

	private bool IsSellerMenuOpen()
	{
		if (!menus["seller"].Visible && !menus["seller"].ChildrenMenus.Any((Menu m) => m.Visible))
		{
			return menus["seller"].ChildrenMenus.SelectMany((Menu m) => m.ChildrenMenus).Any((Menu m) => m.Visible);
		}
		return true;
	}

	[EventHandler("gtacnr:jobs:pricesUpdated")]
	private void OnPricesUpdated(int sellerId)
	{
		if (currentSellerId == sellerId && IsSellerMenuOpen() && !DealershipScript.IsInDealership)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.STP_SERVICES_UPDATED), playSound: false);
			OpenSellerMenu(sellerId);
		}
	}
}
