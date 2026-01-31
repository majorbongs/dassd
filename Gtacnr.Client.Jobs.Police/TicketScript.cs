using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Police;

public class TicketScript : Script
{
	private class TicketInfo
	{
		public int OfficerId { get; set; }

		public int Amount { get; set; }
	}

	private Menu ticketMenu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private TicketInfo currentTicket;

	public TicketScript()
	{
		ticketMenu = new Menu(LocalizationController.S(Entries.Jobs.POLICE_TICKET_MENU_TITLE), "");
		List<int> list = BribeScript.BribeValues[1];
		List<string> list2 = new List<string>();
		foreach (int item in list)
		{
			list2.Add("~g~" + item.ToCurrencyString());
		}
		ticketMenu.AddMenuItem(menuItems["pay"] = new MenuItem(LocalizationController.S(Entries.Jobs.POLICE_TICKET_PAY), LocalizationController.S(Entries.Jobs.POLICE_TICKET_PAY_DESC)));
		ticketMenu.AddMenuItem(menuItems["contest"] = new MenuItem(LocalizationController.S(Entries.Jobs.POLICE_TICKET_CONTEST), LocalizationController.S(Entries.Jobs.POLICE_TICKET_CONTEST_DESC)));
		ticketMenu.AddMenuItem(menuItems["bribe"] = new MenuListItem(LocalizationController.S(Entries.Jobs.POLICE_TICKET_BRIBE), list2)
		{
			Description = LocalizationController.S(Entries.Jobs.POLICE_TICKET_BRIBE_DESC)
		});
		ticketMenu.OnItemSelect += OnItemSelect;
		ticketMenu.OnListItemSelect += OnListItemSelect;
		ticketMenu.OnMenuClose += OnMenuClose;
	}

	[EventHandler("gtacnr:police:ticketReceived")]
	private async void OnTicketReceived(int officerId, int fineAmount)
	{
		MenuController.CloseAllMenus();
		PlayerState playerState = LatentPlayers.Get(officerId);
		currentTicket = new TicketInfo
		{
			OfficerId = officerId,
			Amount = fineAmount
		};
		ticketMenu.MenuSubtitle = LocalizationController.S(Entries.Jobs.POLICE_TICKET_MENU_SUBTITLE, playerState.NameAndId);
		menuItems["pay"].Label = "~g~" + ((double)fineAmount * 0.75).ToInt().ToCurrencyString();
		menuItems["contest"].Label = "~b~(?) " + fineAmount.ToCurrencyString();
		ticketMenu.OpenMenu();
		DateTime start = DateTime.UtcNow;
		while (ticketMenu.Visible && DateTime.UtcNow - start <= TimeSpan.FromSeconds(30.0))
		{
			await BaseScript.Delay(500);
		}
		if (ticketMenu.Visible)
		{
			ticketMenu.CloseMenu(closedByUser: true);
		}
	}

	private void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == menuItems["pay"])
		{
			BaseScript.TriggerServerEvent("gtacnr:police:payTicket", new object[0]);
		}
		else if (menuItem == menuItems["contest"])
		{
			BaseScript.TriggerServerEvent("gtacnr:police:contestTicket", new object[0]);
		}
		currentTicket = null;
		ticketMenu.CloseMenu();
	}

	private async void OnListItemSelect(Menu menu, MenuListItem menuItem, int selectedIndex, int itemIndex)
	{
		if (menuItem == menuItems["bribe"])
		{
			int amount = BribeScript.BribeValues[1][selectedIndex];
			if (await BribeScript.Bribe(currentTicket.OfficerId, amount))
			{
				ticketMenu.CloseMenu();
			}
		}
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (e.ClosedByUser && currentTicket != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:police:refuseTicket", new object[0]);
		}
	}

	[EventHandler("gtacnr:police:bribeIgnored")]
	private void OnBribeIgnored(bool disconnected)
	{
		if (currentTicket != null)
		{
			if (disconnected)
			{
				currentTicket = null;
			}
			else
			{
				ticketMenu.OpenMenu();
			}
		}
	}
}
