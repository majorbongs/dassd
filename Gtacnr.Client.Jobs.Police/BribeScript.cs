using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Communication;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Police;

public class BribeScript : Script
{
	private class BribeInfo
	{
		public int PlayerId { get; set; }

		public int Amount { get; set; }

		public DateTime DateTime { get; set; }

		public bool WasRead { get; set; }

		public bool HasResponse { get; set; }
	}

	public static readonly List<List<int>> BribeValues = Gtacnr.Utils.LoadJson<List<List<int>>>("data/police/bribe.json");

	private static BribeScript instance;

	private Menu bribeOfferedMenu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private BribeInfo currentBribe;

	private bool isBusy;

	public BribeScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		bribeOfferedMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_BRIBE_TITLE));
		bribeOfferedMenu.OnItemSelect += OnMenuItemSelected;
		bribeOfferedMenu.OnMenuClose += OnBribeMenuClosed;
	}

	private void OnBribeMenuClosed(Menu menu, MenuClosedEventArgs e)
	{
		CancelCurrentBribeInternal();
	}

	private void CancelCurrentBribeInternal()
	{
		if (currentBribe != null && !currentBribe.HasResponse)
		{
			PlayerState playerState = LatentPlayers.Get(currentBribe.PlayerId);
			currentBribe = null;
			Utils.DisplayHelpText("You ignored " + playerState.ColorNameAndId + "'s bribe offer.", playSound: false, 4000);
			BaseScript.TriggerServerEvent("gtacnr:police:bribeIgnored", new object[0]);
		}
	}

	public static void CancelCurrentBribe()
	{
		instance.CancelCurrentBribeInternal();
	}

	private void OpenBribeOfferMenu()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (currentBribe == null)
		{
			return;
		}
		MenuController.CloseAllMenus();
		bribeOfferedMenu.OpenMenu();
		bribeOfferedMenu.ClearMenuItems();
		PlayerState playerState = LatentPlayers.Get(currentBribe.PlayerId);
		bribeOfferedMenu.MenuSubtitle = "Bribe offer by " + playerState.ColorNameAndId + ".";
		int num = 0;
		float num2 = 900f;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (!(player == Game.Player) && !((Entity)player.Character).IsDead && LatentPlayers.Get(player).JobEnum.IsPolice())
			{
				Vector3 position2 = ((Entity)player.Character).Position;
				if (((Vector3)(ref position2)).DistanceToSquared(position) < num2)
				{
					num++;
				}
			}
		}
		MenuItem item;
		if (num == 0)
		{
			Menu menu = bribeOfferedMenu;
			Dictionary<string, MenuItem> dictionary = menuItems;
			MenuItem obj = new MenuItem("Accept", "Accept the bribe and let the suspect go. ~y~Warning: you will lose some XP.")
			{
				Label = "~g~+" + currentBribe.Amount.ToCurrencyString()
			};
			item = obj;
			dictionary["acceptKeep"] = obj;
			menu.AddMenuItem(item);
		}
		else
		{
			int amount = Convert.ToInt32(Math.Round((double)currentBribe.Amount / (double)(num + 1)));
			Menu menu2 = bribeOfferedMenu;
			item = (menuItems["accept"] = new MenuItem("Accept and share", string.Format("Accept the bribe, share with {0} other nearby cop{1}, and let the suspect go. You will not lose XP if you share it with other cops.", num, (num == 1) ? "" : "s"))
			{
				Label = "~g~+" + amount.ToCurrencyString()
			});
			menu2.AddMenuItem(item);
			if (PartyScript.IsInParty)
			{
				Menu menu3 = bribeOfferedMenu;
				Dictionary<string, MenuItem> dictionary2 = menuItems;
				MenuItem obj2 = new MenuItem("Accept and share with party", "Accept the bribe, share it with cops who are in your party, and let the suspect go. You will not lose XP if you share it with other cops.")
				{
					Label = "~g~+" + amount.ToCurrencyString()
				};
				item = obj2;
				dictionary2["acceptParty"] = obj2;
				menu3.AddMenuItem(item);
			}
			Menu menu4 = bribeOfferedMenu;
			item = (menuItems["acceptKeep"] = new MenuItem("Accept", "Accept the bribe and keep all the money for yourself. ~r~Warning: this will make you lose a substantial amount of XP!")
			{
				Label = "~g~+" + currentBribe.Amount.ToCurrencyString()
			});
			menu4.AddMenuItem(item);
		}
		Menu menu5 = bribeOfferedMenu;
		item = (menuItems["refuse"] = new MenuItem("~b~Refuse", "Refuse the bribe and report the player to the police. The player will gain another wanted level. Select this option if you are not a crooked cop and you want to do the right thing.")
		{
			Label = "~b~+5 XP"
		});
		menu5.AddMenuItem(item);
	}

	private async void OnMenuItemSelected(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (currentBribe == null || (!IsSelected("accept") && !IsSelected("acceptParty") && !IsSelected("acceptKeep") && !IsSelected("refuse")))
		{
			return;
		}
		currentBribe.HasResponse = true;
		menu.CloseMenu();
		PlayerState targetInfo = LatentPlayers.Get(currentBribe.PlayerId);
		ResponseCode? responseCode = null;
		if (IsSelected("accept"))
		{
			responseCode = await TriggerServerEventAsync("gtacnr:police:answerBribe", true, 0);
		}
		else if (IsSelected("acceptParty"))
		{
			responseCode = await TriggerServerEventAsync("gtacnr:police:answerBribe", true, 1);
		}
		else if (IsSelected("acceptKeep"))
		{
			responseCode = await TriggerServerEventAsync("gtacnr:police:answerBribe", true, 2);
		}
		else if (IsSelected("refuse"))
		{
			responseCode = await TriggerServerEventAsync("gtacnr:police:answerBribe", false, -1);
		}
		if (responseCode.HasValue && responseCode != ResponseCode.Success)
		{
			switch (responseCode)
			{
			case ResponseCode.TooFar:
				Utils.DisplayHelpText("You are ~r~too far ~s~from " + targetInfo.ColorNameAndId + ", you can't accept or refuse the ~r~bribe~s~.");
				break;
			case ResponseCode.Cooldown:
				Utils.DisplayHelpText("You must wait 10 minutes before accepting a ~r~bribe ~s~from the same player again.");
				break;
			case ResponseCode.InsufficientFunds:
				Utils.DisplayHelpText(targetInfo.ColorNameAndId + " could not afford the ~r~bribe~s~.");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x19-{(int)responseCode.Value}"));
				break;
			}
		}
		currentBribe = null;
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}

	public static async Task<bool> Bribe(int targetId, int amount)
	{
		return await instance.BribeInternal(targetId, amount);
	}

	private async Task<bool> BribeInternal(int targetId, int amount)
	{
		if (isBusy)
		{
			return false;
		}
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		try
		{
			if (await Money.GetCachedBalanceOrFetch(AccountType.Cash) < amount)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				return false;
			}
			if (((Entity)Game.PlayerPed).Health <= 0 || ((Entity)Game.PlayerPed).IsDead)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD));
				return false;
			}
			isBusy = true;
			currentBribe = new BribeInfo
			{
				PlayerId = targetId,
				Amount = amount,
				DateTime = DateTime.UtcNow
			};
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:police:bribe", targetId, amount);
			switch (responseCode)
			{
			case ResponseCode.Success:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_OFFERED_AMOUNT_BRIBE, targetInfo.ColorNameAndId, amount.ToCurrencyString()));
				return true;
			case ResponseCode.InvalidTarget:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_NO_LONGER_CONNECTED_BRIBE, targetInfo));
				break;
			case ResponseCode.TooFar:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_NOT_CLOSE_ENOUGH_TO_BRIBE, targetInfo));
				break;
			case ResponseCode.InProgress:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_ALREADY_HAVE_OPEN_OFFER));
				break;
			case ResponseCode.TargetInProgress:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_PLAYER_ALREADY_HAS_OPEN_OFFER, targetInfo));
				break;
			case ResponseCode.Cooldown:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_CANT_BRIBE_SAME_OFFICER_COOLDOWN, 10));
				break;
			case ResponseCode.RecentlyAutoCuffed:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_CANT_BRIBE_AFTER_REVIVE_AUTO_CUFF, 30));
				break;
			default:
				Utils.DisplayError(responseCode, "", "BribeInternal");
				break;
			}
			return false;
		}
		catch (Exception exception)
		{
			Print($"An exception has occurred while bribing officer {targetInfo}: {amount.ToCurrencyString()}");
			Print(exception);
			return false;
		}
		finally
		{
			isBusy = false;
		}
	}

	[EventHandler("gtacnr:police:bribeOffered")]
	private async void OnBribeOffered(int suspectId, int amount)
	{
		PlayerState playerInfo = LatentPlayers.Get(suspectId);
		string text = ((!Utils.IsUsingKeyboard()) ? LocalizationController.S(Entries.Businesses.STP_HOLD, "~INPUT_REPLAY_SCREENSHOT~") : LocalizationController.S(Entries.Businesses.STP_PRESS, "~INPUT_MP_TEXT_CHAT_TEAM~"));
		string text2 = text;
		currentBribe = new BribeInfo
		{
			PlayerId = suspectId,
			Amount = amount,
			DateTime = DateTime.UtcNow
		};
		string message = LocalizationController.S(Entries.Jobs.POLICE_RECEIVED_BRIBE, playerInfo.ColorNameAndId, amount.ToCurrencyString(), text2);
		bool accepted = false;
		string text3 = LocalizationController.S(Entries.Jobs.POLICE_RECEIVED_BRIBE_INSTRUCTIONAL);
		await InteractiveNotificationsScript.Show(message, InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 1u, text3, LocalizationController.S(Entries.Businesses.BTN_STP_LABEL_HOLD, text3), () => currentBribe == null || currentBribe.WasRead);
		if (!accepted)
		{
			currentBribe = null;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_IGNORED_BRIBE, playerInfo.ColorNameAndId), playSound: false, 4000);
			BaseScript.TriggerServerEvent("gtacnr:police:bribeIgnored", new object[0]);
		}
		bool OnAccepted()
		{
			accepted = true;
			Utils.DisplayHelpText();
			if (currentBribe == null)
			{
				return false;
			}
			currentBribe.WasRead = true;
			OpenBribeOfferMenu();
			BaseScript.TriggerServerEvent("gtacnr:police:bribeViewed", new object[0]);
			return true;
		}
	}

	[EventHandler("gtacnr:police:bribeViewed")]
	private void OnBribeViewed()
	{
		if (currentBribe != null)
		{
			PlayerState playerState = LatentPlayers.Get(currentBribe.PlayerId);
			if (playerState != null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_VIEWING_BRIBE, playerState.ColorNameAndId), playSound: false);
			}
		}
	}

	[EventHandler("gtacnr:police:bribeIgnored")]
	private void OnBribeIgnored(bool disconnected)
	{
		if (currentBribe == null)
		{
			return;
		}
		PlayerState playerState = LatentPlayers.Get(currentBribe.PlayerId);
		if (playerState != null)
		{
			if (!disconnected)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_PLAYER_IGNORED_BRIBE, playerState.ColorNameAndId), playSound: false);
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_PLAYER_CANCELED_BRIBE_DISCONNECT, playerState.ColorNameAndId), playSound: false);
			}
		}
		currentBribe = null;
	}

	[EventHandler("gtacnr:police:bribeAccepted")]
	private void OnBribeAccepted()
	{
		if (currentBribe != null)
		{
			PlayerState playerState = LatentPlayers.Get(currentBribe.PlayerId);
			if (playerState != null)
			{
				currentBribe = null;
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_ACCEPTED_BRIBE, playerState.ColorNameAndId), playSound: false);
			}
		}
	}

	[EventHandler("gtacnr:police:bribeRefused")]
	private void OnBribeRefused()
	{
		if (currentBribe != null)
		{
			PlayerState playerState = LatentPlayers.Get(currentBribe.PlayerId);
			if (playerState != null)
			{
				currentBribe = null;
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_REFUSED_BRIBE, playerState.ColorNameAndId), playSound: false);
			}
		}
	}
}
