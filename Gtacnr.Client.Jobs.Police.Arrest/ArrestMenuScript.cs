using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.PlayerInteraction;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class ArrestMenuScript : Script
{
	private static Menu arrestMenu;

	private static Menu searchMenu;

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private bool isBusy;

	private int lastSearchedSuspect;

	public static ArrestMenuScript Instance { get; private set; }

	public ArrestMenuScript()
	{
		Instance = this;
	}

	protected override void OnStarted()
	{
		CreateMenus();
	}

	private void CreateMenus()
	{
		arrestMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARREST_TITLE), LocalizationController.S(Entries.Businesses.MENU_ARREST_SUBTITLE))
		{
			MaxDistance = 3f
		};
		arrestMenu.OnItemSelect += OnMenuItemSelect;
		MenuController.AddMenu(arrestMenu);
		searchMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_SEARCH_RESULTS_TITLE));
		searchMenu.OnItemSelect += OnMenuItemSelect;
		searchMenu.OnIndexChange += OnMenuIndexChange;
		MenuController.AddMenu(searchMenu);
		RefreshMenu();
		RefreshSearchMenuInstructionalButtons();
	}

	private void RefreshSearchMenuInstructionalButtons()
	{
		searchMenu.InstructionalButtons.Clear();
		MenuItem currentMenuItem = searchMenu.GetCurrentMenuItem();
		if (currentMenuItem != null)
		{
			if (currentMenuItem == menuItems["seizeAll"] || currentMenuItem == menuItems["suggestBribe"])
			{
				searchMenu.InstructionalButtons.Add((Control)201, "Confirm");
			}
			searchMenu.InstructionalButtons.Add((Control)202, "Ignore");
		}
	}

	public static void ShowArrestMenu()
	{
		Menus.CloseAll();
		arrestMenu.OpenMenu();
		Instance.RefreshMenu();
	}

	public static void HideArrestMenu()
	{
		arrestMenu.CloseMenu();
	}

	private void RefreshMenu()
	{
		try
		{
			arrestMenu.ClearMenuItems();
			arrestMenu.MenuSubtitle = "";
			arrestMenu.AddLoadingMenuItem();
			if (Gtacnr.Client.API.Jobs.CachedJob != "police")
			{
				HideArrestMenu();
				return;
			}
			if (CuffScript.TargetPlayer == (Player)null)
			{
				HideArrestMenu();
				return;
			}
			int serverId = CuffScript.TargetPlayer.ServerId;
			PlayerState playerState = LatentPlayers.Get(serverId);
			if (serverId == 0)
			{
				HideArrestMenu();
				return;
			}
			int? num = CuffScript.TargetPlayer.State["gtacnr:police:arrestingOfficer"] as int?;
			if (num.HasValue && num.Value != Game.Player.ServerId)
			{
				PlayerState playerState2 = LatentPlayers.Get(num.Value);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_OFFICER_HOLDING_IN_CUSTODY, playerState2.ColorNameAndId, playerState.ColorNameAndId));
				HideArrestMenu();
				return;
			}
			BaseScript.TriggerServerEvent("gtacnr:obtainCustody", new object[1] { CuffScript.TargetPlayer.ServerId });
			Ped character = CuffScript.TargetPlayer.Character;
			Vehicle currentVehicle = character.CurrentVehicle;
			dynamic val = (Entity)(object)currentVehicle != (Entity)null && (dynamic)((Entity)currentVehicle).State.Get("gtacnr:isTransportUnit") == true;
			string colorTextCode = playerState.ColorTextCode;
			if (val)
			{
				HideArrestMenu();
				return;
			}
			arrestMenu.ClearMenuItems();
			arrestMenu.MenuSubtitle = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUBTITLE, playerState.ColorNameAndId);
			dynamic val2 = CuffScript.TargetPlayer.State.Get("gtacnr:police:copImFollowing");
			if (val2 == null || val2 == 0 || val2 == Game.Player.ServerId)
			{
				if (!character.IsInVehicle())
				{
					Menu menu = arrestMenu;
					MenuItem item = (menuItems["search"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SEARCH), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SEARCH_DESC, colorTextCode)));
					menu.AddMenuItem(item);
					if (val2 == null || val2 == 0)
					{
						Menu menu2 = arrestMenu;
						item = (menuItems["follow"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_FOLLOW), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_FOLLOW_DESC, colorTextCode)));
						menu2.AddMenuItem(item);
					}
					else if (val2 == Game.Player.ServerId)
					{
						Menu menu3 = arrestMenu;
						item = (menuItems["stop"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_STOP_FOLLOWING), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_STOP_FOLLOWING_DESC, colorTextCode)));
						menu3.AddMenuItem(item);
					}
					Menu menu4 = arrestMenu;
					item = (menuItems["enter"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_ENTER_VEHICLE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_ENTER_VEHICLE_DESC, colorTextCode))
					{
						Label = "~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_BONUS)
					});
					menu4.AddMenuItem(item);
					if (ManualTransportScript.IsSuspectCloseToFrontDesk(character))
					{
						Menu menu5 = arrestMenu;
						item = (menuItems["handover"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_HANDOVER), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_HANDOVER_DESC, colorTextCode))
						{
							Label = "~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_BONUS)
						});
						menu5.AddMenuItem(item);
					}
					else
					{
						Menu menu6 = arrestMenu;
						item = (menuItems["transport"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_QUICK_ARREST), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_QUICK_ARREST_DESC, colorTextCode)));
						menu6.AddMenuItem(item);
					}
					Menu menu7 = arrestMenu;
					item = (menuItems["suggestBribe"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_DESCRIPTION).Replace("~o~", colorTextCode)));
					menu7.AddMenuItem(item);
					menuItems["suggestBribe"].Enabled = playerState.WantedLevel < 5;
				}
				else
				{
					Menu menu8 = arrestMenu;
					MenuItem item = (menuItems["exit"] = new MenuItem(Entries.Player.MENU_PLAYERMENU_EXIT_VEHICLE, LocalizationController.S(Entries.Player.MENU_PLAYERMENU_EXIT_VEHICLE_DESC, colorTextCode)));
					menu8.AddMenuItem(item);
				}
			}
			if (arrestMenu.GetMenuItems().Count == 0)
			{
				HideArrestMenu();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_IN_CUSTODY));
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		try
		{
			isBusy = true;
			int targetServerId = CuffScript.TargetPlayer.ServerId;
			Ped character = CuffScript.TargetPlayer.Character;
			PlayerState targetInfo = LatentPlayers.Get(targetServerId);
			if (menu == arrestMenu)
			{
				dynamic val = CuffScript.TargetPlayer.State.Get("gtacnr:police:copImFollowing");
				dynamic val2 = val > 0;
				dynamic val3 = val == Game.Player.ServerId;
				if (IsSelected("search"))
				{
					HideArrestMenu();
					if (val2 && !val3)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_IN_CUSTODY));
						return;
					}
					if (lastSearchedSuspect == targetServerId)
					{
						if (searchMenu.GetMenuItems().Count > 0 && searchMenu.MenuSubtitle == $"{targetInfo.Name} ({targetInfo.Id})")
						{
							searchMenu.OpenMenu();
							return;
						}
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_SEARCHED, targetInfo.ColorNameAndId));
						return;
					}
					lastSearchedSuspect = targetServerId;
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_SEARCHING_SUSPECT, targetInfo.ColorNameAndId));
					await Utils.LoadAnimDictionary("frisk@law");
					Game.PlayerPed.Task.PlayAnimation("frisk@law", "frisk_clip");
					await BaseScript.Delay(5000);
					Game.PlayerPed.Task.ClearAnimation("frisk@law", "frisk_clip");
					PoliceSearchResults policeSearchResults = (await TriggerServerEventAsync<string>("gtacnr:police:searchSuspect", new object[1] { targetServerId })).Unjson<PoliceSearchResults>();
					if (policeSearchResults.ResponseCode == PoliceSearchResponse.Success)
					{
						long num = 0L;
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_SEARCHING_ILLEGAL_ITEMS, targetInfo.ColorNameAndId));
						searchMenu.ClearMenuItems();
						searchMenu.MenuSubtitle = $"{targetInfo.Name} ({targetInfo.Id})";
						int num2 = 0;
						MenuItem menuItem2;
						foreach (InventoryEntry foundEntry in policeSearchResults.FoundEntries)
						{
							if (Gtacnr.Data.Items.IsItemDefined(foundEntry.ItemId))
							{
								InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(foundEntry.ItemId);
								int num3 = Convert.ToInt32(Math.Round((float)itemDefinition.SeizeValue * foundEntry.Amount));
								menuItem2 = new MenuItem(itemDefinition.Name);
								menuItem2.Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CONFISCATE_REWARD, "~g~" + num3.ToCurrencyString() + " ~s~(" + itemDefinition.SeizeValue.ToCurrencyString() + "/" + (itemDefinition.Unit ?? "piece") + ")");
								menuItem2.Label = $"{foundEntry.Amount:0.##}{itemDefinition.Unit}";
								menuItem2.ItemData = foundEntry;
								menuItem2.PlaySelectSound = false;
								MenuItem item = menuItem2;
								searchMenu.AddMenuItem(item);
								num += num3;
								num2++;
							}
							else if (Gtacnr.Data.Items.IsWeaponDefined(foundEntry.ItemId))
							{
								WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(foundEntry.ItemId);
								int num4 = Convert.ToInt32(Math.Round((float)weaponDefinition.SeizeValue * foundEntry.Amount));
								menuItem2 = new MenuItem(weaponDefinition.Name);
								menuItem2.Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CONFISCATE_REWARD, "~g~" + num4.ToCurrencyString() + " ~s~(" + weaponDefinition.SeizeValue.ToCurrencyString() + "/" + (weaponDefinition.Unit ?? "unit") + ")");
								menuItem2.Label = $"{foundEntry.Amount:0.##}";
								menuItem2.ItemData = foundEntry;
								menuItem2.PlaySelectSound = false;
								MenuItem item2 = menuItem2;
								searchMenu.AddMenuItem(item2);
								num += num4;
								num2++;
							}
							else if (Gtacnr.Data.Items.IsAmmoDefined(foundEntry.ItemId))
							{
								AmmoDefinition ammoDefinition = Gtacnr.Data.Items.GetAmmoDefinition(foundEntry.ItemId);
								int num5 = Convert.ToInt32(Math.Round((float)ammoDefinition.SeizeValue * foundEntry.Amount));
								menuItem2 = new MenuItem(ammoDefinition.Name);
								menuItem2.Description = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CONFISCATE_REWARD, "~g~" + num5.ToCurrencyString() + " ~s~(" + ammoDefinition.SeizeValue.ToCurrencyString() + "/" + (ammoDefinition.Unit ?? "round") + ")");
								menuItem2.Label = $"{foundEntry.Amount:0.##}";
								menuItem2.ItemData = foundEntry;
								menuItem2.PlaySelectSound = false;
								MenuItem item3 = menuItem2;
								searchMenu.AddMenuItem(item3);
								num += num5;
								num2++;
							}
						}
						searchMenu.CounterPreText = LocalizationController.S(Entries.Player.MENU_COUNT_ITEMS, num2);
						Menu menu2 = searchMenu;
						menuItem2 = (menuItems["seizeAll"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CONFISCATE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CONFISCATE_DESC))
						{
							Label = "~g~" + num.ToCurrencyString()
						});
						menu2.AddMenuItem(menuItem2);
						Menu menu3 = searchMenu;
						menuItem2 = (menuItems["suggestBribe"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE), LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_DESCRIPTION)));
						menu3.AddMenuItem(menuItem2);
						searchMenu.OpenMenu();
					}
					else
					{
						switch (policeSearchResults.ResponseCode)
						{
						case PoliceSearchResponse.NothingFound:
							await BaseScript.Delay(2000);
							Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SEARCH_NO_RESULTS, targetInfo.ColorNameAndId));
							break;
						case PoliceSearchResponse.AlreadySearched:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_SEARCHED_BY_SOMEONE, targetInfo.ColorNameAndId));
							break;
						default:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x1E-{(int)policeSearchResults.ResponseCode}"));
							break;
						}
					}
				}
				else if (IsSelected("follow"))
				{
					if (!val2 || val3)
					{
						CuffScript.OrderToFollow();
					}
					else
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_IN_CUSTODY));
					}
					HideArrestMenu();
				}
				else if (IsSelected("stop"))
				{
					if (!val2 || val3)
					{
						CuffScript.OrderToStopFollowing();
					}
					else
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_IN_CUSTODY));
					}
					HideArrestMenu();
				}
				else if (IsSelected("enter"))
				{
					if (!val2 || val3)
					{
						CuffScript.OrderToEnterVehicle();
					}
					else
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_IN_CUSTODY));
					}
					HideArrestMenu();
				}
				else if (IsSelected("exit"))
				{
					if (!val2 || val3)
					{
						CuffScript.OrderToExitVehicle();
					}
					else
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUSPECT_ALREADY_IN_CUSTODY));
					}
					HideArrestMenu();
				}
				else if (IsSelected("transport"))
				{
					TransportUnitScript.CallTransport();
					HideArrestMenu();
				}
				else if (IsSelected("handover") && ManualTransportScript.IsSuspectCloseToFrontDesk(character))
				{
					ManualTransportScript.Instance.HandOver(targetServerId);
					HideArrestMenu();
				}
				else if (IsSelected("suggestBribe"))
				{
					BaseScript.TriggerServerEvent("gtacnr:police:suggestBribe", new object[1] { targetServerId });
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_YOU_ASKED_FOR_BRIBE, targetInfo.ColorNameAndId));
					menuItem.Enabled = false;
					menuItem.Label = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_REQUESTED);
				}
			}
			else
			{
				if (menu != searchMenu)
				{
					return;
				}
				if (IsSelected("seizeAll"))
				{
					BribeScript.CancelCurrentBribe();
					PoliceSeizeResponse policeSeizeResponse = (PoliceSeizeResponse)(await TriggerServerEventAsync<int>("gtacnr:police:seizeFromSuspect", new object[1] { targetServerId }));
					switch (policeSeizeResponse)
					{
					case PoliceSeizeResponse.Success:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_SEIZED_ALL, targetInfo.ColorNameAndId));
						break;
					case PoliceSeizeResponse.NotInCustody:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_NO_LONGER_IN_CUSTODY, targetInfo.ColorNameAndId));
						break;
					case PoliceSeizeResponse.NothingFound:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_SEARCHING_NO_ILLEGAL_ITEMS, targetInfo.ColorNameAndId));
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x1F-{(int)policeSeizeResponse}"));
						break;
					}
					searchMenu.ClearMenuItems();
					searchMenu.CloseMenu();
				}
				else if (IsSelected("suggestBribe"))
				{
					BaseScript.TriggerServerEvent("gtacnr:police:suggestBribe", new object[1] { targetServerId });
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_YOU_ASKED_FOR_BRIBE, targetInfo.ColorNameAndId));
					menuItem.Enabled = false;
					menuItem.Label = LocalizationController.S(Entries.Player.MENU_PLAYERMENU_SUGGEST_BRIBE_REQUESTED);
				}
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
		bool IsSelected(string menuItemKey)
		{
			if (menuItems.ContainsKey(menuItemKey))
			{
				return menuItem == menuItems[menuItemKey];
			}
			return false;
		}
	}

	private void OnMenuIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		if (menu == searchMenu)
		{
			RefreshSearchMenuInstructionalButtons();
		}
	}

	[EventHandler("gtacnr:police:bribeSuggested")]
	private async void OnBribeSuggested(int sourceId)
	{
		Ped playerPed = Game.PlayerPed;
		if (playerPed != null && !((Entity)playerPed).IsDead)
		{
			PlayerState playerState = LatentPlayers.Get(sourceId);
			if (playerState != null)
			{
				string text = ((!Utils.IsUsingKeyboard()) ? LocalizationController.S(Entries.Businesses.STP_HOLD, "~INPUT_REPLAY_SCREENSHOT~") : LocalizationController.S(Entries.Businesses.STP_PRESS, "~INPUT_MP_TEXT_CHAT_TEAM~"));
				string text2 = text;
				string message = LocalizationController.S(Entries.Jobs.POLICE_ASKED_TO_BRIBE, playerState.ColorNameAndId) + " " + LocalizationController.S(Entries.Jobs.POLICE_BRIBE_MENU_ACTION, text2);
				string text3 = LocalizationController.S(Entries.Jobs.POLICE_TICKET_BRIBE).Replace("~o~", "");
				await InteractiveNotificationsScript.Show(message, InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, text3, LocalizationController.S(Entries.Businesses.BTN_STP_LABEL_HOLD, text3));
			}
		}
		bool OnAccepted()
		{
			Utils.DisplayHelpText();
			PlayerMenuScript.OpenBribeMenu(sourceId);
			return true;
		}
	}

	[EventHandler("gtacnr:arrested")]
	private void OnPlayerArrested(int officerId, int suspectId, byte wantedLevel)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (lastSearchedSuspect == suspectId)
		{
			lastSearchedSuspect = 0;
		}
		PlayerState playerState = LatentPlayers.Get(officerId);
		PlayerState playerState2 = LatentPlayers.Get(suspectId);
		switch (DeathScript.DeathFeedMode)
		{
		case DeathFeedMode.Off:
			return;
		case DeathFeedMode.Proximity:
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(playerState2.Position) > 40000f)
			{
				return;
			}
			break;
		}
		}
		Utils.SendNotification(LocalizationController.S(Entries.Jobs.POLICE_ARREST_NOTIFICATION, playerState.FullyFormatted, playerState2.FullyFormatted));
	}
}
