using System;
using System.Collections.Generic;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.PrefixedGUIDs;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Crews;

public sealed class CrewMembersMenuScript : Script
{
	private static Menu MemberActions = new Menu("Actions");

	private static CrewMemberData? SelectedMember;

	private static MenuItem ChangeRankItem = new MenuItem("Change Rank");

	private static MenuItem ManagePermissionsItem = new MenuItem("Manage Permissions");

	private static MenuItem KickItem = new MenuItem("~r~Kick", "~r~Kick~s~ a member whose rank is lower than yours from the crew.");

	private static MenuItem TransferOwnershipItem = new MenuItem("Transfer Ownership", "Transfer the ownership of the crew to someone else. ~r~Warning: ~s~once you've transferred your crew ownership, there's no way for you to get it back unless they transfer it back to you.");

	private static Menu RanksMenu = new Menu("Set Rank");

	private static Menu PermissionsMenu = new Menu("Manage Permissions");

	private static List<CrewMemberData>? cachedMembersData;

	private static CrewId? cachedCrewId;

	private static bool isBusy = false;

	public static Menu MainMenu { get; private set; } = new Menu("Crew Members", "Crew Members Menu");

	public CrewMembersMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.OnMenuOpen += OnMainMenuOpen;
		MainMenu.OnItemSelect += OnMainItemSelect;
		MenuController.AddMenu(MemberActions);
		MemberActions.OnMenuOpen += OnActionsMenuOpen;
		MemberActions.OnItemSelect += OnActionsItemSelect;
		MemberActions.AddMenuItem(ChangeRankItem);
		MemberActions.AddMenuItem(ManagePermissionsItem);
		MemberActions.AddMenuItem(KickItem);
		MenuController.AddMenu(RanksMenu);
		MenuController.BindMenuItem(MemberActions, RanksMenu, ChangeRankItem);
		RanksMenu.OnItemSelect += OnRanksItemSelect;
		RanksMenu.OnMenuOpen += OnRanksMenuOpen;
		MenuController.AddMenu(PermissionsMenu);
		MenuController.BindMenuItem(MemberActions, PermissionsMenu, ManagePermissionsItem);
		PermissionsMenu.OnMenuOpen += OnPermissionsMenuOpen;
		PermissionsMenu.OnMenuClose += OnPermissionsMenuClose;
		LocalizationController.LanguageChanged += OnLanguageChanged;
	}

	private async void OnActionsItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == KickItem)
		{
			if (isBusy)
			{
				Utils.PlayErrorSound();
				return;
			}
			string text = SelectedMember.Username ?? "Deleted User";
			if (!(await Utils.ShowConfirm("You are about to ~r~kick~s~ " + text + " from your crew.\n")))
			{
				Utils.PlayErrorSound();
				return;
			}
			string text2 = await Utils.GetUserInput("Kick Member", "Enter a reason for kicking the member (optional):", "", 100);
			CrewsKickMemberResponse crewsKickMemberResponse;
			try
			{
				isBusy = true;
				crewsKickMemberResponse = (CrewsKickMemberResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:kickMember", new object[2] { SelectedMember.UserId, text2 }));
			}
			finally
			{
				isBusy = false;
			}
			if (crewsKickMemberResponse != CrewsKickMemberResponse.Success)
			{
				Utils.DisplayErrorMessage(0, -1, $"An unknown error occurred while trying to kick the member: {crewsKickMemberResponse}");
				return;
			}
			cachedMembersData.Remove(SelectedMember);
			menu.GoBack();
		}
		else
		{
			if (menuItem != TransferOwnershipItem)
			{
				return;
			}
			if (isBusy)
			{
				Utils.PlayErrorSound();
				return;
			}
			string text3 = SelectedMember.Username ?? "Deleted User";
			if (!(await Utils.ShowConfirm("You are about to transfer ownership of the crew to " + text3 + ".\nYou will lose all ownership privileges. Are you sure?")))
			{
				Utils.PlayErrorSound();
				return;
			}
			CrewsTransferOwnershipResponse crewsTransferOwnershipResponse;
			try
			{
				isBusy = true;
				crewsTransferOwnershipResponse = (CrewsTransferOwnershipResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:transferOwnership", new object[1] { SelectedMember.UserId }));
			}
			finally
			{
				isBusy = false;
			}
			if (crewsTransferOwnershipResponse != CrewsTransferOwnershipResponse.Success)
			{
				Utils.DisplayErrorMessage(0, -1, $"An unknown error occurred while trying to transfer ownership: {crewsTransferOwnershipResponse}");
				return;
			}
			CrewScript.MemberData.Rank = 0;
			CrewScript.MemberData.Permissions = CrewPermissions.None;
			MenuController.CloseAllMenus();
		}
	}

	private async void OnPermissionsMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		CrewPermissions newPermissions = CrewPermissions.None;
		foreach (MenuItem menuItem in PermissionsMenu.GetMenuItems())
		{
			CrewPermissions crewPermissions = (CrewPermissions)menuItem.ItemData;
			if (((MenuCheckboxItem)menuItem).Checked)
			{
				newPermissions |= crewPermissions;
			}
		}
		if (newPermissions == SelectedMember.Permissions)
		{
			return;
		}
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		CrewsSetPermissionsResponse crewsSetPermissionsResponse;
		try
		{
			isBusy = true;
			crewsSetPermissionsResponse = (CrewsSetPermissionsResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:setMemberPermissions", new object[2]
			{
				SelectedMember.UserId,
				(uint)newPermissions
			}));
		}
		finally
		{
			isBusy = false;
		}
		if (crewsSetPermissionsResponse != CrewsSetPermissionsResponse.Success)
		{
			Utils.DisplayErrorMessage(0, -1, $"An unknown error occurred while trying to change the member's permissions: {crewsSetPermissionsResponse}");
		}
		else
		{
			SelectedMember.Permissions = newPermissions;
		}
	}

	private void OnPermissionsMenuOpen(Menu menu)
	{
		PermissionsMenu.ClearMenuItems();
		CrewPermissions[] array = (CrewPermissions[])Enum.GetValues(typeof(CrewPermissions));
		foreach (CrewPermissions crewPermissions in array)
		{
			if (crewPermissions != CrewPermissions.None)
			{
				bool flag = SelectedMember.Permissions.HasAll(crewPermissions);
				MenuCheckboxItem menuCheckboxItem = new MenuCheckboxItem($"{crewPermissions}")
				{
					Checked = flag,
					ItemData = crewPermissions
				};
				menuCheckboxItem.Enabled = CrewScript.MemberData.Rank == 99 || (CrewScript.MemberData.Permissions.HasAll(CrewPermissions.ManagePermissions) && CrewScript.MemberData.Permissions.HasAll(crewPermissions));
				PermissionsMenu.AddMenuItem(menuCheckboxItem);
			}
		}
	}

	private void OnRanksMenuOpen(Menu menu)
	{
		menu.GetMenuItems().ForEach(delegate(MenuItem i)
		{
			i.Enabled = (int)i.ItemData != SelectedMember.Rank;
		});
	}

	private async void OnRanksItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		int newRank = (int)menuItem.ItemData;
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		CrewsSetRankResponse crewsSetRankResponse;
		try
		{
			isBusy = true;
			crewsSetRankResponse = (CrewsSetRankResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:setMemberRank", new object[2] { SelectedMember.UserId, newRank }));
		}
		finally
		{
			isBusy = false;
		}
		if (crewsSetRankResponse != CrewsSetRankResponse.Success)
		{
			Utils.DisplayErrorMessage(0, -1, $"An unknown error occurred while trying to change the member's rank: {crewsSetRankResponse}");
			return;
		}
		SelectedMember.Rank = newRank;
		menu.GetMenuItems().ForEach(delegate(MenuItem i)
		{
			i.Enabled = (int)i.ItemData != SelectedMember.Rank;
		});
	}

	private void OnActionsMenuOpen(Menu menu)
	{
		MemberActions.MenuSubtitle = SelectedMember.Username ?? "Deleted User";
		bool flag = CrewScript.MemberData.Rank == 99;
		ChangeRankItem.Enabled = flag || CrewScript.MemberData.Permissions.HasAll(CrewPermissions.PromoteDemoteMembers);
		ManagePermissionsItem.Enabled = flag || CrewScript.MemberData.Permissions.HasAll(CrewPermissions.ManagePermissions);
		KickItem.Enabled = flag || CrewScript.MemberData.Permissions.HasAll(CrewPermissions.RemoveMembers);
		if (SelectedMember.UserId == CrewScript.MemberData.UserId || SelectedMember.Rank >= CrewScript.MemberData.Rank)
		{
			ChangeRankItem.Enabled = false;
			ManagePermissionsItem.Enabled = false;
			KickItem.Enabled = false;
		}
		menu.RemoveMenuItem(TransferOwnershipItem);
		if (flag && SelectedMember.Rank != 99)
		{
			menu.AddMenuItem(TransferOwnershipItem);
		}
	}

	private void OnMainItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		SelectedMember = menuItem.ItemData as CrewMemberData;
	}

	private async void OnMainMenuOpen(Menu menu)
	{
		try
		{
			MainMenu.ClearMenuItems();
			MainMenu.AddLoadingMenuItem();
			List<CrewMemberData> list;
			if (cachedCrewId == CrewScript.CrewData.Id && cachedMembersData != null)
			{
				list = cachedMembersData;
			}
			else
			{
				string text = await TriggerServerEventAsync<string>("gtacnr:crews:getMembers", new object[0]);
				if (text == null)
				{
					AddNoMembersItem();
					return;
				}
				list = (cachedMembersData = text.Unjson<List<CrewMemberData>>());
				cachedCrewId = CrewScript.CrewData.Id;
			}
			MainMenu.ClearMenuItems();
			if (list.Count == 0)
			{
				AddNoMembersItem();
				return;
			}
			foreach (CrewMemberData item in list)
			{
				MenuItem menuItem = new MenuItem((item.Username ?? "~c~Deleted User") ?? "")
				{
					Label = $"{item.Rank} ({item.GetRankName(CrewScript.CrewData)})",
					ItemData = item
				};
				MainMenu.AddMenuItem(menuItem);
				MenuController.BindMenuItem(MainMenu, MemberActions, menuItem);
			}
			RanksMenu.ClearMenuItems();
			if (CrewScript.CrewData.RankData == null || CrewScript.CrewData.RankData.RankNames == null || CrewScript.CrewData.RankData.RankNames.Count == 0)
			{
				for (int i = 0; i <= 4; i++)
				{
					AddRankItem(i, CrewRankData.GetDefaultRankName(i));
				}
				return;
			}
			foreach (KeyValuePair<int, string> rankName in CrewScript.CrewData.RankData.RankNames)
			{
				if (rankName.Key != 99)
				{
					AddRankItem(rankName.Key, rankName.Value);
				}
			}
		}
		catch (Exception ex)
		{
			Print(ex);
			MainMenu.ClearMenuItems();
			MainMenu.AddErrorMenuItem(ex);
		}
		static void AddNoMembersItem()
		{
			MainMenu.AddMenuItem(new MenuItem("No members :(", "Your crew doesn't have any member."));
		}
		static void AddRankItem(int item, string name)
		{
			RanksMenu.AddMenuItem(new MenuItem($"{item} ({name})")
			{
				ItemData = item
			});
		}
	}

	private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
	{
	}
}
