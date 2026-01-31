using System.Collections.Generic;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.PlayerInteraction;

public class OfferMenu
{
	public Menu MainMenu;

	private Dictionary<string, MenuItem> itemMenuItems = new Dictionary<string, MenuItem>();

	private Dictionary<string, MenuItem> purchaseableMenuItems = new Dictionary<string, MenuItem>();

	protected MenuItem cashItem;

	public TradeOffer tradeOffer { get; private set; }

	public OfferMenu(string title, string subtitle)
	{
		MainMenu = new Menu(title, subtitle)
		{
			PlaySelectSound = false
		};
		tradeOffer = new TradeOffer();
		cashItem = new MenuItem(LocalizationController.S(Entries.Player.MENU_TRADING_CASH_TEXT))
		{
			Label = 0.ToCurrencyString()
		};
		MainMenu.AddMenuItem(cashItem);
		MenuController.AddMenu(MainMenu);
	}

	public void UpdateTradeOffer(TradeOffer newTradeOffer)
	{
		tradeOffer = newTradeOffer;
		foreach (InventoryEntry item in newTradeOffer.Items)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(item.ItemId);
			AddModifyItem(itemDefinition, item.Amount);
		}
		foreach (PurchaseableEntry purchaseableItem in newTradeOffer.PurchaseableItems)
		{
			PurchaseableItem definition = PurchaseableItems.GetDefinition(purchaseableItem.ItemId);
			if (definition != null)
			{
				AddModifyPurchaseable(definition, purchaseableItem.Amount);
			}
		}
		UpdateCashOffer(newTradeOffer.Cash);
	}

	public virtual void AddModifyItem(InventoryItem itemInfo, float amount)
	{
		if (!itemMenuItems.ContainsKey(itemInfo.Id))
		{
			MenuItem menuItem = new MenuItem(itemInfo.Name)
			{
				Label = GetAmountStr(),
				ItemData = itemInfo
			};
			itemMenuItems.Add(itemInfo.Id, menuItem);
			MainMenu.AddMenuItem(menuItem);
		}
		else
		{
			MenuItem menuItem2 = itemMenuItems[itemInfo.Id];
			if (amount == 0f)
			{
				MainMenu.RemoveMenuItem(menuItem2);
			}
			else
			{
				menuItem2.Label = GetAmountStr();
			}
		}
		string GetAmountStr()
		{
			if (itemInfo.IsFractional)
			{
				return $"{amount:0.##}{itemInfo.Unit}";
			}
			return $"{amount:0}{itemInfo.Unit}";
		}
	}

	public virtual void AddModifyPurchaseable(PurchaseableItem purchasableInfo, decimal amount)
	{
		if (!purchaseableMenuItems.ContainsKey(purchasableInfo.Id))
		{
			MenuItem menuItem = new MenuItem(purchasableInfo.Name)
			{
				Label = ((long)amount).ToString(),
				ItemData = purchasableInfo
			};
			purchaseableMenuItems.Add(purchasableInfo.Id, menuItem);
			MainMenu.AddMenuItem(menuItem);
		}
		else
		{
			MenuItem menuItem2 = purchaseableMenuItems[purchasableInfo.Id];
			if (amount == 0m)
			{
				MainMenu.RemoveMenuItem(menuItem2);
			}
			else
			{
				menuItem2.Label = amount.ToString("0");
			}
		}
	}

	public virtual void UpdateCashOffer(ulong cash)
	{
		cashItem.Label = "~g~" + cash.ToCurrencyString();
	}

	public void ClearAllButtons()
	{
		foreach (KeyValuePair<string, MenuItem> itemMenuItem in itemMenuItems)
		{
			MainMenu.RemoveMenuItem(itemMenuItem.Value);
		}
		itemMenuItems.Clear();
		foreach (KeyValuePair<string, MenuItem> purchaseableMenuItem in purchaseableMenuItems)
		{
			MainMenu.RemoveMenuItem(purchaseableMenuItem.Value);
		}
		purchaseableMenuItems.Clear();
		cashItem.Label = 0.ToCurrencyString();
	}
}
