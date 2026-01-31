using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Items;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.PlayerInteraction;

public class PlayerMenuScript : Script
{
	private static PlayerMenuScript instance;

	private readonly long MAX_TRANSACTION_VALUE = 5000000L;

	private readonly IEnumerable<long> validTransferAmounts = new List<long> { 1000L, 2000L, 5000L, 10000L, 20000L, 50000L, 100000L, 200000L, 500000L, 1000000L };

	private List<long> giveMoneyAmounts = new List<long>();

	private Player targetPlayer;

	private bool canOpenMenu;

	private int openMenuWl = -1;

	private bool isBusy;

	public static Menu PlayerMenu { get; private set; }

	private Dictionary<string, MenuItem> menuItems { get; } = new Dictionary<string, MenuItem>();

	public static Player MenuTargetPlayer { get; private set; }

	public PlayerMenuScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		PlayerMenu = new Menu(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_TITLE), "")
		{
			MaxDistance = 3f
		};
		PlayerMenu.OnItemSelect += OnMenuItemSelect;
		PlayerMenu.OnListItemSelect += OnMenuListItemSelect;
	}

	private void OpenMenu(Menu parentMenu = null)
	{
		if (!PlayerMenu.Visible)
		{
			MenuController.CloseAllMenus();
			PlayerMenu.ParentMenu = parentMenu;
			PlayerMenu.OpenMenu();
			RefreshMenu();
		}
	}

	private void CloseMenu()
	{
		PlayerMenu.CloseMenu();
	}

	public static void OpenBribeMenu(int playerId, Menu parentMenu = null)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		if (!PlayerMenu.Visible)
		{
			MenuController.CloseAllMenus();
			PlayerMenu.ParentMenu = parentMenu;
			PlayerMenu.OpenMenu();
			SetTargetPlayer(new Player(API.GetPlayerFromServerId(playerId)));
		}
	}

	public static void SetTargetPlayer(Player player)
	{
		instance.targetPlayer = player;
		instance.RefreshMenu();
	}

	public static void DisableMaxDistance()
	{
		PlayerMenu.MaxDistance = 0f;
	}

	private async void RefreshMenu()
	{
		_ = 1;
		try
		{
			if (targetPlayer == (Player)null)
			{
				CloseAndBeep();
				return;
			}
			MenuTargetPlayer = targetPlayer;
			int serverId = MenuTargetPlayer.ServerId;
			PlayerState playerInfo = LatentPlayers.Get(Game.Player);
			PlayerState targetInfo = LatentPlayers.Get(serverId);
			Job pJobInfo = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
			Job tJobInfo = Gtacnr.Data.Jobs.GetJobData(targetInfo.Job);
			PlayerMenu.MaxDistance = 3f;
			if (playerInfo == null || targetInfo == null)
			{
				CloseAndBeep();
				return;
			}
			long cash = Money.GetCachedBalance(AccountType.Cash);
			if (cash <= 0)
			{
				PlayerMenu.ClearMenuItems();
				PlayerMenu.AddLoadingMenuItem();
				cash = await Money.GetBalance(AccountType.Cash);
			}
			int level = Gtacnr.Utils.GetLevelByXP(Users.CachedXP);
			if (InventoryMenuScript.Cache == null)
			{
				await InventoryMenuScript.ReloadInventory();
			}
			IEnumerable<InventoryEntry> cache = InventoryMenuScript.Cache;
			PlayerMenu.MenuSubtitle = targetInfo.ColorNameAndId;
			PlayerMenu.CounterPreText = tJobInfo.GetColoredName(targetInfo.WantedLevel);
			PlayerMenu.ClearMenuItems();
			List<string> list = new List<string>();
			openMenuWl = playerInfo.WantedLevel;
			bool canBeCuffed = targetInfo.CanBeCuffed;
			bool isCuffed = targetInfo.IsCuffed;
			bool isInCustody = targetInfo.IsInCustody;
			bool flag = !canBeCuffed && targetInfo.CanBeStopped;
			MenuItem item;
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed && MenuTargetPlayer.Character.IsInVehicle(Game.PlayerPed.CurrentVehicle))
			{
				Menu playerMenu = PlayerMenu;
				item = (menuItems["eject"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_PLAYER_EJECT), LocalizationController.S(Entries.Vehicles.MENU_PLAYER_EJECT_DESC)));
				playerMenu.AddMenuItem(item);
			}
			if (pJobInfo.CanOffer && (pJobInfo.CanOfferToPublicJobs || !targetInfo.JobEnum.IsPublicService()))
			{
				Menu playerMenu2 = PlayerMenu;
				item = (menuItems["offerItems"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_OFFER_SERVICES), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_OFFER_SERVICES_DESCRIPTION)));
				playerMenu2.AddMenuItem(item);
			}
			if (tJobInfo.CanOffer && (tJobInfo.CanOfferToPublicJobs || !playerInfo.JobEnum.IsPublicService()))
			{
				Menu playerMenu3 = PlayerMenu;
				item = (menuItems["browseItems"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BROWSE_SERVICES), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BROWSE_SERVICES_DESCRIPTION)));
				playerMenu3.AddMenuItem(item);
			}
			if (!playerInfo.JobEnum.IsPublicService())
			{
				if (targetInfo.JobEnum.IsPolice())
				{
					foreach (int item2 in BribeScript.BribeValues[playerInfo.WantedLevel])
					{
						list.Add("~g~" + item2.ToCurrencyString());
					}
					if (list.Count == 0)
					{
						Menu playerMenu4 = PlayerMenu;
						Dictionary<string, MenuItem> dictionary = menuItems;
						MenuItem obj = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_BRIBE_TITLE))
						{
							Enabled = false
						};
						item = obj;
						dictionary["offerBribe"] = obj;
						playerMenu4.AddMenuItem(item);
						if (playerInfo.WantedLevel < 1)
						{
							menuItems["offerBribe"].Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BRIBE_NOT_WANTED);
						}
						else if (playerInfo.WantedLevel > 4)
						{
							menuItems["offerBribe"].Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BRIBE_MOST_WANTED);
						}
					}
					else
					{
						Menu playerMenu5 = PlayerMenu;
						item = (menuItems["offerBribe"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BRIBE_TEXT), list, 0)
						{
							Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BRIBE_DESCRIPTION)
						});
						playerMenu5.AddMenuItem(item);
					}
				}
				else if (isCuffed)
				{
					Menu playerMenu6 = PlayerMenu;
					item = (menuItems["uncuff"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_UNCUFF), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_UNCUFF_DESCRIPTION)));
					playerMenu6.AddMenuItem(item);
				}
			}
			else if (playerInfo.JobEnum.IsPolice() && !targetInfo.JobEnum.IsPublicService())
			{
				bool flag2 = MenuTargetPlayer.Character.IsInVehicle();
				if (isCuffed)
				{
					if (isInCustody)
					{
						Menu playerMenu7 = PlayerMenu;
						item = (menuItems["arrestMenu"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_ARREST)));
						playerMenu7.AddMenuItem(item);
					}
					else
					{
						Menu playerMenu8 = PlayerMenu;
						item = (menuItems["holdCustody"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CUSTODY)));
						playerMenu8.AddMenuItem(item);
					}
				}
				else if (canBeCuffed && !flag2)
				{
					Menu playerMenu9 = PlayerMenu;
					item = (menuItems["cuff"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CUFF)));
					playerMenu9.AddMenuItem(item);
				}
				else if (flag)
				{
					Menu playerMenu10 = PlayerMenu;
					item = (menuItems["giveTicket"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_TICKET)));
					playerMenu10.AddMenuItem(item);
				}
				if (targetInfo.WantedLevel > 1)
				{
					if (flag2 && !isInCustody)
					{
						Vehicle currentVehicle = MenuTargetPlayer.Character.CurrentVehicle;
						_ = MenuTargetPlayer.Character.SeatIndex;
						VehicleWindowIndex val = currentVehicle.ClosestSeatWindow(((Entity)Game.PlayerPed).Position);
						if ((int)val != -1 && currentVehicle.Windows.HasWindow(val) && currentVehicle.Windows[val].IsIntact)
						{
							Menu playerMenu11 = PlayerMenu;
							item = (menuItems["breakWindow"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BREAK_WINDOW)));
							playerMenu11.AddMenuItem(item);
							InventoryEntry inventoryEntry = cache.FirstOrDefault((InventoryEntry i) => i.ItemId == "window_punch");
							if (inventoryEntry == null || inventoryEntry.Amount == 0f)
							{
								InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition("window_punch");
								menuItems["breakWindow"].Enabled = false;
								menuItems["breakWindow"].Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_BREAK_WINDOW_NO_ITEM, itemDefinition.Name);
							}
						}
					}
					Menu playerMenu12 = PlayerMenu;
					item = (menuItems["suggestBribe"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_DESCRIPTION)));
					playerMenu12.AddMenuItem(item);
					if (targetInfo.WantedLevel == 5)
					{
						menuItems["suggestBribe"].Enabled = false;
						MenuItem menuItem12 = menuItems["suggestBribe"];
						menuItem12.Description = menuItem12.Description + "\n" + LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_MOST_WANTED);
					}
				}
			}
			if (DrugScript.IsPlayerOverdosing(MenuTargetPlayer))
			{
				Menu playerMenu13 = PlayerMenu;
				item = (menuItems["antidote"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_ANTIDOTE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_ANTIDOTE_DESCRIPTION)));
				playerMenu13.AddMenuItem(item);
				InventoryEntry inventoryEntry2 = cache.FirstOrDefault((InventoryEntry i) => i.ItemId == "naloxone");
				if (inventoryEntry2 == null || inventoryEntry2.Amount == 0f)
				{
					InventoryItem itemDefinition2 = Gtacnr.Data.Items.GetItemDefinition("naloxone");
					menuItems["antidote"].Enabled = false;
					menuItems["antidote"].Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_ANTIDOTE_NO_ITEM, itemDefinition2.Name);
				}
			}
			if (playerInfo.JobEnum.IsPublicService() == targetInfo.JobEnum.IsPublicService())
			{
				List<string> list2 = new List<string>();
				bool enabled = false;
				string description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_CASH_DESCRIPTION, targetInfo);
				giveMoneyAmounts.Clear();
				if (level >= 4)
				{
					if (cash > 0)
					{
						foreach (long validTransferAmount in validTransferAmounts)
						{
							if (validTransferAmount > cash)
							{
								giveMoneyAmounts.Add(cash);
								list2.Add("~g~" + cash.ToCurrencyString());
								break;
							}
							giveMoneyAmounts.Add(validTransferAmount);
							list2.Add("~g~" + validTransferAmount.ToCurrencyString());
						}
						list2.Add(LocalizationController.S(Entries.Banking.MENU_ATM_CUSTOM_AMOUNT));
						enabled = true;
					}
					else
					{
						list2.Add(LocalizationController.S(Entries.Banking.MENU_ATM_NO_FUNDS));
						enabled = false;
					}
				}
				else
				{
					description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_CASH_LEVEL, 4);
				}
				menuItems["giveMoney"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_CASH), list2, 0)
				{
					Description = description,
					Enabled = enabled
				};
				PlayerMenu.AddMenuItem(menuItems["giveMoney"]);
			}
			Menu playerMenu14 = PlayerMenu;
			item = (menuItems["giveItem"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_ITEM), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_ITEM_DESCRIPTION, targetInfo)));
			playerMenu14.AddMenuItem(item);
			Menu playerMenu15 = PlayerMenu;
			item = (menuItems["giveKeys"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_KEYS), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_KEYS_DESCRIPTION) + "~n~~y~This feature is not available yet!")
			{
				Enabled = false
			});
			playerMenu15.AddMenuItem(item);
			Menu playerMenu16 = PlayerMenu;
			item = (menuItems["offerTrade"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_OFFER_TRADE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_OFFER_TRADE_DESCRIPTION)));
			playerMenu16.AddMenuItem(item);
			if (PlayerMenu.GetMenuItems().Count == 0)
			{
				PlayerMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_NO_ACTIONS), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_NO_ACTIONS_DESCRIPTION))
				{
					Enabled = false
				});
			}
		}
		catch (Exception exception)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			Print(exception);
		}
		void CloseAndBeep()
		{
			CloseMenu();
			Utils.PlayErrorSound();
		}
	}

	[Update]
	private async Coroutine FindTargetTask()
	{
		await Script.Wait(500);
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		float num = 4f;
		targetPlayer = null;
		bool flag = Game.PlayerPed.IsInVehicle();
		if (flag)
		{
			num = 1.9599999f;
		}
		bool isInCustody = CuffedScript.IsInCustody;
		Vector3 entityCoords = API.GetEntityCoords(((PoolObject)Game.PlayerPed).Handle, false);
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player == Game.Player || ((Entity)player.Character).IsDead)
			{
				continue;
			}
			bool flag2 = player.Character.IsInVehicle();
			if (!flag && !isInCustody && !API.IsPedFacingPed(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)player.Character).Handle, 60f))
			{
				continue;
			}
			PlayerState playerState = LatentPlayers.Get(player);
			if (playerState == null || playerState.GhostMode || playerState.AdminDuty)
			{
				continue;
			}
			Vector3 val = ((Entity)player.Character).Position;
			Vector3 val2 = entityCoords;
			if (flag)
			{
				if (flag2)
				{
					continue;
				}
				Vector3 rightVector = ((Entity)Game.PlayerPed).RightVector;
				VehicleSeat seatIndex = Game.PlayerPed.SeatIndex;
				if ((int)seatIndex == -1 || (int)seatIndex == 1)
				{
					val2 -= rightVector * 0.3f;
				}
				if ((int)seatIndex == 0 || (int)seatIndex == 2)
				{
					val2 += rightVector * 0.3f;
				}
			}
			if (flag2)
			{
				Vector3 rightVector2 = ((Entity)player.Character).RightVector;
				VehicleSeat seatIndex2 = player.Character.SeatIndex;
				if ((int)seatIndex2 == -1 || (int)seatIndex2 == 1)
				{
					val -= rightVector2 * 0.3f;
				}
				if ((int)seatIndex2 == 0 || (int)seatIndex2 == 2)
				{
					val += rightVector2 * 0.3f;
				}
			}
			float num2 = ((Vector3)(ref val)).DistanceToSquared(val2);
			if (num2 < num)
			{
				num = num2;
				targetPlayer = player;
			}
		}
		if (targetPlayer != (Player)null && !CuffedScript.IsCuffed && !DrugScript.IsOverdosing && !Game.PlayerPed.IsBeingStunned)
		{
			EnableControl();
		}
		else
		{
			DisableControl();
		}
		void DisableControl()
		{
			if (canOpenMenu)
			{
				canOpenMenu = false;
				Utils.RemoveInstructionalButton("openPlayerMenu");
				KeysScript.DetachListener((Control)51, OnControlEvent);
			}
		}
		void EnableControl()
		{
			if (!canOpenMenu)
			{
				canOpenMenu = true;
				Utils.AddInstructionalButton("openPlayerMenu", new InstructionalButton(LocalizationController.S(Entries.Main.BTN_INTERACT), 2, (Control)51));
				KeysScript.AttachListener((Control)51, OnControlEvent, 10);
			}
		}
	}

	private bool OnControlEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed)
		{
			OpenMenu();
		}
		return false;
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (MenuTargetPlayer == (Player)null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x15"));
		}
		else if (IsSelected("offerItems"))
		{
			menu.CloseMenu();
			SellToPlayersScript.OfferTo(MenuTargetPlayer);
		}
		else if (IsSelected("browseItems"))
		{
			menu.CloseMenu();
			SellToPlayersScript.OpenSellerMenu(MenuTargetPlayer, PlayerMenu);
		}
		else if (IsSelected("offerTrade"))
		{
			menu.CloseMenu();
			TradingMenuScript.Instance.SendTradeOffer(MenuTargetPlayer);
		}
		else if (IsSelected("uncuff"))
		{
			menu.CloseMenu();
			if (Game.PlayerPed.IsBeingStunned)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_CANT_UNCUFF_WHEN_STUNNED));
			}
			else if (CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsCuffed)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_CANT_UNCUFF_WHEN_CUFFED));
			}
			else if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)(object)MenuTargetPlayer.Character.CurrentVehicle)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_CANT_UNCUFF_WHEN_IN_VEHICLE));
			}
			else if (!(await TriggerServerEventAsync<bool>("gtacnr:police:uncuff", new object[1] { MenuTargetPlayer.ServerId })))
			{
				Print($"Unable to uncuff player {MenuTargetPlayer.ServerId}");
			}
		}
		else if (IsSelected("giveItem"))
		{
			menu.CloseMenu();
			InventoryMenuScript.OnItemSelected = InventoryMenuScript.Instance.OnInventoryItemGive;
			InventoryMenuScript.ItemSelectInstructionalText = LocalizationController.S(Entries.Imenu.BTN_IMENU_INVENTORY_GIVE);
			InventoryMenuScript.Open(setDefaults: false);
		}
		else
		{
			if (IsSelected("giveKeys") || IsSelected("addFriend"))
			{
				return;
			}
			if (IsSelected("arrestMenu"))
			{
				menu.CloseMenu();
				ArrestMenuScript.ShowArrestMenu();
			}
			else if (IsSelected("cuff"))
			{
				CuffScript.TargetPlayer = MenuTargetPlayer;
				if (await CuffScript.Cuff())
				{
					menu.CloseMenu();
				}
			}
			else if (IsSelected("giveTicket"))
			{
				CuffScript.GiveTicket(MenuTargetPlayer);
				menu.CloseMenu();
			}
			else if (IsSelected("holdCustody"))
			{
				CuffScript.HoldSuspectInCustody(MenuTargetPlayer);
				menu.CloseMenu();
			}
			else if (IsSelected("breakWindow"))
			{
				menu.CloseMenu();
				Vehicle targetVeh = MenuTargetPlayer.Character.CurrentVehicle;
				if ((Entity)(object)targetVeh == (Entity)null)
				{
					Utils.PlayErrorSound();
					return;
				}
				_ = MenuTargetPlayer.Character.SeatIndex;
				VehicleWindowIndex window = targetVeh.ClosestSeatWindow(((Entity)Game.PlayerPed).Position);
				if ((int)window == -1 || !targetVeh.Windows.HasWindow(window) || !targetVeh.Windows[window].IsIntact)
				{
					Utils.PlayErrorSound();
					return;
				}
				await Utils.LoadAnimDictionary("melee@unarmed@streamed_variations");
				API.TaskTurnPedToFaceEntity(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)targetVeh).Handle, 1000);
				await BaseScript.Delay(1000);
				if (targetVeh.Speed >= 1f)
				{
					Utils.PlayErrorSound();
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_BREAK_WINDOW_MOVING), playSound: false);
					return;
				}
				Prop prop = await World.CreateProp(new Model(Gtacnr.Data.Items.GetItemDefinition("window_punch").Model), ((Entity)Game.PlayerPed).Position, false, false);
				API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 57005), 0.13f, -0.01f, 21.24f, -1.54f, 0f, 0f, true, true, false, true, 1, true);
				Game.PlayerPed.Task.PlayAnimation("melee@unarmed@streamed_variations", "plyr_takedown_rear_backhit");
				await BaseScript.Delay(300);
				((PoolObject)prop).Delete();
				if (await TriggerServerEventAsync<bool>("gtacnr:police:breakWindow", new object[1] { MenuTargetPlayer.ServerId }))
				{
					PlayerState playerState = LatentPlayers.Get(MenuTargetPlayer.ServerId);
					targetVeh.Windows[window].Smash();
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_BREAK_WINDOW_SUCCESS, playerState.ColorNameAndId));
				}
			}
			else if (IsSelected("antidote"))
			{
				Game.PlayerPed.Task.PlayAnimation("amb@medic@standing@kneel@base", "base");
				await BaseScript.Delay(2000);
				Game.PlayerPed.Task.PlayAnimation("amb@medic@standing@kneel@exit", "exit");
				await BaseScript.Delay(1000);
				ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:drugs:giveAntidote", MenuTargetPlayer.ServerId);
				if (responseCode == ResponseCode.Success)
				{
					PlayerState playerState2 = LatentPlayers.Get(MenuTargetPlayer.ServerId);
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.GAVE_ANTIDOTE, playerState2.ColorNameAndId));
				}
				else
				{
					Utils.DisplayError(responseCode, "", "OnMenuItemSelect");
				}
				await BaseScript.Delay(500);
				Game.PlayerPed.Task.ClearAnimation("amb@medic@standing@kneel@exit", "exit");
			}
			else if (IsSelected("suggestBribe"))
			{
				PlayerState playerState3 = LatentPlayers.Get(MenuTargetPlayer.ServerId);
				BaseScript.TriggerServerEvent("gtacnr:police:suggestBribe", new object[1] { MenuTargetPlayer.ServerId });
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_YOU_ASKED_FOR_BRIBE, playerState3.ColorNameAndId));
				menuItem.Enabled = false;
				menuItem.Label = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_REQUESTED);
			}
			else if (IsSelected("eject"))
			{
				if (await DashboardMenuScript.EjectPlayer(MenuTargetPlayer))
				{
					DashboardMenuScript.UpdatePassengersMenuItem();
				}
				CloseMenu();
			}
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}

	[EventHandler("gtacnr:police:windowBroken")]
	private void OnWindowBroken(int officerId)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		PlayerState playerState = LatentPlayers.Get(officerId);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_WINDOW_BROKEN, playerState.ColorNameAndId));
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		VehicleWindowIndex vehicleWindowBySeat = Utils.GetVehicleWindowBySeat(Game.PlayerPed.SeatIndex);
		currentVehicle.LockStatus = (VehicleLockStatus)1;
		if ((int)vehicleWindowBySeat != -1)
		{
			currentVehicle.Windows[vehicleWindowBySeat].Smash();
		}
	}

	private async void OnMenuListItemSelect(Menu menu, MenuListItem menuItem, int selectedIndex, int itemIndex)
	{
		if (MenuTargetPlayer == (Player)null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TOO_FAR_FROM_PLAYER));
			return;
		}
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		isBusy = true;
		try
		{
			long amount2;
			if (IsSelected("offerBribe"))
			{
				int num = await Gtacnr.Client.API.Crime.GetWantedLevel();
				if (num != openMenuWl)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_PLAYERMENU_WANTED_CHANGED));
					RefreshMenu();
					return;
				}
				menu.CloseMenu();
				int amount = BribeScript.BribeValues[num][selectedIndex];
				await BribeScript.Bribe(MenuTargetPlayer.ServerId, amount);
			}
			else if (IsSelected("giveMoney"))
			{
				if (selectedIndex < giveMoneyAmounts.Count)
				{
					amount2 = giveMoneyAmounts[selectedIndex];
				}
				else
				{
					PlayerState playerState = LatentPlayers.Get(MenuTargetPlayer);
					if (!int.TryParse(await Utils.GetUserInput(LocalizationController.S(Entries.Player.INPUT_PLAYERMENU_GIVE_CASH), LocalizationController.S(Entries.Player.INPUT_PLAYERMENU_GIVE_CASH_TEXT, playerState), "", 11), out var result))
					{
						isBusy = false;
						return;
					}
					if (result > MAX_TRANSACTION_VALUE || result < 0)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.PLAYER_ONLY_SEND_AT_A_TIME, MAX_TRANSACTION_VALUE.ToCurrencyString()));
						isBusy = false;
						return;
					}
					amount2 = result;
				}
				if (amount2 > 0)
				{
					PlayerState targetInfo = LatentPlayers.Get(MenuTargetPlayer);
					SendMoneyResponse sendMoneyResponse = (SendMoneyResponse)(await TriggerServerEventAsync<int>("gtacnr:transferMoneyToPlayer", new object[2] { MenuTargetPlayer.ServerId, amount2 }));
					switch (sendMoneyResponse)
					{
					case SendMoneyResponse.Success:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.YOU_GAVE_CURRENCY, targetInfo.ColorNameAndId, amount2.ToCurrencyString()));
						Animate();
						break;
					case SendMoneyResponse.InvalidAmount:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.AMOUNT_ENTERED_INVALID));
						break;
					case SendMoneyResponse.InvalidRecipient:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RECIPIENT_NO_LONGER_ONLINE));
						break;
					case SendMoneyResponse.InsufficientFunds:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
						break;
					case SendMoneyResponse.TooFar:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TOO_FAR_FROM_RECIPIENT));
						break;
					case SendMoneyResponse.InsufficientLevel:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.DONT_HAVE_REQUIRED_LEVEL_TRANSFER_MONEY));
						break;
					case SendMoneyResponse.PlayerLimit:
					case SendMoneyResponse.GlobalLimit:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.SENT_TOO_MUCH_MONEY_ANOTHER_SESSION));
						break;
					case SendMoneyResponse.PlayerCooldown:
					case SendMoneyResponse.GlobalCooldown:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.SENDING_MONEY_TOO_FAST));
						break;
					case SendMoneyResponse.TargetPlayerLimit:
					case SendMoneyResponse.TargetGlobalLimit:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.PLAYER_RECEIVED_TOO_MUCH_MONEY));
						break;
					case SendMoneyResponse.TargetPlayerCooldown:
					case SendMoneyResponse.TargetGlobalCooldown:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.PLAYER_RECEIVED_MONEY_RECENTLY));
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x15-{(int)sendMoneyResponse}"));
						break;
					}
				}
			}
			async void Animate()
			{
				if (!Game.PlayerPed.IsInVehicle())
				{
					Tuple<string, string, int> animInfo = ((amount2 >= 1000000) ? Tuple.Create("impexp_int-0", "mp_m_waremech_01_dual-0", 3000) : ((amount2 >= 500000) ? Tuple.Create("impexp_int-0", "mp_m_waremech_01_dual-0", 3000) : ((amount2 >= 100000) ? Tuple.Create("mp_ped_interaction", "handshake_guy_a", 1500) : ((amount2 >= 50000) ? Tuple.Create("mp_ped_interaction", "handshake_guy_a", 1500) : ((amount2 >= 10000) ? Tuple.Create("mp_ped_interaction", "handshake_guy_a", 1500) : Tuple.Create("mp_ped_interaction", "handshake_guy_a", 1500))))));
					Tuple<int, string, float, float, float, float, float, Tuple<float>> propInfo = ((amount2 >= 1000000) ? Tuple.Create(24817, "prop_cash_case_02", -0.09f, 0.558f, -0.01f, 6f, 90f, 180f) : ((amount2 >= 500000) ? Tuple.Create(24817, "h4_prop_h4_cash_stack_02a", -0.09f, 0.423f, -0.01f, 6f, 90f, 180f) : ((amount2 >= 100000) ? Tuple.Create(57005, "prop_anim_cash_pile_02", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f) : ((amount2 >= 50000) ? Tuple.Create(57005, "prop_anim_cash_pile_01", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f) : ((amount2 >= 10000) ? Tuple.Create(57005, "xs_prop_arena_cash_pile_s", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f) : Tuple.Create(57005, "prop_cash_note_01", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f))))));
					API.RequestAnimDict(animInfo.Item1);
					while (!API.HasAnimDictLoaded(animInfo.Item1))
					{
						await BaseScript.Delay(0);
					}
					Game.PlayerPed.Task.PlayAnimation(animInfo.Item1, animInfo.Item2, 4f, animInfo.Item3, (AnimationFlags)51);
					Prop prop = await World.CreateProp(new Model(propInfo.Item2), ((Entity)Game.PlayerPed).Position, false, false);
					API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, propInfo.Item1), propInfo.Item3, propInfo.Item4, propInfo.Item5, propInfo.Item6, propInfo.Item7, propInfo.Rest.Item1, true, true, false, true, 1, true);
					await BaseScript.Delay(animInfo.Item3);
					Game.PlayerPed.Task.ClearAnimation(animInfo.Item1, animInfo.Item2);
					((PoolObject)prop).Delete();
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x15"));
		}
		isBusy = false;
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}
}
