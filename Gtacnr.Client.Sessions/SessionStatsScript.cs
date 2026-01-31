using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Model;
using ScaleformUI.Elements;
using ScaleformUI.LobbyMenu;
using ScaleformUI.Menu;
using ScaleformUI.PauseMenus.Elements;
using ScaleformUI.PauseMenus.Elements.Columns;
using ScaleformUI.PauseMenus.Elements.Items;

namespace Gtacnr.Client.Sessions;

public class SessionStatsScript : Script
{
	private class SessionStatInfo
	{
		public string Id { get; set; }

		public string Label { get; set; }

		public string Description { get; set; }

		public bool IsCurrency { get; set; }
	}

	private MainView mainView;

	private Dictionary<string, Character> characterData = new Dictionary<string, Character>();

	private readonly List<SessionStatInfo> sessionStatInfo = Gtacnr.Utils.LoadJson<List<SessionStatInfo>>("data/sessionStats.json");

	public static bool IsRunning { get; private set; }

	[EventHandler("gtacnr:displaySessionStats")]
	private async void DisplaySessionStats(string jSessionStats, string jUsernameDictionary, string jLevelDictionary)
	{
		IsRunning = true;
		API.DisplayHud(false);
		API.DisplayRadar(false);
		await Utils.SwitchOut();
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		AntiTeleportScript.JustTeleported();
		((Entity)Game.PlayerPed).Position = new Vector3(position.X, position.Y, 1250f);
		((Entity)Game.PlayerPed).IsPositionFrozen = true;
		ShowWeekEndStatsMenu(jSessionStats.Unjson<Dictionary<string, Dictionary<string, long>>>(), jUsernameDictionary.Unjson<Dictionary<string, string>>(), jLevelDictionary.Unjson<Dictionary<string, int>>());
	}

	[EventHandler("gtacnr:restart:fadeOut")]
	private async void OnFadeOut()
	{
		LoadingPrompt.Show("Restarting", (LoadingSpinnerType)5);
		await Utils.FadeOut(2000);
	}

	private async Task ShowWeekEndStatsMenu(Dictionary<string, Dictionary<string, long>> sessionStats, Dictionary<string, string> usernameDictionary, Dictionary<string, int> levelDictionary)
	{
		if (mainView != null)
		{
			Debug.WriteLine("The main view already exists.");
			return;
		}
		LatentPlayers.Get(Game.Player);
		CountKDR(sessionStats);
		mainView = new MainView("Session Stats", "Thanks for playing Cops and Robbers V. Join our discord at discord.gg/cnr.~n~Leaderboard powered by ScaleformUI.", newStyle: false);
		mainView.CanPlayerCloseMenu = false;
		mainView.InstructionalButtons.Clear();
		KeysScript.AttachListener((Control)188, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)187, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)180, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)181, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)189, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)190, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)202, OnKeyEvent, 10000);
		KeysScript.AttachListener((Control)204, OnKeyEvent, 10000);
		List<Column> upColumns = new List<Column>
		{
			new SettingsListColumn("STATS", SColor.HUD_Blue),
			new PlayerListColumn("LEADERBOARD", SColor.HUD_Blue)
		};
		mainView.SetUpColumns(upColumns);
		int mugshot = API.RegisterPedheadshot(((PoolObject)Game.PlayerPed).Handle);
		while (!API.IsPedheadshotReady(mugshot))
		{
			await BaseScript.Delay(1);
		}
		string pedheadshotTxdString = API.GetPedheadshotTxdString(mugshot);
		mainView.HeaderPicture = Tuple.Create(pedheadshotTxdString, pedheadshotTxdString);
		List<SColor> colors = new List<SColor>
		{
			SColor.HUD_Gold,
			SColor.HUD_Silver,
			SColor.HUD_Bronze
		};
		List<SColor> list = new List<SColor>
		{
			SColor.HUD_Red,
			SColor.HUD_Blue,
			SColor.HUD_Green,
			SColor.HUD_Yellow,
			SColor.HUD_Pink,
			SColor.HUD_Purple,
			SColor.HUD_Orange
		};
		colors.AddRange(list.Shuffle());
		int num = 0;
		foreach (SessionStatInfo statInfo in sessionStatInfo)
		{
			if (!sessionStats.ContainsKey(statInfo.Id))
			{
				continue;
			}
			Dictionary<string, long> stat = sessionStats[statInfo.Id];
			long amount = stat.Sum<KeyValuePair<string, long>>((KeyValuePair<string, long> kvp) => kvp.Value);
			string rightLabel = (statInfo.IsCurrency ? ("~g~" + amount.ToCurrencyString()) : ("~b~" + amount));
			if (statInfo.Id == "KDR")
			{
				rightLabel = "-";
			}
			UIMenuItem menuItem = new UIMenuItem(statInfo.Label, statInfo.Description ?? statInfo.Label);
			menuItem.SetRightLabel(rightLabel);
			mainView.SettingsColumn.AddSettings(menuItem);
			num++;
			SColor originalColor = menuItem.MainColor;
			menuItem.Highlighted += delegate
			{
				int num2 = 0;
				mainView.PlayersColumn.Clear();
				foreach (UIMenuItem item in mainView.SettingsColumn.Items.Except<UIMenuItem>(new UIMenuItem[1] { menuItem }))
				{
					item.MainColor = originalColor;
				}
				menuItem.MainColor = menuItem.HighlightColor;
				foreach (string userId in (from kvp in stat
					orderby kvp.Value descending
					select kvp.Key).Take(10))
				{
					if (!usernameDictionary.TryGetValue(userId, out string value))
					{
						value = "Unknown";
					}
					if (!levelDictionary.TryGetValue(userId, out var value2))
					{
						value2 = 0;
					}
					SColor itemColor = colors[num2];
					FriendItem friendItem = ((!(statInfo.Id == "KDR")) ? new FriendItem(value, itemColor, coloredTag: true, value2, statInfo.IsCurrency ? stat[userId].ToCurrencyString() : stat[userId].ToString("0.##")) : new FriendItem(value, itemColor, coloredTag: true, value2, ((double)stat[userId] / 100.0).ToString("0.00")));
					if (num2 == 0)
					{
						friendItem.SetLeftIcon(BadgeIcon.CROWN);
					}
					mainView.PlayersColumn.AddPlayer(friendItem);
					if (LatentPlayers.All.FirstOrDefault((PlayerState lp) => lp.Uid == userId) == null)
					{
						friendItem.SetOffline();
					}
					else
					{
						friendItem.SetOnline();
					}
					num2++;
				}
				mainView.PlayersColumn.CurrentSelection = 0;
			};
		}
		mainView.Visible = true;
	}

	private void CountKDR(Dictionary<string, Dictionary<string, long>> sessionStats)
	{
		Dictionary<string, long> dictionary = sessionStats.TryGetRefOrNull("KILLS") ?? new Dictionary<string, long>();
		Dictionary<string, long> dictionary2 = sessionStats.TryGetRefOrNull("DEATHS") ?? new Dictionary<string, long>();
		Dictionary<string, long> dictionary3 = new Dictionary<string, long>();
		foreach (string key in dictionary.Keys)
		{
			long num = dictionary[key];
			long num2 = (dictionary2.ContainsKey(key) ? dictionary2[key] : 0);
			long value = (long)(((num2 > 0) ? ((double)num / (double)num2) : ((double)num)) * 100.0);
			dictionary3[key] = value;
		}
		sessionStats["KDR"] = dictionary3;
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Invalid comparison between Unknown and I4
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard)
		{
			if (control.In((Control[])(object)new Control[2]
			{
				(Control)188,
				(Control)181
			}))
			{
				mainView.GoUp();
				return true;
			}
			if (control.In((Control[])(object)new Control[2]
			{
				(Control)187,
				(Control)180
			}))
			{
				mainView.GoDown();
				return true;
			}
			if ((int)control == 189)
			{
				mainView.GoLeft();
				return true;
			}
			if ((int)control == 190)
			{
				mainView.GoRight();
				return true;
			}
			if ((int)control == 202)
			{
				mainView.GoBack();
				return true;
			}
			if ((int)control == 204)
			{
				if (mainView.FocusLevel == 0)
				{
					mainView.SelectColumn(mainView.PlayersColumn);
				}
				else
				{
					mainView.SelectColumn(mainView.SettingsColumn);
				}
				return true;
			}
		}
		return false;
	}
}
