using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class DailyChallengesMenuScript : Script
{
	private static Menu MainMenu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_DAILY_CHALLENGES));

	private static Dictionary<string, MenuItem> ChallengeItems = new Dictionary<string, MenuItem>();

	private static Dictionary<string, DailyChallengeEntry> dailyChallenges = new Dictionary<string, DailyChallengeEntry>();

	private static DateTime lastRefreshT = DateTime.MinValue;

	public DailyChallengesMenuScript()
	{
		MainMenu.InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)166, Menu.ControlPressCheckType.JUST_PRESSED, async delegate
		{
			if (!Gtacnr.Utils.CheckTimePassed(lastRefreshT, 30000.0))
			{
				Utils.PlayErrorSound();
			}
			else
			{
				await RefreshData();
			}
		}, disableControl: true));
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		MenuController.AddSubmenu(MainMenuScript.StatsAndTasksMenu, MainMenu);
		MenuController.BindMenuItem(MainMenuScript.StatsAndTasksMenu, MainMenu, MainMenuScript.MainMenuItems["dailyChallenges"]);
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		await RefreshData();
	}

	private async Coroutine<bool> RefreshData()
	{
		lastRefreshT = DateTime.UtcNow;
		string text = await TriggerServerEventAsync<string>("gtacnr:dailyChallenges:getChallenges", new object[0]);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		DailyChallengeResponse dailyChallengeResponse = text.Unjson<DailyChallengeResponse>();
		if (dailyChallengeResponse == null)
		{
			return false;
		}
		MainMenu.ClearMenuItems();
		ChallengeItems.Clear();
		dailyChallenges = dailyChallengeResponse.DailyChallengeEntries.ToDictionary((DailyChallengeEntry d) => d.Id, (DailyChallengeEntry d) => d);
		foreach (DailyChallengeEntry dailyChallengeEntry in dailyChallengeResponse.DailyChallengeEntries)
		{
			DailyChallenge challengeDefinition = DailyChallenges.GetChallengeDefinition(dailyChallengeEntry.Id);
			if (challengeDefinition != null)
			{
				uint val = 0u;
				if (dailyChallengeResponse.PlayerProgress.ContainsKey(dailyChallengeEntry.Id))
				{
					val = dailyChallengeResponse.PlayerProgress[dailyChallengeEntry.Id];
				}
				val = Math.Min(val, dailyChallengeEntry.PointsNeeded);
				MenuItem menuItem = new MenuItem(LocalizationController.S(challengeDefinition.Name) ?? "", challengeDefinition.GetLocalizedDescriptionString(dailyChallengeEntry.PointsNeeded))
				{
					Label = $"{val}/{dailyChallengeEntry.PointsNeeded}"
				};
				if (val == dailyChallengeEntry.PointsNeeded)
				{
					menuItem.RightIcon = MenuItem.Icon.TICK;
					menuItem.Enabled = false;
				}
				ChallengeItems.Add(dailyChallengeEntry.Id, menuItem);
				MainMenu.AddMenuItem(menuItem);
			}
		}
		return true;
	}

	[EventHandler("gtacnr:dailyChallenges:progressUpdate")]
	private void OnProgressUpdate(string challengeId, uint progress)
	{
		if (!ChallengeItems.TryGetValue(challengeId, out MenuItem value) || !dailyChallenges.TryGetValue(challengeId, out DailyChallengeEntry value2))
		{
			return;
		}
		progress = Math.Min(progress, value2.PointsNeeded);
		value.Label = $"{progress}/{value2.PointsNeeded}";
		if (progress == value2.PointsNeeded)
		{
			value.RightIcon = MenuItem.Icon.TICK;
			value.Enabled = false;
			DailyChallenge challengeDefinition = DailyChallenges.GetChallengeDefinition(challengeId);
			if (challengeDefinition != null)
			{
				Utils.SendNotification("You've completed one of your ~b~daily challenges~s~: " + challengeDefinition.GetLocalizedDescriptionString(value2.PointsNeeded));
				Utils.DisplaySubtitle("~b~Daily Challenge Completed!", 2000);
				Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
			}
		}
	}
}
