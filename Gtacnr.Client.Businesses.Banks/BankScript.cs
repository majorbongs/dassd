using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Businesses.Banks;

public class BankScript : Script
{
	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Menu historyMenu;

	private int currentHistoryPage;

	protected override void OnStarted()
	{
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_INTEREST_RATE), "Capped at ~b~" + 100000L.ToCurrencyString() + "~s~.")
		{
			Label = $"~b~{0.04f:0.00}%",
			PlaySelectSound = false
		});
		MenuItem menuItem = (menuItems["deposit"] = new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_DEPOSIT), LocalizationController.S(Entries.Banking.MENU_BANK_DEPOSIT_DESC)));
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, menuItem);
		menuItem = (menuItems["withdraw"] = new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_WITHDRAW), LocalizationController.S(Entries.Banking.MENU_BANK_WITHDRAW_DESC)));
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, menuItem);
		menuItem = (menuItems["transfer"] = new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_TRANSFER), LocalizationController.S(Entries.Banking.MENU_BANK_TRANSFER_DESC)));
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, menuItem);
		menuItem = (menuItems["history"] = new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_HISTORY), LocalizationController.S(Entries.Banking.MENU_BANK_HISTORY_DESC)));
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, menuItem);
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_LOAN), LocalizationController.S(Entries.Banking.MENU_BANK_LOAN_DESC) + "\n" + LocalizationController.S(Entries.Main.NOT_IMPLEMENTED))
		{
			Enabled = false
		};
		menuItem = obj;
		dictionary["loan"] = obj;
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, menuItem);
		Dictionary<string, MenuItem> dictionary2 = menuItems;
		MenuItem obj2 = new MenuItem(LocalizationController.S(Entries.Banking.MENU_BANK_DEBT), LocalizationController.S(Entries.Banking.MENU_BANK_DEBT_DESC) + "\n" + LocalizationController.S(Entries.Main.NOT_IMPLEMENTED))
		{
			Enabled = false
		};
		menuItem = obj2;
		dictionary2["debt"] = obj2;
		ShoppingScript.AddExternalMenuItem(BusinessType.Bank, menuItem);
		ShoppingScript.AddExternalItemSelectHandler(OnMenuItemSelect);
		historyMenu = new Menu(LocalizationController.S(Entries.Banking.MENU_BANK_HISTORY), LocalizationController.S(Entries.Banking.MENU_BANK_HISTORY_SUB));
		historyMenu.OnItemSelect += OnMenuItemSelect;
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int index)
	{
		if (IsSelected("deposit"))
		{
			string text = await Utils.GetUserInput(LocalizationController.S(Entries.Banking.INPUT_ATM_DEPOSIT_TITLE), LocalizationController.S(Entries.Banking.INPUT_ATM_DEPOSIT_CONTENT), "", 11, "number");
			if (text == null || !int.TryParse(text, out var result))
			{
				Utils.PlayErrorSound();
				return;
			}
			if (result < 1)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.DEPOSIT_TOO_LITTLE, 1.ToCurrencyString()));
				return;
			}
			if (result > 100000000)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.DEPOSIT_TOO_MUCH, 100000000.ToCurrencyString()));
				return;
			}
			switch (await TriggerServerEventAsync("gtacnr:businesses:bank:deposit", result, BusinessScript.ClosestBusiness.Id))
			{
			case ResponseCode.Success:
				Utils.PlaySelectSound();
				break;
			case ResponseCode.InsufficientFunds:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
				break;
			case ResponseCode.InvalidAmount:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INVALID_AMOUNT));
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				break;
			}
		}
		else if (IsSelected("withdraw"))
		{
			string text2 = await Utils.GetUserInput(LocalizationController.S(Entries.Banking.INPUT_ATM_WITHDRAW_TITLE), LocalizationController.S(Entries.Banking.INPUT_ATM_WITHDRAW_CONTENT), "", 11, "number");
			if (text2 == null || !int.TryParse(text2, out var result2))
			{
				Utils.PlayErrorSound();
				return;
			}
			if (result2 < 1)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.WITHDRAW_TOO_LITTLE, 1.ToCurrencyString()));
				return;
			}
			if (result2 > 100000000)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.WITHDRAW_TOO_MUCH, 100000000.ToCurrencyString()));
				return;
			}
			switch (await TriggerServerEventAsync("gtacnr:businesses:bank:withdraw", result2, BusinessScript.ClosestBusiness.Id))
			{
			case ResponseCode.Success:
				Utils.PlaySelectSound();
				break;
			case ResponseCode.InsufficientFunds:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
				break;
			case ResponseCode.InvalidAmount:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INVALID_AMOUNT));
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				break;
			}
		}
		else if (IsSelected("transfer"))
		{
			PlayerListMenu.ShowMenu(menu, LatentPlayers.All.OrderBy((PlayerState p) => p.Id), async delegate(Menu playerMenu, int playerId)
			{
				string text5 = await Utils.GetUserInput(LocalizationController.S(Entries.Banking.MENU_BANK_TRANSFER), LocalizationController.S(Entries.Banking.MENU_BANK_TRANSFER_AMOUNT), "", 11, "number");
				if (text5 == null || !int.TryParse(text5, out var amount))
				{
					Utils.PlayErrorSound();
				}
				else
				{
					text5 = await Utils.GetUserInput(LocalizationController.S(Entries.Banking.MENU_BANK_TRANSFER), LocalizationController.S(Entries.Banking.MENU_BANK_TRANSFER_REASON), "", 64);
					if (text5 == null)
					{
						Utils.PlayErrorSound();
					}
					else
					{
						string text6 = text5;
						ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:bank:transfer", playerId, amount, text6, BusinessScript.ClosestBusiness.Id);
						switch (responseCode)
						{
						case ResponseCode.Success:
							Utils.PlaySelectSound();
							break;
						case ResponseCode.InsufficientFunds:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
							break;
						case ResponseCode.InvalidAmount:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INVALID_AMOUNT));
							break;
						default:
							Utils.DisplayError(responseCode, "", "OnMenuItemSelect");
							break;
						}
					}
				}
			}, null, exceptMe: true);
		}
		else if (IsSelected("history"))
		{
			MenuController.CloseAllMenus();
			MenuController.AddSubmenu(menu, historyMenu);
			historyMenu.OpenMenu();
			currentHistoryPage = 0;
			historyMenu.ClearMenuItems();
			historyMenu.AddLoadingMenuItem();
			RefreshHistoryMenu(await ReloadHistory());
		}
		else if (IsSelected("loan"))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
		}
		else if (IsSelected("debt"))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
		}
		else if (menuItem.ItemData is string text3 && text3 == "prevPage")
		{
			currentHistoryPage--;
			historyMenu.ClearMenuItems();
			historyMenu.AddLoadingMenuItem();
			RefreshHistoryMenu(await ReloadHistory());
		}
		else if (menuItem.ItemData is string text4 && text4 == "nextPage")
		{
			currentHistoryPage++;
			historyMenu.ClearMenuItems();
			historyMenu.AddLoadingMenuItem();
			RefreshHistoryMenu(await ReloadHistory());
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

	private async Task<List<UnifiedTransaction>?> ReloadHistory()
	{
		string text = await TriggerServerEventAsync<string>("gtacnr:businesses:bank:history", new object[2]
		{
			currentHistoryPage,
			BusinessScript.ClosestBusiness.Id
		});
		if (int.TryParse(text, out var result))
		{
			Utils.DisplayError((ResponseCode)result, "", "ReloadHistory");
			return null;
		}
		return text?.Unjson<List<UnifiedTransaction>>();
	}

	private void RefreshHistoryMenu(List<UnifiedTransaction>? transactions)
	{
		if (transactions == null)
		{
			return;
		}
		historyMenu.ClearMenuItems();
		historyMenu.CounterPreText = $"Page {currentHistoryPage + 1}";
		if (currentHistoryPage > 0)
		{
			historyMenu.AddMenuItem(new MenuItem("~b~Newer transactions")
			{
				ItemData = "prevPage"
			});
		}
		foreach (UnifiedTransaction transaction in transactions)
		{
			bool flag = transaction.FromCharacterId == MainScript.SelectedCharacter.Id;
			MenuItem item = new MenuItem($"~c~{transaction.DateTime.FormatShortDateString()} at {transaction.DateTime:HH:mm:ss}", transaction.Description)
			{
				Label = (flag ? ("~r~(" + transaction.Amount.ToCurrencyString() + ")") : ("~g~" + transaction.Amount.ToCurrencyString())),
				PlaySelectSound = false
			};
			historyMenu.AddMenuItem(item);
		}
		historyMenu.AddMenuItem(new MenuItem("~b~Older transactions")
		{
			ItemData = "nextPage"
		});
		historyMenu.CurrentIndex = 0;
	}
}
