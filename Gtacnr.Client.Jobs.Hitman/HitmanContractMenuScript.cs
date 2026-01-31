using System;
using System.Linq;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Phone;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Hitman;

public class HitmanContractMenuScript : Script
{
	private static readonly Menu MainMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_NAME));

	private static readonly MenuItem TargetItem = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_TARGET));

	private static readonly MenuItem RewardItem = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_REWARD));

	private static readonly MenuItem CreateButton = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_CREATE));

	private static int _targetId = 0;

	private static ulong reward = 0uL;

	private static bool isBusy = false;

	private static bool _onSite = false;

	private static HitmanContractMenuScript instance;

	public HitmanContractMenuScript()
	{
		instance = this;
		MainMenu.AddMenuItem(TargetItem);
		MainMenu.AddMenuItem(RewardItem);
		MainMenu.AddMenuItem(CreateButton);
		MenuController.AddMenu(MainMenu);
		MainMenu.OnItemSelect += OnItemSelect;
	}

	public static void ShowMenu(Menu parentMenu = null, bool onSite = false, int targetId = 0)
	{
		_onSite = onSite;
		_targetId = targetId;
		reward = 50000uL;
		float num = (_onSite ? 0.1f : 0.2f);
		if (parentMenu != null)
		{
			MenuController.AddSubmenu(parentMenu, MainMenu);
		}
		else
		{
			MainMenu.ParentMenu = null;
		}
		MainMenu.MenuSubtitle = LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_FEE, Math.Floor(num * 100f));
		if (targetId != 0)
		{
			PlayerState playerState = LatentPlayers.Get(targetId);
			TargetItem.Label = playerState.ColorNameAndId;
			instance.CustomizeTargetMenuItem(TargetItem, targetId);
		}
		else
		{
			TargetItem.Text = LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_TARGET);
			TargetItem.RightIcon = MenuItem.Icon.NONE;
			TargetItem.Label = Utils.MENU_ARROW;
		}
		RewardItem.Label = "~g~" + reward.ToCurrencyString();
		MainMenu.OpenMenu();
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == TargetItem)
		{
			MenuController.CloseAllMenus();
			RevengeData revengeData = DeathScript.CachedRevengeData;
			IOrderedEnumerable<PlayerState> players = LatentPlayers.All.OrderBy(delegate(PlayerState ps)
			{
				RevengeData revengeData2 = revengeData;
				return (revengeData2 == null) ? ((bool?)null) : new bool?(!revengeData2.Targets.Contains(ps.Id));
			});
			Action<Menu, int> onPlayerSelected = OnTargetSelected;
			PlayerListMenu.ShowMenu(menu, players, onPlayerSelected, CustomizeTargetMenuItem, exceptMe: true);
		}
		else
		{
			if (menuItem == RewardItem)
			{
				if (isBusy)
				{
					return;
				}
				try
				{
					isBusy = true;
					long num = ((!_onSite) ? (await Money.GetCachedBalanceOrFetch(AccountType.Bank)) : (await Money.GetCachedBalanceOrFetch(AccountType.Cash)));
					long currentBalance = num;
					if (currentBalance <= 0)
					{
						return;
					}
					ulong amount = 50000uL;
					string text = await Utils.GetUserInput(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_REWARD) + ": ", "", $"{amount:0.##}", 12, "number");
					if (text == null)
					{
						return;
					}
					if (text == "")
					{
						text = $"{amount}";
					}
					text = text.Replace("$", "").Replace(",", "").Replace(".", "")
						.Replace(" ", "");
					if (!ulong.TryParse(text, out amount))
					{
						Utils.SendNotification(LocalizationController.S(Entries.Player.AMOUNT_ENTERED_INVALID));
						Utils.PlayErrorSound();
						return;
					}
					if (amount < 50000)
					{
						Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_REWARD_BELOW_MINIMUM, 50000uL.ToCurrencyString()));
						Utils.PlayErrorSound();
						return;
					}
					if (amount > 1000000)
					{
						Utils.SendNotification(LocalizationController.S(Entries.Businesses.MENU_HITMAN_NEW_CONTRACT_REWARD_OVER_LIMIT, 1000000uL.ToCurrencyString()));
						Utils.PlayErrorSound();
						return;
					}
					float num2 = (_onSite ? 0.1f : 0.2f);
					if ((float)amount + num2 * (float)amount > (float)currentBalance)
					{
						Utils.SendNotification(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
						Utils.PlayErrorSound();
					}
					else
					{
						reward = amount;
						RewardItem.Label = "~g~" + reward.ToCurrencyString();
					}
					return;
				}
				finally
				{
					isBusy = false;
				}
			}
			if (menuItem != CreateButton || isBusy)
			{
				return;
			}
			if (_targetId == 0 || reward == 0L)
			{
				Utils.PlayErrorSound();
				return;
			}
			isBusy = true;
			try
			{
				PlayerState playerState = LatentPlayers.Get(_targetId);
				if (playerState == null)
				{
					return;
				}
				bool flag = !(DeathScript.CachedRevengeData?.Targets.Contains(_targetId) ?? false);
				if (flag)
				{
					flag = !(await Utils.ShowConfirm(LocalizationController.S(Entries.Businesses.HITMAN_NEW_CONTRACT_CONFIRMATION, playerState.ColorTextCode + playerState.Name + "~s~"), "~r~warning", TimeSpan.FromSeconds(10.0)));
				}
				if (flag)
				{
					return;
				}
				if (MainMenu.ParentMenu == PhoneMenuScript.ServiceMenu)
				{
					PhoneMenuScript.CallAnim();
				}
				CreateHitmanContractResponse createHitmanContractResponse = await TriggerServerEventAsync<CreateHitmanContractResponse>("gtacnr:hitman:newContract", new object[3] { _targetId, reward, _onSite });
				switch (createHitmanContractResponse)
				{
				case CreateHitmanContractResponse.Success:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.HITMAN_NEW_CONTRACT_RESPONSE_SUCCESS));
					break;
				case CreateHitmanContractResponse.InvalidTarget:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.HITMAN_NEW_CONTRACT_RESPONSE_INVALID_TARGET));
					Utils.PlayErrorSound();
					break;
				case CreateHitmanContractResponse.InvalidReward:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.HITMAN_NEW_CONTRACT_RESPONSE_INVALID_REWARD));
					Utils.PlayErrorSound();
					break;
				case CreateHitmanContractResponse.TooManyActiveContracts:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.HITMAN_NEW_CONTRACT_RESPONSE_TOO_MANY));
					Utils.PlayErrorSound();
					break;
				case CreateHitmanContractResponse.GenericError:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
					Utils.PlayErrorSound();
					break;
				case CreateHitmanContractResponse.NotEnoughMoney:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
					Utils.PlayErrorSound();
					break;
				case CreateHitmanContractResponse.TransactionError:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_TRANSACTION));
					Utils.PlayErrorSound();
					break;
				case CreateHitmanContractResponse.Duplicate:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.HITMAN_NEW_CONTRACT_RESPONSE_DUPLICATE));
					Utils.PlayErrorSound();
					break;
				default:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x6D-{(int)createHitmanContractResponse}"));
					break;
				}
			}
			finally
			{
				isBusy = false;
			}
			MainMenu.CloseMenu();
		}
	}

	private void OnTargetSelected(Menu menu, int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			_targetId = playerId;
			TargetItem.Label = playerState.ColorNameAndId;
			CustomizeTargetMenuItem(TargetItem, playerId);
			menu.CloseMenu();
			MainMenu.OpenMenu();
		}
	}

	private void CustomizeTargetMenuItem(MenuItem menuItem, int playerId)
	{
		RevengeData cachedRevengeData = DeathScript.CachedRevengeData;
		if (cachedRevengeData != null && cachedRevengeData.Targets.Contains(playerId))
		{
			menuItem.RightIcon = MenuItem.Icon.INV_SURVIVAL;
		}
		else
		{
			menuItem.RightIcon = MenuItem.Icon.NONE;
		}
	}
}
