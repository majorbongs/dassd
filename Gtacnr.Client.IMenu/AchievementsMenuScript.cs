using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitizenFX.Core;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class AchievementsMenuScript : Script
{
	private static Menu MainMenu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS));

	private static Dictionary<string, MenuItem> AchievementItems = new Dictionary<string, MenuItem>();

	private static Dictionary<string, ulong> PlayerProgress = new Dictionary<string, ulong>();

	private static Dictionary<string, ulong> UnlockedTiers = new Dictionary<string, ulong>();

	private static DateTime lastRefreshT = DateTime.MinValue;

	public AchievementsMenuScript()
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
		MenuController.BindMenuItem(MainMenuScript.StatsAndTasksMenu, MainMenu, MainMenuScript.MainMenuItems["achievements"]);
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		await RefreshData();
	}

	private async Coroutine<bool> RefreshData()
	{
		lastRefreshT = DateTime.UtcNow;
		string text = await TriggerServerEventAsync<string>("gtacnr:achievements:getAchievements", new object[0]);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		AchievementResponse achievementResponse = text.Unjson<AchievementResponse>();
		if (achievementResponse == null)
		{
			return false;
		}
		PlayerProgress = achievementResponse.Progress;
		UnlockedTiers = achievementResponse.UnlockedTiers;
		MainMenu.ClearMenuItems();
		AchievementItems.Clear();
		foreach (Achievement allAchievementDefinition in Achievements.GetAllAchievementDefinitions())
		{
			ulong value;
			ulong num = (PlayerProgress.TryGetValue(allAchievementDefinition.Id, out value) ? value : 0);
			ulong value2;
			ulong unlockedTier = (UnlockedTiers.TryGetValue(allAchievementDefinition.Id, out value2) ? value2 : 0);
			ulong num2 = allAchievementDefinition.Tiers.Keys.FirstOrDefault((ulong k) => k > unlockedTier);
			if (!allAchievementDefinition.IsSecret || unlockedTier != 0)
			{
				string label = ((num2 != 0) ? $"{num}/{num2}" : LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_COMPLETED));
				MenuItem menuItem = new MenuItem((allAchievementDefinition.IsSecret ? "~p~" : "") + Gtacnr.Utils.ResolveLocalization(allAchievementDefinition.Name), (allAchievementDefinition.IsSecret ? ("~p~" + LocalizationController.S(Entries.Imenu.SECRET_ACHIEVEMENT) + "~s~\n") : "") + GetTierDescriptionWithReward(allAchievementDefinition, unlockedTier, num2).Trim())
				{
					Label = label
				};
				if (num2 == 0L)
				{
					menuItem.RightIcon = MenuItem.Icon.TICK;
					menuItem.Enabled = false;
				}
				AchievementItems.Add(allAchievementDefinition.Id, menuItem);
				MainMenu.AddMenuItem(menuItem);
			}
		}
		return true;
	}

	private string GetTierDescriptionWithReward(Achievement def, ulong unlockedTier, ulong nextTier)
	{
		AchievementTier value;
		if (nextTier != 0)
		{
			def.Tiers.TryGetValue(nextTier, out value);
		}
		else
		{
			def.Tiers.TryGetValue(unlockedTier, out value);
		}
		if (value == null)
		{
			return LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_NO_PROGRESS);
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Gtacnr.Utils.ResolveLocalization(value.Description));
		stringBuilder.AppendLine(LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_REWARDS));
		bool flag = false;
		if (value.Reward.Money > 0)
		{
			stringBuilder.AppendLine("- ~g~" + value.Reward.Money.ToCurrencyString() + "~s~");
			flag = true;
		}
		if (value.Reward.XP > 0)
		{
			stringBuilder.AppendLine($"- ~b~{value.Reward.XP} XP~s~");
			flag = true;
		}
		if (value.Reward.Clothes != null && value.Reward.Clothes.Count > 0)
		{
			List<string> values = (from c in value.Reward.Clothes.Select(Gtacnr.Data.Items.GetClothingItemDefinition)
				where c != null
				select c.Name).ToList();
			stringBuilder.AppendLine("- " + LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_CLOTHES, string.Join(", ", values)));
			flag = true;
		}
		if (!flag)
		{
			stringBuilder.Append(LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_REWARDS_NONE));
		}
		return stringBuilder.ToString().Replace("\r", "");
	}

	[EventHandler("gtacnr:achievements:progressUpdate")]
	private void OnProgressUpdate(string achievementId, ulong progress)
	{
		if (!AchievementItems.TryGetValue(achievementId, out MenuItem value))
		{
			return;
		}
		Achievement achievementDefinition = Achievements.GetAchievementDefinition(achievementId);
		if (achievementDefinition != null)
		{
			ulong value2;
			ulong unlockedTier = (UnlockedTiers.TryGetValue(achievementId, out value2) ? value2 : 0);
			ulong num = achievementDefinition.Tiers.Keys.FirstOrDefault((ulong k) => k > unlockedTier);
			value.Label = ((num != 0) ? $"{progress}/{num}" : LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_COMPLETED));
			value.Description = GetTierDescriptionWithReward(achievementDefinition, unlockedTier, num);
			if (num == 0L)
			{
				value.RightIcon = MenuItem.Icon.TICK;
				value.Enabled = false;
			}
		}
	}

	[EventHandler("gtacnr:achievements:tierUnlocked")]
	private void OnTierUnlocked(string achievementId, ulong tier)
	{
		UnlockedTiers[achievementId] = tier;
		Achievement achievementDefinition = Achievements.GetAchievementDefinition(achievementId);
		if (achievementDefinition != null && achievementDefinition.Tiers.TryGetValue(tier, out AchievementTier value))
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.NOTIFICATION_ACHIEVEMENT_UNLOCKED, Gtacnr.Utils.ResolveLocalization(achievementDefinition.Name), Gtacnr.Utils.ResolveLocalization(value.Description)));
			Utils.DisplaySubtitle(achievementDefinition.IsSecret ? LocalizationController.S(Entries.Imenu.SUBTITLE_SECRET_ACHIEVEMENT_UNLOCKED) : LocalizationController.S(Entries.Imenu.SUBTITLE_ACHIEVEMENT_UNLOCKED), 2000);
			Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
		}
	}
}
