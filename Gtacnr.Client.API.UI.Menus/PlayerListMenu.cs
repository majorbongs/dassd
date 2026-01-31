using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.API.UI.Menus;

public static class PlayerListMenu
{
	private static readonly Menu MainMenu;

	private static Action<Menu, int>? OnPlayerSelected;

	private static Menu.ButtonPressHandler searchButtonPressHandler;

	static PlayerListMenu()
	{
		MainMenu = new Menu(LocalizationController.S(Entries.Main.MENU_PLAYERLIST_TITLE));
		OnPlayerSelected = null;
		searchButtonPressHandler = new Menu.ButtonPressHandler((Control)206, Menu.ControlPressCheckType.JUST_PRESSED, async delegate
		{
			await SearchPlayer();
		}, disableControl: true);
		MainMenu.OnItemSelect += OnItemSelect;
	}

	private static void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is int arg)
		{
			OnPlayerSelected?.Invoke(menu, arg);
		}
	}

	private static async Task SearchPlayer()
	{
		string input = await Utils.GetUserInput(LocalizationController.S(Entries.Main.BTN_SEARCH), "", "", 23);
		int targetId;
		bool isId = int.TryParse(input, out targetId);
		if (string.IsNullOrWhiteSpace(input))
		{
			MainMenu.ResetFilter();
		}
		else
		{
			MainMenu.FilterMenuItems(delegate(MenuItem item)
			{
				if (item.ItemData is PlayerState playerState)
				{
					if (isId)
					{
						return playerState.Id == targetId;
					}
					if (!(playerState.Id.ToString() == input))
					{
						return playerState.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
					}
					return true;
				}
				return false;
			});
		}
		Utils.PlaySelectSound();
	}

	public static void ShowMenu(Menu? previousMenu = null, IEnumerable<PlayerState>? players = null, Action<Menu, int>? onPlayerSelected = null, Action<MenuItem, int>? customizeMenuItem = null, bool exceptMe = false, string? menuTitle = null, string? menuSubtitle = null, string? noPlayersText = null, string? noPlayersDescription = null, bool enableSearch = true)
	{
		players = players ?? LatentPlayers.All;
		menuTitle = menuTitle ?? LocalizationController.S(Entries.Main.MENU_PLAYERLIST_TITLE);
		menuSubtitle = menuSubtitle ?? LocalizationController.S(Entries.Main.MENU_PLAYERLIST_SUBTITLE);
		noPlayersText = noPlayersText ?? LocalizationController.S(Entries.Main.MENU_PLAYERLIST_NO_PLAYERS_TEXT);
		noPlayersDescription = noPlayersDescription ?? LocalizationController.S(Entries.Main.MENU_PLAYERLIST_NO_PLAYERS_DESCRIPTION);
		MainMenu.ClearMenuItems();
		MainMenu.MenuTitle = menuTitle;
		MainMenu.MenuSubtitle = menuSubtitle;
		if (previousMenu != null)
		{
			MenuController.AddSubmenu(previousMenu, MainMenu);
		}
		else
		{
			MainMenu.ParentMenu = null;
		}
		if (enableSearch)
		{
			if (!MainMenu.InstructionalButtons.ContainsKey((Control)206))
			{
				MainMenu.InstructionalButtons.Add((Control)206, LocalizationController.S(Entries.Main.BTN_SEARCH));
			}
			if (!MainMenu.ButtonPressHandlers.Contains(searchButtonPressHandler))
			{
				MainMenu.ButtonPressHandlers.Add(searchButtonPressHandler);
			}
		}
		else
		{
			if (MainMenu.InstructionalButtons.ContainsKey((Control)206))
			{
				MainMenu.InstructionalButtons.Remove((Control)206);
			}
			if (MainMenu.ButtonPressHandlers.Contains(searchButtonPressHandler))
			{
				MainMenu.ButtonPressHandlers.Remove(searchButtonPressHandler);
			}
		}
		OnPlayerSelected = onPlayerSelected;
		foreach (PlayerState player in players)
		{
			if (!exceptMe || player.Id != Game.Player.ServerId)
			{
				MenuItem menuItem = new MenuItem(player.ColorNameAndId)
				{
					ItemData = player.Id
				};
				customizeMenuItem?.Invoke(menuItem, player.Id);
				MainMenu.AddMenuItem(menuItem);
			}
		}
		if (MainMenu.GetMenuItems().Count == 0)
		{
			MainMenu.AddMenuItem(new MenuItem(noPlayersText, noPlayersDescription));
		}
		if (MenuController.IsAnyMenuOpen())
		{
			MenuController.CloseAllMenus();
		}
		MainMenu.OpenMenu();
	}
}
