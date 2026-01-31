using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Communication;

public class PartyScript : Script
{
	private RadioChannel partyChannel;

	private Menu partyMenu;

	private readonly Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static List<int> partyMembers = new List<int>();

	private static PartyScript instance;

	public static ReadOnlyCollection<int> PartyMembers => partyMembers.AsReadOnly();

	public static int PartyLeader { get; private set; }

	public static bool IsInParty { get; private set; }

	public static Menu Menu => instance.partyMenu;

	public PartyScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		AddSuggestions();
		CreatePartyMenu();
	}

	private void AddSuggestions()
	{
		Chat.AddSuggestion("/party", LocalizationController.S(Entries.Main.PARTY_DESC));
		Chat.AddSuggestion(new string[2] { "/party-create", "/pcreate" }, LocalizationController.S(Entries.Main.PARTY_CREATE_DESC));
		Chat.AddSuggestion(new string[2] { "/party-invite", "/pinvite" }, LocalizationController.S(Entries.Main.PARTY_INVITE_DESC), new ChatParamSuggestion("player", LocalizationController.S(Entries.Main.PARTY_INVITE_PARAM_PLAYER_DESC)));
		Chat.AddSuggestion(new string[2] { "/party-join", "/pjoin" }, LocalizationController.S(Entries.Main.PARTY_JOIN_DESC), new ChatParamSuggestion("player", LocalizationController.S(Entries.Main.PARTY_JOIN_PARAM_PLAYER_DESC)));
		Chat.AddSuggestion(new string[2] { "/party-leave", "/pleave" }, LocalizationController.S(Entries.Main.PARTY_LEAVE_DESC));
		Chat.AddSuggestion(new string[2] { "/party-kick", "/pkick" }, LocalizationController.S(Entries.Main.PARTY_KICK_DESC), new ChatParamSuggestion("player", LocalizationController.S(Entries.Main.PARTY_KICK_PARAM_PLAYER_DESC)));
		Chat.AddSuggestion(new string[2] { "/party-list", "/plist" }, LocalizationController.S(Entries.Main.PARTY_LIST_DESC));
		Chat.AddSuggestion(new string[2] { "/party-leader", "/pleader" }, LocalizationController.S(Entries.Main.PARTY_LEADER_DESC), new ChatParamSuggestion("player", LocalizationController.S(Entries.Main.PARTY_LEADER_PARAM_PLAYER_DESC)));
		Chat.AddSuggestion("/party-sendgps", LocalizationController.S(Entries.Main.PARTY_SENDGPS_DESC));
		Chat.AddSuggestion("/party-sendwaypoint", LocalizationController.S(Entries.Main.PARTY_SENDWAYPOINT_DESC));
	}

	[EventHandler("gtacnr:partyJoined")]
	private void OnPartyJoined(int partyIdx, string jPlayers, int leader)
	{
		List<int> collection = jPlayers.Unjson<List<int>>();
		if (partyChannel != null)
		{
			RadioScript.RemoveChannel(partyChannel);
		}
		partyChannel = new RadioChannel
		{
			Frequency = 500 + partyIdx,
			DisplayName = "Party",
			Description = "Communicate with party members."
		};
		RadioScript.AddChannel(partyChannel);
		RadioScript.RefreshChannels();
		RadioScript.ToggleRadio(on: true);
		RadioScript.SetChannel(partyChannel.Frequency);
		partyMembers.Clear();
		partyMembers.AddRange(collection);
		PartyLeader = leader;
		IsInParty = true;
	}

	[EventHandler("gtacnr:partyLeft")]
	private void OnPartyLeft()
	{
		IsInParty = false;
		if (partyChannel != null)
		{
			RadioScript.RemoveChannel(partyChannel);
			RadioScript.RefreshChannels();
			if (RadioScript.AvailableChannels.Count() > 0)
			{
				RadioScript.SetChannel(RadioScript.AvailableChannels.FirstOrDefault()?.Frequency ?? 0f);
			}
			else
			{
				RadioScript.ToggleRadio(on: false);
			}
			partyMembers.Clear();
		}
	}

	[EventHandler("gtacnr:playerJoinedParty")]
	private void OnPlayerJoinedParty(int playerId)
	{
		partyMembers.Add(playerId);
	}

	[EventHandler("gtacnr:playerLeftParty")]
	private void OnPlayerLeftParty(int playerId)
	{
		partyMembers.Remove(playerId);
	}

	[EventHandler("gtacnr:partyLeaderChanged")]
	private void OnPartyLeaderChanged(int oldLeader, int newLeader)
	{
		PartyLeader = newLeader;
	}

	[EventHandler("gtacnr:party:gotInvited")]
	private async void OnGotInvited(int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			string message = LocalizationController.S(Entries.Main.PARTY_INVITE_REQUEST_INCOMING, playerState.ColorNameAndId, playerId);
			Func<bool> onAccept = OnAccepted;
			string keyboardMessage = LocalizationController.S(Entries.Main.PARTY_INVITE_ACCEPT_INVITE);
			string controllerMessage = LocalizationController.S(Entries.Main.PARTY_INVITE_ACCEPT_INVITE_HOLD);
			await InteractiveNotificationsScript.Show(message, InteractiveNotificationType.HelpText, onAccept, null, 0u, keyboardMessage, controllerMessage);
		}
		bool OnAccepted()
		{
			API.ExecuteCommand($"pjoin {playerId}");
			return true;
		}
	}

	[Command("party")]
	private void PartyCommand()
	{
		_OpenPartyMenu();
	}

	private bool GPSCommandCheck(out List<int>? otherMembers)
	{
		otherMembers = null;
		if (!IsInParty)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.PARTY_NOT_IN_PARTY));
			return false;
		}
		otherMembers = new List<int>(partyMembers);
		otherMembers.Remove(Game.Player.ServerId);
		if (otherMembers.Count == 0)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.PARTY_NO_OTHER_MEMBERS));
			return false;
		}
		return true;
	}

	[Command("party-sendgps")]
	private void PartySendGPSCommand()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (GPSCommandCheck(out List<int> otherMembers))
		{
			BaseScript.TriggerServerEvent("gtacnr:shareGPS", new object[3]
			{
				otherMembers.Json(),
				(byte)0,
				(object)default(Vector3)
			});
		}
	}

	[Command("party-sendwaypoint")]
	private void PartySendWaypointCommand()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (GPSCommandCheck(out List<int> otherMembers))
		{
			int firstBlipInfoId = API.GetFirstBlipInfoId(8);
			if (!API.DoesBlipExist(firstBlipInfoId))
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.SENDWAYPOINT_NOT_SELECTED));
				return;
			}
			BaseScript.TriggerServerEvent("gtacnr:shareGPS", new object[3]
			{
				otherMembers.Json(),
				(byte)1,
				API.GetBlipInfoIdCoord(firstBlipInfoId)
			});
		}
	}

	private async void CreatePartyMenu()
	{
		partyMenu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_PARTY), LocalizationController.S(Entries.Imenu.IMENU_PARTY_DESCR));
		partyMenu.OnItemSelect += OnItemSelect;
	}

	public static void OpenPartyMenu()
	{
		instance._OpenPartyMenu();
	}

	private void _OpenPartyMenu()
	{
		MenuController.CloseAllMenus();
		partyMenu.OpenMenu();
		Utils.PlaySelectSound();
	}

	public static void RefreshPartyMenu()
	{
		instance._RefreshPartyMenu();
	}

	private void _RefreshPartyMenu()
	{
		partyMenu.ClearMenuItems();
		if (IsInParty)
		{
			string label = LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LIST_LABEL, partyMembers.Count);
			Menu menu = partyMenu;
			Dictionary<string, MenuItem> dictionary = menuItems;
			MenuItem obj = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LIST), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LIST_DESC))
			{
				Label = label
			};
			MenuItem item = obj;
			dictionary["plist"] = obj;
			menu.AddMenuItem(item);
			Menu menu2 = partyMenu;
			item = (menuItems["pinvite"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_INVITE), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_INVITE_DESC)));
			menu2.AddMenuItem(item);
			Menu menu3 = partyMenu;
			item = (menuItems["psendgps"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_SENDGPS), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_SENDGPS_DESC)));
			menu3.AddMenuItem(item);
			Menu menu4 = partyMenu;
			item = (menuItems["psendwp"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_SENDWP), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_SENDWP_DESC)));
			menu4.AddMenuItem(item);
			if (PartyLeader == Game.Player.ServerId)
			{
				Menu menu5 = partyMenu;
				item = (menuItems["pkick"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_KICK), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_KICK_DESC)));
				menu5.AddMenuItem(item);
				Menu menu6 = partyMenu;
				item = (menuItems["ptransfer"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LEADER), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LEADER_DESC)));
				menu6.AddMenuItem(item);
			}
			Menu menu7 = partyMenu;
			item = (menuItems["pleave"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LEAVE), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LEAVE_DESC)));
			menu7.AddMenuItem(item);
		}
		else
		{
			Menu menu8 = partyMenu;
			MenuItem item = (menuItems["pcreate"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_CREATE), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_CREATE_DESC)));
			menu8.AddMenuItem(item);
			Menu menu9 = partyMenu;
			item = (menuItems["pjoin"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_JOIN), LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_JOIN_DESC)));
			menu9.AddMenuItem(item);
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (IsSelected("pcreate"))
		{
			API.ExecuteCommand("pcreate");
			menuItem.Enabled = false;
			menuItem.Label = "...";
			await BaseScript.Delay(500);
			RefreshPartyMenu();
		}
		else
		{
			if (IsSelected("pjoin"))
			{
				return;
			}
			if (IsSelected("plist"))
			{
				IOrderedEnumerable<PlayerState> players = from m in LatentPlayers.Get((IEnumerable<int>)partyMembers)
					orderby (m.Id != Game.Player.ServerId) ? ((m.Id == PartyLeader) ? 1 : 0) : 2 descending
					select m;
				string menuSubtitle = LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LIST_LABEL, partyMembers.Count);
				PlayerListMenu.ShowMenu(partyMenu, players, null, delegate(MenuItem menuItem2, int playerId)
				{
					menuItem2.RightIcon = ((playerId == PartyLeader) ? MenuItem.Icon.CROWN : MenuItem.Icon.NONE);
				}, exceptMe: false, null, menuSubtitle);
			}
			else if (IsSelected("pinvite"))
			{
				IOrderedEnumerable<PlayerState> players2 = from p in LatentPlayers.All
					where !partyMembers.Contains(p.Id)
					orderby p.Id
					select p;
				PlayerListMenu.ShowMenu(partyMenu, players2, delegate(Menu menu2, int playerId)
				{
					MenuItem menuItem2 = menu2.GetMenuItems().FirstOrDefault((MenuItem i) => (int)i.ItemData == playerId);
					menuItem2.Enabled = false;
					menuItem2.RightIcon = MenuItem.Icon.TICK;
					API.ExecuteCommand($"pinvite {playerId}");
				});
			}
			else if (IsSelected("pleave"))
			{
				API.ExecuteCommand("pleave");
				menuItem.Enabled = false;
				menuItem.Label = "...";
				await BaseScript.Delay(500);
				RefreshPartyMenu();
			}
			else if (IsSelected("psendgps"))
			{
				PartySendGPSCommand();
			}
			else if (IsSelected("psendwp"))
			{
				PartySendWaypointCommand();
			}
			else if (IsSelected("pkick"))
			{
				IEnumerable<PlayerState> players3 = from ps in LatentPlayers.Get((IEnumerable<int>)partyMembers)
					where ps.Id != PartyLeader
					select ps;
				string menuSubtitle2 = LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LIST_LABEL, partyMembers.Count);
				PlayerListMenu.ShowMenu(partyMenu, players3, async delegate(Menu menu2, int playerId)
				{
					MenuItem menuItem2 = menu2.GetMenuItems().FirstOrDefault((MenuItem i) => (int)i.ItemData == playerId);
					menuItem2.Enabled = false;
					menuItem2.RightIcon = MenuItem.Icon.TICK;
					API.ExecuteCommand($"pkick {playerId}");
					await BaseScript.Delay(500);
					RefreshPartyMenu();
				}, null, exceptMe: false, null, menuSubtitle2);
			}
			else
			{
				if (!IsSelected("ptransfer"))
				{
					return;
				}
				IEnumerable<PlayerState> players4 = from ps in LatentPlayers.Get((IEnumerable<int>)partyMembers)
					where ps.Id != PartyLeader
					select ps;
				string menuSubtitle3 = LocalizationController.S(Entries.Imenu.IMENU_PHONE_PARTY_LIST_LABEL, partyMembers.Count);
				PlayerListMenu.ShowMenu(partyMenu, players4, async delegate(Menu menu2, int playerId)
				{
					MenuItem menuItem2 = menu2.GetMenuItems().FirstOrDefault((MenuItem i) => (int)i.ItemData == playerId);
					menuItem2.Enabled = false;
					menuItem2.RightIcon = MenuItem.Icon.CROWN;
					API.ExecuteCommand($"pleader {playerId}");
					await BaseScript.Delay(500);
					RefreshPartyMenu();
				}, null, exceptMe: false, null, menuSubtitle3);
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
}
