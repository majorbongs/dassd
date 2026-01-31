using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class ReportMenuScript : Script
{
	private readonly TimeSpan REPORT_COOLDOWN = TimeSpan.FromMinutes(10.0);

	public static Menu ReportMenu;

	public static Menu MyReportsMenu;

	private static ReportMenuScript instance;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private int targetPlayerId;

	private List<Report> userReports = new List<Report>();

	private Dictionary<int, DateTime> timestamps = new Dictionary<int, DateTime>();

	public ReportMenuScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		Chat.AddSuggestion("/report", LocalizationController.S(Entries.Player.REPORT_CMD_SUGGESTION_HELP), new ChatParamSuggestion(LocalizationController.S(Entries.Player.REPORT_CMD_SUGGESTION_PARAM_NAME), LocalizationController.S(Entries.Player.REPORT_CMD_SUGGESTION_PARAM_HELP)));
		ReportMenu = new Menu(LocalizationController.S(Entries.Player.MENU_REPORT_TITLE), LocalizationController.S(Entries.Player.MENU_REPORT_SUBTITLE))
		{
			CloseWhenDead = false
		};
		Menu reportMenu = ReportMenu;
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem("Deathmatching (RDM)", "This player ~r~attacked ~s~and/or ~r~killed ~s~me or someone else for no valid reason.~n~~y~Examples: ~s~cop killing innocents, cops or paramedics, player randomly killing all cops on sight, player randomly killing players at the hospitals.")
		{
			ItemData = ReportReason.RDM
		};
		MenuItem item = obj;
		dictionary["rdm"] = obj;
		reportMenu.AddMenuItem(item);
		Menu reportMenu2 = ReportMenu;
		Dictionary<string, MenuItem> dictionary2 = menuItems;
		MenuItem obj2 = new MenuItem("Cross Teaming", "This player is ~r~teaming up ~s~with an ~r~opponent~s~.")
		{
			ItemData = ReportReason.CrossTeaming
		};
		item = obj2;
		dictionary2["crossteam"] = obj2;
		reportMenu2.AddMenuItem(item);
		Menu reportMenu3 = ReportMenu;
		Dictionary<string, MenuItem> dictionary3 = menuItems;
		MenuItem obj3 = new MenuItem("Cheating", "This player is a ~r~modder ~s~or abusing ~r~glitches~s~.")
		{
			ItemData = ReportReason.Cheating
		};
		item = obj3;
		dictionary3["cheat"] = obj3;
		reportMenu3.AddMenuItem(item);
		Menu reportMenu4 = ReportMenu;
		Dictionary<string, MenuItem> dictionary4 = menuItems;
		MenuItem obj4 = new MenuItem("Quitting", "This player ~r~quit ~s~the game mid-combat or arrest.")
		{
			ItemData = ReportReason.Quitting
		};
		item = obj4;
		dictionary4["quit"] = obj4;
		reportMenu4.AddMenuItem(item);
		Menu reportMenu5 = ReportMenu;
		Dictionary<string, MenuItem> dictionary5 = menuItems;
		MenuItem obj5 = new MenuItem("Spamming", "This player is ~r~advertising ~s~another server, website or product, or otherwise being noisy or flooding the chat.")
		{
			ItemData = ReportReason.Spamming
		};
		item = obj5;
		dictionary5["spam"] = obj5;
		reportMenu5.AddMenuItem(item);
		Menu reportMenu6 = ReportMenu;
		Dictionary<string, MenuItem> dictionary6 = menuItems;
		MenuItem obj6 = new MenuItem("~r~Hate speech", "This player is using ~r~racist ~s~or ~r~homophobic ~s~language, or another kind of ~r~hate speech~s~.")
		{
			ItemData = ReportReason.HateSpeech
		};
		item = obj6;
		dictionary6["hatespeech"] = obj6;
		reportMenu6.AddMenuItem(item);
		Menu reportMenu7 = ReportMenu;
		Dictionary<string, MenuItem> dictionary7 = menuItems;
		MenuItem obj7 = new MenuItem("~r~Sexual harassment", "This player is ~r~verbally harassing~s~ someone in a sexual manner.~n~~y~Warning: ~s~this is a serious allegation, it's better to report these cases on Discord with solid evidence.")
		{
			ItemData = ReportReason.Harassment
		};
		item = obj7;
		dictionary7["harassment"] = obj7;
		reportMenu7.AddMenuItem(item);
		Menu reportMenu8 = ReportMenu;
		Dictionary<string, MenuItem> dictionary8 = menuItems;
		MenuItem obj8 = new MenuItem("Other", "This player is breaking another rule or term of service.")
		{
			ItemData = ReportReason.Other
		};
		item = obj8;
		dictionary8["other"] = obj8;
		reportMenu8.AddMenuItem(item);
		ReportMenu.OnItemSelect += OnSelect;
		MyReportsMenu = new Menu(LocalizationController.S(Entries.Player.MENU_REPORTS_TITLE), LocalizationController.S(Entries.Player.MENU_REPORTS_SUBTITLE))
		{
			CloseWhenDead = false
		};
	}

	private async void OnSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		ReportMenu.CloseMenu();
		string input = await Utils.GetUserInput(LocalizationController.S(Entries.Player.MENU_REPORT_TITLE), "Details (10-300 characters).", "", 300);
		if (input == null || input.Length < 10)
		{
			Utils.PlayErrorSound();
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_NOT_SUBMITTED));
			return;
		}
		PlayerState playerInfo = LatentPlayers.Get(targetPlayerId);
		ReportReason reason = (ReportReason)menuItem.ItemData;
		if (await Utils.ShowConfirm(LocalizationController.S(Entries.Player.INPUT_REPORT_WARNING, playerInfo.NameAndId, Gtacnr.Utils.GetDescription(reason))))
		{
			bool success = await TriggerServerEventAsync<bool>("gtacnr:submitReport", new object[3]
			{
				targetPlayerId,
				(int)reason,
				input.Trim()
			});
			await BaseScript.Delay(500);
			if (success)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.REPORT_SUBMITTED, playerInfo.Name));
				timestamps[playerInfo.Id] = DateTime.UtcNow;
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			}
		}
	}

	public static void OpenReportMenu(int targetId)
	{
		instance.targetPlayerId = targetId;
		instance.OpenReportMenuInternal();
	}

	private void OpenReportMenuInternal()
	{
		PlayerState playerState = LatentPlayers.Get(targetPlayerId);
		if (playerState != null)
		{
			if (!timestamps.ContainsKey(playerState.Id))
			{
				timestamps[playerState.Id] = default(DateTime);
			}
			if (!Gtacnr.Utils.CheckTimePassed(timestamps[playerState.Id], REPORT_COOLDOWN))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.REPORT_CANT_REPORT_PLAYER_THAT_SOON));
				Utils.PlayErrorSound();
			}
			else
			{
				ReportMenu.OpenMenu();
				ReportMenu.MenuSubtitle = LocalizationController.S(Entries.Player.MENU_REPORT_REPORTING_SUBTITLE, playerState.NameAndId);
			}
		}
	}

	public static void OpenMyReportsMenu()
	{
		MenuController.CloseAllMenus();
		instance.OpenMyReportsMenuInternal();
	}

	private async void OpenMyReportsMenuInternal()
	{
		MyReportsMenu.OpenMenu();
		MyReportsMenu.ClearMenuItems();
		MyReportsMenu.AddLoadingMenuItem();
		string text = await TriggerServerEventAsync<string>("gtacnr:fetchMyReports", new object[0]);
		userReports.Clear();
		if (!string.IsNullOrEmpty(text))
		{
			userReports = text.Unjson<List<Report>>();
		}
		MyReportsMenu.ClearMenuItems();
		foreach (Report userReport in userReports)
		{
			MenuItem menuItem = new MenuItem(userReport.ReportedUserName);
			menuItem.Description = LocalizationController.S(Entries.Player.MENU_REPORTS_INFO_DESCRIPTION, userReport.DateTime.ToFormalDateTime(), Gtacnr.Utils.GetDescription(userReport.Reason), userReport.Details);
			menuItem.Label = ((userReport.State >= ReportState.Assigned) ? (Gtacnr.Utils.GetDescription(userReport.State) ?? "") : "");
			MenuItem menuItem2 = menuItem;
			if (userReport.State >= ReportState.Assigned)
			{
				if (userReport.State >= ReportState.Solved)
				{
					menuItem2.Description = menuItem2.Description + "\n" + LocalizationController.S(Entries.Player.MENU_REPORTS_INFO_RESPONSE, userReport.ResponderUserName, userReport.ClosingResponse);
				}
				else
				{
					menuItem2.Description = menuItem2.Description + "\n" + LocalizationController.S(Entries.Player.MENU_REPORTS_INFO_ASSIGNED, userReport.ResponderUserName);
				}
			}
			MyReportsMenu.AddMenuItem(menuItem2);
		}
		if (userReports.Count == 0)
		{
			MyReportsMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Player.MENU_REPORTS_INFO_NO_RECENT_TEXT), LocalizationController.S(Entries.Player.MENU_REPORTS_INFO_NO_RECENT_DESCRIPTION)));
		}
		MyReportsMenu.MenuSubtitle = LocalizationController.S(Entries.Player.MENU_REPORTS_INFO_SUBTITLE, userReports.Count);
	}

	[Command("report")]
	private void ReportCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, LocalizationController.S(Entries.Main.CMD_USAGE, "[" + Entries.Player.REPORT_CMD_SUGGESTION_PARAM_NAME + "]"));
			Utils.PlayErrorSound();
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, Gtacnr.Utils.RemoveGta5TextFormatting(LocalizationController.S(Entries.Imenu.IMENU_HELP_INVALID_ID)));
			Utils.PlayErrorSound();
			return;
		}
		PlayerState playerState = LatentPlayers.Get(result);
		if (playerState == null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, Gtacnr.Utils.RemoveGta5TextFormatting(LocalizationController.S(Entries.Imenu.IMENU_HELP_PLAYER_NOT_CONNECTED)));
			Utils.PlayErrorSound();
			return;
		}
		if (!timestamps.ContainsKey(playerState.Id))
		{
			timestamps[playerState.Id] = default(DateTime);
		}
		if (!Gtacnr.Utils.CheckTimePassed(timestamps[playerState.Id], REPORT_COOLDOWN))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, Gtacnr.Utils.RemoveGta5TextFormatting(LocalizationController.S(Entries.Player.REPORT_CANT_REPORT_PLAYER_THAT_SOON)));
			Utils.PlayErrorSound();
		}
		else
		{
			targetPlayerId = playerState.Id;
			OpenReportMenuInternal();
		}
	}
}
