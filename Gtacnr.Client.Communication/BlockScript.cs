using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Communication;

public class BlockScript : Script
{
	private static HashSet<string> blockedPlayers = new HashSet<string>();

	private static Menu blockMenu;

	protected override void OnStarted()
	{
		blockMenu = new Menu(LocalizationController.S(Entries.Main.MENU_BLOCKED_TITLE), " ");
		MenuController.AddMenu(blockMenu);
		blockMenu.OnItemSelect += OnMenuItemSelect;
		blockMenu.InstructionalButtons.Clear();
		blockMenu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Main.BTN_UNBLOCK));
		blockMenu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		string resourceKvpString = API.GetResourceKvpString("blocked_uids");
		if (!string.IsNullOrEmpty(resourceKvpString))
		{
			blockedPlayers = resourceKvpString.Unjson<HashSet<string>>();
			BaseScript.TriggerEvent("gtacnr:chat:blockedUpdated", new object[0]);
		}
		Chat.AddSuggestion("/block", LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_HELP), new ChatParamSuggestion(LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_PARAM_NAME), LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_PARAM_HELP), isOptional: true));
		Chat.AddSuggestion("/unblock", LocalizationController.S(Entries.Main.UNBLOCK_CMD_SUGGESTION_HELP), new ChatParamSuggestion(LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_PARAM_NAME), LocalizationController.S(Entries.Main.UNBLOCK_CMD_SUGGESTION_PARAM_HELP)));
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		object itemData = menuItem.ItemData;
		string playerUid = itemData as string;
		if (playerUid != null)
		{
			string text = "";
			PlayerState playerState = LatentPlayers.All.FirstOrDefault((PlayerState p) => p.Uid == playerUid);
			text = ((playerState == null) ? ("~c~" + playerUid) : playerState.Name);
			if (Unblock(playerUid))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.UNBLOCK_PLAYER, text));
				menu.RemoveMenuItem(menuItem);
				blockMenu.CounterPreText = LocalizationController.S(Entries.Imenu.IMENU_BLOCKED_PLAYERS_PRETEXT, blockedPlayers.Count);
			}
			else
			{
				Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.UNABLE_UNBLOCK_PLAYER, text));
			}
		}
	}

	public static void OpenBlockMenu()
	{
		blockMenu.ClearMenuItems();
		foreach (string playerUid in blockedPlayers)
		{
			string text = "";
			PlayerState playerState = LatentPlayers.All.FirstOrDefault((PlayerState p) => p.Uid == playerUid);
			bool flag = playerState != null;
			text = ((!flag) ? ("~c~" + playerUid) : (playerState.ColorTextCode + playerState.Name));
			MenuItem menuItem = new MenuItem(text, LocalizationController.S(Entries.Imenu.IMENU_BLOCKED_PLAYERS_MENUITEM_DESCRIPTION, text));
			menuItem.Label = (flag ? $"{playerState.ColorTextCode}{playerState.Id}" : ("~c~" + LocalizationController.S(Entries.Main.OFFLINE)));
			MenuItem menuItem2 = menuItem;
			menuItem2.ItemData = playerUid;
			blockMenu.AddMenuItem(menuItem2);
		}
		if (blockedPlayers.Count == 0)
		{
			blockMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_BLOCKED_PLAYERS_EMPTY_MENUITEM_TEXT), LocalizationController.S(Entries.Imenu.IMENU_BLOCKED_PLAYERS_EMPTY_MENUITEM_DESCRIPTION)));
		}
		blockMenu.CounterPreText = LocalizationController.S(Entries.Imenu.IMENU_BLOCKED_PLAYERS_PRETEXT, blockedPlayers.Count);
		blockMenu.OpenMenu();
	}

	[Command("block")]
	private void BlockCommand(string[] args)
	{
		if (args.Length < 1)
		{
			OpenBlockMenu();
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.CMD_USAGE, "/block [" + LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_PARAM_NAME) + "]"));
			return;
		}
		PlayerState playerState = LatentPlayers.Get(result);
		if (playerState == null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, Gtacnr.Utils.RemoveGta5TextFormatting(LocalizationController.S(Entries.Imenu.IMENU_HELP_PLAYER_NOT_CONNECTED)));
			return;
		}
		if (blockedPlayers.Contains(playerState.Uid))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.BLOCK_CMD_ERROR_ALREADY_BLOCKED));
			return;
		}
		Block(playerState.Uid);
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Main.BLOCK_CMD_SUCCESS, playerState.Name));
	}

	[Command("unblock")]
	private void UnblockCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.CMD_USAGE, "/unblock [" + LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_PARAM_NAME) + "]"));
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.CMD_USAGE, "/unblock [" + LocalizationController.S(Entries.Main.BLOCK_CMD_SUGGESTION_PARAM_NAME) + "]"));
			return;
		}
		PlayerState playerState = LatentPlayers.Get(result);
		if (playerState == null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, Gtacnr.Utils.RemoveGta5TextFormatting(LocalizationController.S(Entries.Imenu.IMENU_HELP_PLAYER_NOT_CONNECTED)));
			return;
		}
		if (!blockedPlayers.Contains(playerState.Uid))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.UNBLOCK_CMD_ERROR_NOT_BLOCKED));
			return;
		}
		Unblock(playerState.Uid);
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Main.UNBLOCK_CMD_SUCCESS, playerState.Name));
	}

	private static void UpdateBlockedPlayers()
	{
		API.SetResourceKvp("blocked_uids", blockedPlayers.Json());
		BaseScript.TriggerEvent("gtacnr:chat:blockedUpdated", new object[0]);
	}

	public static bool Block(string playerUid)
	{
		if (blockedPlayers.Contains(playerUid))
		{
			return false;
		}
		blockedPlayers.Add(playerUid);
		UpdateBlockedPlayers();
		return true;
	}

	public static bool Unblock(string playerUid)
	{
		if (!blockedPlayers.Contains(playerUid))
		{
			return false;
		}
		blockedPlayers.Remove(playerUid);
		UpdateBlockedPlayers();
		return true;
	}

	public static bool IsBlocked(string playerUid)
	{
		return blockedPlayers.Contains(playerUid);
	}

	[EventHandler("gtacnr:chat:ready")]
	private void OnChatReady()
	{
		BaseScript.TriggerEvent("gtacnr:chat:blockedUpdated", new object[0]);
	}
}
