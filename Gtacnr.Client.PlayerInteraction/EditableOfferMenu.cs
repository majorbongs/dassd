using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gtacnr.Client.API;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.PlayerInteraction;

public class EditableOfferMenu : OfferMenu
{
	private class ModifyButtons
	{
		private EditableOfferMenu parentMenu;

		public MenuItem addItemButton;

		public MenuItem addPurchaseableButton;

		public ModifyButtons(EditableOfferMenu menu)
		{
			parentMenu = menu;
			addItemButton = new MenuItem("~b~" + LocalizationController.S(Entries.Player.MENU_TRADING_ADD_ITEM))
			{
				Label = Utils.MENU_ARROW
			};
			addPurchaseableButton = new MenuItem("~b~" + LocalizationController.S(Entries.Player.MENU_TRADING_ADD_PURCHASEABLE))
			{
				Label = Utils.MENU_ARROW
			};
			parentMenu.MainMenu.AddMenuItem(addItemButton);
			parentMenu.MainMenu.AddMenuItem(addPurchaseableButton);
			parentMenu.MainMenu.OnItemSelect += OnItemSelect;
		}

		private void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
		{
			if (menuItem == addItemButton)
			{
				menu.CloseMenu();
				InventoryMenuScript.OnItemSelected = OnInventoryItemSelected;
				InventoryMenuScript.ItemSelectInstructionalText = LocalizationController.S(Entries.Main.BTN_ADD);
				InventoryMenuScript.Open(setDefaults: false);
				InventoryMenuScript.Menu.ParentMenu = menu;
			}
			else if (menuItem == addPurchaseableButton)
			{
				menu.CloseMenu();
				PurchaseableItemsMenuScript.OnPurchaseableItemSelected = OnPurchaseableItemSelected;
				PurchaseableItemsMenuScript.TransferButtonEnabled = false;
				PurchaseableItemsMenuScript.ItemSelectInstructionalText = LocalizationController.S(Entries.Main.BTN_ADD);
				PurchaseableItemsMenuScript.Open(setDefaults: false);
				PurchaseableItemsMenuScript.MainMenu.ParentMenu = menu;
			}
		}

		private async Task OnInventoryItemSelected(Menu menu, MenuItem menuItem)
		{
			if (menuItem == null)
			{
				return;
			}
			object itemData = menuItem.ItemData;
			if (!(itemData is InventoryEntry entry))
			{
				return;
			}
			string itemId = entry.ItemId;
			InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(itemId);
			if (!itemInfo.CanGive)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Player.TRADING_RESPONSE_NOT_TRANSFERABLE));
				Utils.PlayErrorSound();
				return;
			}
			string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT), "", "", 12, "number");
			if (string.IsNullOrEmpty(text))
			{
				Utils.PlayErrorSound();
				return;
			}
			if (text == null || !float.TryParse(text, out var result) || result <= 0f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_INVALID_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			if (!itemInfo.IsFractional && !int.TryParse(text, out var _))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			if (result > entry.Amount)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_AMOUNT, itemInfo.Name));
				return;
			}
			parentMenu.AddModifyItem(itemInfo, result);
			parentMenu.wereChangesMade = true;
			Utils.SendNotification(LocalizationController.S(Entries.Player.MENU_TRADING_ITEM_ADDED, result.ToString("0.##"), itemInfo.Name));
			Utils.PlaySelectSound();
		}

		private async void OnPurchaseableItemSelected(PurchaseableItem purchaseableItem)
		{
			if (!purchaseableItem.IsTransferable)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Player.TRADING_RESPONSE_NOT_TRANSFERABLE));
				Utils.PlayErrorSound();
				return;
			}
			PurchaseableEntry entry = PurchaseableItemsMenuScript.PurchasedItems.Find((PurchaseableEntry p) => p.ItemId == purchaseableItem.Id);
			if (entry == null)
			{
				return;
			}
			decimal num = Math.Min(1m, entry.Amount);
			if (int.TryParse(await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT), "", $"{num:0.##}", 12, "number"), out var result))
			{
				num = result;
				if (num <= 0m)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_INVALID_AMOUNT));
					Utils.PlayErrorSound();
					return;
				}
				num = Math.Min(num, entry.Amount);
				parentMenu.AddModifyPurchaseable(purchaseableItem, num);
				parentMenu.wereChangesMade = true;
				Utils.SendNotification(LocalizationController.S(Entries.Player.MENU_TRADING_ITEM_ADDED, num.ToString("0.##"), purchaseableItem.Name));
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
				Utils.PlayErrorSound();
			}
		}
	}

	private bool wereChangesMade;

	public Action<TradeOffer> OnOfferChanged;

	private ModifyButtons modifyButtons;

	public EditableOfferMenu(string title, string subtitle, bool includeModifyButtons = false)
		: base(title, subtitle)
	{
		modifyButtons = new ModifyButtons(this);
		MainMenu.RemoveMenuItem(cashItem);
		MainMenu.AddMenuItem(cashItem);
		MainMenu.OnItemSelect += OnItemSelect;
		MainMenu.OnMenuClose += OnMenuClose;
		MainMenu.PlaySelectSound = true;
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == cashItem)
		{
			OpenCashPrompt();
			return;
		}
		object itemData = menuItem.ItemData;
		InventoryItem itemInfo = itemData as InventoryItem;
		int result;
		if (itemInfo != null)
		{
			string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT), "", "", 12, "number");
			if (string.IsNullOrEmpty(text))
			{
				Utils.PlayErrorSound();
				return;
			}
			if (!float.TryParse(text, out var amount) || amount < 0f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_INVALID_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			if (!itemInfo.IsFractional && !int.TryParse(text, out result))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
				Utils.PlayErrorSound();
				return;
			}
			IEnumerable<InventoryEntry> source = ((!InventoryMenuScript.ShouldRefreshCache) ? InventoryMenuScript.Cache : (await InventoryMenuScript.ReloadInventory()));
			InventoryEntry inventoryEntry = source.FirstOrDefault((InventoryEntry e) => e.ItemId == itemInfo.Id);
			if (inventoryEntry == null || amount > inventoryEntry.Amount)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_AMOUNT, itemInfo.Name));
				Utils.PlayErrorSound();
			}
			else
			{
				AddModifyItem(itemInfo, amount);
				wereChangesMade = true;
			}
			return;
		}
		itemData = menuItem.ItemData;
		PurchaseableItem purchasableInfo = itemData as PurchaseableItem;
		if (purchasableInfo == null)
		{
			return;
		}
		string text2 = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT), "", "", 12, "number");
		if (string.IsNullOrEmpty(text2))
		{
			Utils.PlayErrorSound();
			return;
		}
		if (!decimal.TryParse(text2, out var result2) || result2 < 0m)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_INVALID_AMOUNT));
			Utils.PlayErrorSound();
			return;
		}
		if (!int.TryParse(text2, out result))
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
			Utils.PlayErrorSound();
			return;
		}
		PurchaseableEntry purchaseableEntry = PurchaseableItemsMenuScript.PurchasedItems.Find((PurchaseableEntry p) => p.ItemId == purchasableInfo.Id);
		if (purchaseableEntry == null || result2 > purchaseableEntry.Amount)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_SEND_INSUFFICIENT_AMOUNT, purchasableInfo.Name));
			Utils.PlayErrorSound();
		}
		else
		{
			AddModifyPurchaseable(purchasableInfo, result2);
			wereChangesMade = true;
		}
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (wereChangesMade)
		{
			OnOfferChanged?.Invoke(base.tradeOffer);
			wereChangesMade = false;
		}
	}

	private async void OpenCashPrompt()
	{
		ulong currentCash = (ulong)(await Money.GetCachedBalanceOrFetch(AccountType.Cash));
		if (currentCash == 0)
		{
			return;
		}
		string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.INPUT_IMENU_INVENTORY_SEND_AMOUNT), "", "", 12, "number");
		if (string.IsNullOrEmpty(text))
		{
			Utils.PlayErrorSound();
			return;
		}
		if (!ulong.TryParse(text, out var result))
		{
			Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_FRACTIONAL_AMOUNT));
			Utils.PlayErrorSound();
			return;
		}
		if (currentCash < result)
		{
			Utils.SendNotification("You only have ~r~" + currentCash.ToCurrencyString() + " ~s~in cash.");
		}
		result = Math.Min(currentCash, result);
		UpdateCashOffer(result);
	}

	public override void AddModifyItem(InventoryItem itemInfo, float amount)
	{
		base.AddModifyItem(itemInfo, amount);
		InventoryEntry inventoryEntry = base.tradeOffer.Items.FirstOrDefault((InventoryEntry i) => i.ItemId == itemInfo.Id);
		if (inventoryEntry != null)
		{
			inventoryEntry.Amount = amount;
		}
		else
		{
			base.tradeOffer.Items.Add(new InventoryEntry
			{
				ItemId = itemInfo.Id,
				Amount = amount
			});
		}
		wereChangesMade = true;
	}

	public override void AddModifyPurchaseable(PurchaseableItem purchasableInfo, decimal amount)
	{
		base.AddModifyPurchaseable(purchasableInfo, amount);
		PurchaseableEntry purchaseableEntry = base.tradeOffer.PurchaseableItems.FirstOrDefault((PurchaseableEntry i) => i.ItemId == purchasableInfo.Id);
		if (purchaseableEntry != null)
		{
			purchaseableEntry.Amount = amount;
		}
		else
		{
			base.tradeOffer.PurchaseableItems.Add(new PurchaseableEntry
			{
				ItemId = purchasableInfo.Id,
				Amount = amount
			});
		}
		wereChangesMade = true;
	}

	public override void UpdateCashOffer(ulong cash)
	{
		base.UpdateCashOffer(cash);
		base.tradeOffer.Cash = cash;
		wereChangesMade = true;
	}
}
