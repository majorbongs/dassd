using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Premium;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class PurchaseableItemsMenuScript : Script
{
	private static Menu.ButtonPressHandler transferButtonPressHandler;

	private static string? _itemSelectInstructionalText;

	private static bool isRefreshing;

	public static PurchaseableItemsMenuScript Instance { get; private set; }

	public static MenuItem MainMenuItem { get; private set; }

	public static Menu MainMenu { get; private set; }

	public static bool TransferButtonEnabled
	{
		get
		{
			return MainMenu.InstructionalButtons.ContainsKey((Control)204);
		}
		set
		{
			if (value)
			{
				MainMenu.InstructionalButtons[(Control)204] = "Transfer";
				if (MainMenu.ButtonPressHandlers.IndexOf(transferButtonPressHandler) < 0)
				{
					MainMenu.ButtonPressHandlers.Add(transferButtonPressHandler);
				}
			}
			else
			{
				MainMenu.InstructionalButtons.Remove((Control)204);
				MainMenu.ButtonPressHandlers.Remove(transferButtonPressHandler);
			}
		}
	}

	public static Action<PurchaseableItem>? OnPurchaseableItemSelected { private get; set; }

	public static string? ItemSelectInstructionalText
	{
		private get
		{
			return _itemSelectInstructionalText;
		}
		set
		{
			_itemSelectInstructionalText = value;
			MainMenu.InstructionalButtons.Remove((Control)201);
			MainMenu.InstructionalButtons.Add((Control)201, _itemSelectInstructionalText);
		}
	}

	public static List<PurchaseableEntry>? PurchasedItems { get; private set; }

	protected override void OnStarted()
	{
		Instance = this;
		MainMenuItem = new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_PURCHASES_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_STOCK
		};
		MainMenu = new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_PURCHASES_SUBTITLE));
		MembershipScript.SubscriptionInfoUpdated = (EventHandler)Delegate.Combine(MembershipScript.SubscriptionInfoUpdated, (EventHandler)delegate
		{
			RefreshPurchases();
		});
		transferButtonPressHandler = new Menu.ButtonPressHandler((Control)204, Menu.ControlPressCheckType.JUST_PRESSED, OnTransferButtonPress, disableControl: true);
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)166, Menu.ControlPressCheckType.JUST_PRESSED, async delegate
		{
			await RefreshPurchases();
		}, disableControl: true));
		MainMenu.InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
		MainMenu.OnItemSelect += OnItemSelect;
	}

	public static async void Open(bool setDefaults = true)
	{
		MainMenu.ParentMenu = null;
		if (setDefaults)
		{
			SetDefaultValues();
		}
		MainMenu.OpenMenu();
	}

	public static void SetDefaultValues()
	{
		OnPurchaseableItemSelected = Instance.UsePurchaseableItem;
		TransferButtonEnabled = true;
		ItemSelectInstructionalText = LocalizationController.S(Entries.Main.BTN_SELECT);
	}

	public async Task RefreshPurchases()
	{
		if (isRefreshing)
		{
			return;
		}
		isRefreshing = true;
		try
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:memberships:getPurchases", new object[0]);
			PurchasedItems = null;
			if (string.IsNullOrEmpty(text))
			{
				MainMenuItem.Enabled = false;
				MainMenuItem.Label = "~r~ERROR";
				MainMenuItem.Description = LocalizationController.S(Entries.Main.UNEXPECTED_ERROR) + " " + LocalizationController.S(Entries.Main.ERROR_LOADING_PURCHASE_DATA);
				return;
			}
			MainMenuItem.Enabled = true;
			MainMenuItem.Label = null;
			MainMenuItem.Description = "Manage your ~p~purchases~s~.";
			PurchasedItems = text.Unjson<List<PurchaseableEntry>>();
			MainMenu.ClearMenuItems();
			foreach (PurchaseableEntry purchasedItem in PurchasedItems)
			{
				PurchaseableItem definition = PurchaseableItems.GetDefinition(purchasedItem.ItemId);
				if (definition != null && !(purchasedItem.Amount <= 0m))
				{
					MenuItem.Icon leftIcon = MenuItem.Icon.NONE;
					MainMenu.AddMenuItem(new MenuItem(definition.Name, definition.Description)
					{
						Label = $"{purchasedItem.Amount:0}",
						LeftIcon = leftIcon,
						ItemData = definition
					});
				}
			}
			MainMenu.InstructionalButtons.Remove((Control)201);
			if (PurchasedItems.Count == 0)
			{
				MainMenu.AddMenuItem(new MenuItem("No purchases :(", "There are no purchases to display. Press F5 to refresh.")
				{
					Enabled = false
				});
			}
			else
			{
				MainMenu.InstructionalButtons.Add((Control)201, ItemSelectInstructionalText);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isRefreshing = false;
		}
	}

	private async void UsePurchaseableItem(PurchaseableItem purchaseableItem)
	{
		if (purchaseableItem.Type == PurchaseableItemType.MembershipGift || purchaseableItem.Type == PurchaseableItemType.MembershipPreOrder)
		{
			if (await Utils.ShowConfirm($"Do you really want to activate a ~y~{purchaseableItem.Name}~s~? It will start today and end in ~y~{purchaseableItem.DurationDays} days~s~.", "activate", TimeSpan.FromSeconds(5.0)))
			{
				ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:memberships:activateFromGiftCard", purchaseableItem.Id);
				switch (responseCode)
				{
				case ResponseCode.Success:
					Utils.DisplayHelpText("You successfully activated your ~p~" + purchaseableItem.Name + "~s~.", playSound: false);
					await RefreshPurchases();
					break;
				case ResponseCode.Busy:
					Utils.DisplayHelpText("~r~The server is still processing your previous request. Please, wait before sending another one.");
					break;
				case ResponseCode.AlreadyActive:
					Utils.DisplayHelpText("~r~You already have an active subscription. If you need help, open a support ticket on Discord.");
					break;
				case ResponseCode.UnableToDeactivate:
					Utils.DisplayHelpText("~r~Unable to deactivate your current Silver Membership to upgrade. If the error persists, please open a support ticket on our Discord.");
					break;
				default:
					Utils.DisplayError(responseCode, "", "UsePurchaseableItem");
					break;
				}
			}
		}
		else if (purchaseableItem.Extra == "character")
		{
			Utils.DisplayHelpText("Visit a ~r~hospital ~s~to get a ~p~plastic surgery~s~.");
		}
		else if (purchaseableItem.Extra == "name")
		{
			Utils.DisplayHelpText("Go back to the ~b~Account ~s~menu to change your username.");
		}
		else
		{
			Utils.DisplayHelpText("This kind of item ~r~cannot be used~s~.");
			Utils.PlayErrorSound();
		}
	}

	private async void StartTransferPurchase(PurchaseableItem purchaseableItem)
	{
		Utils.PlaySelectSound();
		string inputUsername = await Utils.GetUserInput("Transfer " + purchaseableItem.Name, "Insert the in-game name (excluding crew tag) of the player you want to send this item to. Warning: we don't guarantee anything in exchange for this transaction and we are not responsible for any mistake or scam. For untrusted transactions, we recommend using the trading menu instead.", "", 24);
		if (string.IsNullOrWhiteSpace(inputUsername))
		{
			Utils.PlayErrorSound();
			Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
			return;
		}
		bool flag = false;
		foreach (PlayerState item in LatentPlayers.All)
		{
			if (item.Name.ToLowerInvariant() == inputUsername.ToLowerInvariant())
			{
				flag = true;
				break;
			}
		}
		if (!flag && !(await Utils.ShowConfirm("Player ~y~" + inputUsername + " ~s~is not online. Are you really sure that this is the player you want to send the item to?\n Make sure you have entered the in-game name of the player (excluding their crew tag). If you send this item to the ~r~wrong account ~s~you will ~r~not have it back~s~.", "Warning")))
		{
			Utils.PlayErrorSound();
			Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
			return;
		}
		if (!int.TryParse(await Utils.GetUserInput("Amount", "How many " + purchaseableItem.Name + "s do you want to transfer to " + inputUsername + "?", "1", 24), out var amount))
		{
			amount = 1;
		}
		Utils.PlaySelectSound();
		try
		{
			TransferPurchaseResponse transferPurchaseResponse = (TransferPurchaseResponse)(await TriggerServerEventAsync<int>("gtacnr:memberships:transferPurchase", new object[3] { purchaseableItem.Id, inputUsername, amount }));
			switch (transferPurchaseResponse)
			{
			case TransferPurchaseResponse.Success:
				Utils.DisplayHelpText($"You successfully transferred {amount} ~p~{purchaseableItem.Name} ~s~to ~y~{inputUsername}~s~.", playSound: false);
				await RefreshPurchases();
				break;
			case TransferPurchaseResponse.Busy:
				Utils.DisplayHelpText("~r~The server is still processing your previous request. Please, wait before sending another one.");
				break;
			case TransferPurchaseResponse.InvalidTarget:
				Utils.DisplayHelpText("~r~The server is unable to find the specified player.");
				break;
			case TransferPurchaseResponse.SelfTarget:
				Utils.DisplayHelpText("~r~You can't send a gift to yourself.");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x22-{(int)transferPurchaseResponse}"));
				break;
			}
		}
		catch (Exception exception)
		{
			Print(exception);
			Utils.PlayErrorSound();
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x22"), playSound: false);
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is PurchaseableItem obj)
		{
			OnPurchaseableItemSelected?.Invoke(obj);
		}
	}

	private async void OnTransferButtonPress(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is PurchaseableItem purchaseableItem)
		{
			StartTransferPurchase(purchaseableItem);
		}
	}
}
