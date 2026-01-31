using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Crews.Creation;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.Model.PrefixedGUIDs;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Crews;

public sealed class CrewScript : Script
{
	public class CrewChangedEventArgs
	{
		public CrewId? PreviousCrewId { get; set; }

		public CrewId? NewCrewId { get; set; }
	}

	private static readonly Dictionary<Guid, Crew> CrewsDataCache = new Dictionary<Guid, Crew>();

	private static Vector3 CrewManagementLocation = new Vector3(-259.374f, -701.1476f, 33.1765f);

	private static Blip crewBlip;

	private static bool isCloseToLocation = false;

	private static bool CanOpenCrewMenu = false;

	private static Menu MainMenu = new Menu("Crews", "Crew Menu");

	private static MenuItem JoinMenuItem = new MenuItem("Join a Crew", "Ask to join a crew that is currently online and is accepting applications.");

	private static MenuItem CreateMenuItem = new MenuItem("Create a Crew", "Create your very own crew!");

	private static MenuItem InfoMenuItem = new MenuItem("~b~Information");

	private static MenuItem CopyIdMenuItem = new MenuItem("Copy Crew Id");

	private static MenuItem ManageApplicationsMenuItem = new MenuItem("Manage Applications");

	private static MenuItem MembersMenuItem = new MenuItem("Manage Members", "Manage members whose ~y~rank ~s~is lower than yours.");

	private static MenuItem EditMenuItem = new MenuItem("Edit Crew Info", "Edit crew information such as ~b~name ~s~and ~b~description~s~.");

	private static MenuItem CrewLogsMenuItem = new MenuItem("Crew Logs", "View all crew related logs.");

	private static MenuItem LeaveMenuItem = new MenuItem("~r~Leave", "Leave your crew ~r~permanently~s~. Unless you're invited or accepted again, you won't be able to join back.");

	private static MenuItem DeleteMenuItenm = new MenuItem("~r~Delete Crew", "Delete your crew ~r~permanently~s~. There's ~r~no way ~s~to recover the crew once you've done this.");

	public static bool CrewDataLoaded { get; private set; } = false;

	public static Crew? CrewData { get; private set; } = null;

	public static CrewMemberData? MemberData { get; private set; } = null;

	public static event EventHandler<CrewChangedEventArgs>? OnCrewChanged;

	public static Tuple<string, AcronymStyleData>? GetCrewAcronymData(CrewId crewId)
	{
		if (CrewsDataCache.TryGetValue((Guid)crewId, out Crew value))
		{
			return Tuple.Create(value.Acronym, value.AcronymStyle);
		}
		return null;
	}

	public CrewScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.PlaySelectSound = false;
		MainMenu.OnItemSelect += OnMainMenuItemSelect;
	}

	protected override void OnStarted()
	{
		CreateBlip();
		LoadCrewsData();
		Chat.AddSuggestion("/crew", "Open the crew menu.");
	}

	private async void LoadCrewsData()
	{
		foreach (KeyValuePair<CrewId, Crew> item in (await TriggerServerEventAsync<string>("gtacnr:crews:getOnline", new object[0])).Unjson<List<Crew>>().ToDictionary((Crew e) => e.Id))
		{
			CrewsDataCache.Add((Guid)item.Key, item.Value);
		}
		foreach (PlayerState item2 in LatentPlayers.All)
		{
			item2.CrewId = item2.CrewId;
		}
	}

	[EventHandler("gtacnr:crews:crewDataLoaded")]
	private void OnCrewDataLoaded(string jCrewData)
	{
		Crew crew = jCrewData.Unjson<Crew>();
		CrewsDataCache[(Guid)crew.Id] = crew;
		RefreshPlayerStateUsernames();
	}

	private void RefreshPlayerStateUsernames()
	{
		foreach (PlayerState item in LatentPlayers.All)
		{
			item.CrewId = item.CrewId;
		}
	}

	[EventHandler("gtacnr:crews:crewData:updateField")]
	private void OnUpdateField(byte[] crewIdBytes, byte fieldByte, byte[] serializedValue)
	{
		CrewDataField field = (CrewDataField)fieldByte;
		Guid crewId = new Guid(crewIdBytes);
		UpdateField(crewId, field, serializedValue);
	}

	private void UpdateField(Guid crewId, CrewDataField field, byte[] serializedValue)
	{
		if (CrewsDataCache.TryGetValue(crewId, out Crew value))
		{
			switch (field)
			{
			case CrewDataField.AcronymStyle:
			{
				AcronymStyle style = (AcronymStyle)SyncedDataSerialization.DeserializeByte(serializedValue);
				value.AcronymStyle.Style = style;
				RefreshPlayerStateUsernames();
				break;
			}
			case CrewDataField.AcronymSeparator:
			{
				AcronymStyleSeparator separator = (AcronymStyleSeparator)SyncedDataSerialization.DeserializeByte(serializedValue);
				value.AcronymStyle.Separator = separator;
				RefreshPlayerStateUsernames();
				break;
			}
			}
		}
	}

	[EventHandler("gtacnr:crews:setCrewData")]
	private void OnSetCrewData(string jCrewData, string jMemberData)
	{
		CrewDataLoaded = true;
		if (!string.IsNullOrEmpty(jCrewData) && !string.IsNullOrEmpty(jMemberData))
		{
			Crew crewData = CrewData;
			CrewData = jCrewData.Unjson<Crew>();
			MemberData = jMemberData.Unjson<CrewMemberData>();
			CrewScript.OnCrewChanged?.Invoke(this, new CrewChangedEventArgs
			{
				PreviousCrewId = crewData?.Id,
				NewCrewId = CrewData.Id
			});
		}
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		if (CrewData != null && MemberData != null)
		{
			Print($"You are member of {CrewData.Acronym} | Rank: {MemberData.Rank} ({MemberData.GetRankName(CrewData)})");
		}
	}

	[EventHandler("gtacnr:crews:removeCrewData")]
	private void OnRemoveCrewData()
	{
		Crew crewData = CrewData;
		CrewData = null;
		MemberData = null;
		Print("You are no longer member of any crew.");
		CrewScript.OnCrewChanged?.Invoke(this, new CrewChangedEventArgs
		{
			PreviousCrewId = crewData?.Id,
			NewCrewId = null
		});
	}

	private void CreateBlip()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		crewBlip = World.CreateBlip(CrewManagementLocation);
		crewBlip.Sprite = (BlipSprite)437;
		crewBlip.Color = (BlipColor)83;
		Utils.SetBlipName(crewBlip, "Crews");
		crewBlip.Scale = 0.9f;
		crewBlip.IsShortRange = true;
	}

	[Update]
	private async Coroutine CrewPosDistanceTask()
	{
		await BaseScript.Delay(1000);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = ((Vector3)(ref position)).DistanceToSquared(CrewManagementLocation);
		bool flag = isCloseToLocation;
		bool canOpenCrewMenu = CanOpenCrewMenu;
		isCloseToLocation = num < 625f;
		CanOpenCrewMenu = num < 2.25f;
		if (isCloseToLocation && !flag)
		{
			base.Update += DrawCrewMarkerTask;
		}
		else if (!isCloseToLocation && flag)
		{
			base.Update -= DrawCrewMarkerTask;
		}
		if (CanOpenCrewMenu && !canOpenCrewMenu)
		{
			KeysScript.AttachListener((Control)51, OnKeyEvent);
			Utils.AddInstructionalButton("crewMenu", new InstructionalButton("Crew Menu", 2, (Control)51));
		}
		else if (!CanOpenCrewMenu && canOpenCrewMenu)
		{
			KeysScript.DetachListener((Control)51, OnKeyEvent);
			Utils.RemoveInstructionalButton("crewMenu");
		}
	}

	private bool OnKeyEvent(Control ctrl, KeyEventType eventType, InputType inputType)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (!CanOpenCrewMenu || (int)ctrl != 51 || eventType != KeyEventType.JustPressed || MainMenu.Visible)
		{
			return false;
		}
		OpenCrewMenu(remote: false);
		return true;
	}

	private async Coroutine DrawCrewMarkerTask()
	{
		World.DrawMarker((MarkerType)1, CrewManagementLocation, Vector3.Zero, Vector3.Zero, new Vector3(1f, 1f, 0.75f), System.Drawing.Color.FromArgb(-2135228416), false, false, false, (string)null, (string)null, false);
	}

	private async void OpenCrewMenu(bool remote)
	{
		MainMenu.OpenMenu();
		MainMenu.ClearMenuItems();
		if (CrewData == null)
		{
			MainMenu.AddLoadingMenuItem();
			MainMenu.ClearMenuItems();
			Gtacnr.Utils.GetLevelByXP(await Users.GetXP());
			await Money.GetBalance(AccountType.Bank);
			MainMenu.MenuTitle = "Crews";
			MainMenu.MenuSubtitle = "Crew Menu";
			MainMenu.AddMenuItem(JoinMenuItem);
			MenuController.BindMenuItem(MainMenu, CrewJoinMenuScript.MainMenu, JoinMenuItem);
			CreateMenuItem.Enabled = !remote;
			MainMenu.AddMenuItem(CreateMenuItem);
			MenuController.BindMenuItem(MainMenu, CrewCreationMenuScript.MainMenu, CreateMenuItem);
			return;
		}
		MainMenu.MenuTitle = CrewData.Acronym;
		MainMenu.MenuSubtitle = CrewData.Name;
		MainMenu.AddMenuItem(InfoMenuItem);
		InfoMenuItem.Description = "~b~Motto: ~s~" + CrewData.Motto + "~n~" + $"~b~Your rank: ~s~{MemberData.Rank} ({MemberData.GetRankName(CrewData)})~n~" + $"~b~Crew score: ~s~{CrewData.Score}~n~" + "~b~Created: ~s~" + (CrewData.CreationDateTime?.ToString("MMMM dd, yyyy") ?? "Unknown");
		MainMenu.AddMenuItem(CopyIdMenuItem);
		Print($"Permissions: {(uint)MemberData.Permissions}");
		bool num = MemberData.Rank == 99;
		if (num || MemberData.Permissions.HasAll(CrewPermissions.AddMembers))
		{
			MainMenu.AddMenuItem(ManageApplicationsMenuItem);
			MenuController.BindMenuItem(MainMenu, CrewApplicationsMenuScript.MainMenu, ManageApplicationsMenuItem);
		}
		if (num || MemberData.Permissions.HasAny(CrewPermissions.RemoveMembers | CrewPermissions.PromoteDemoteMembers | CrewPermissions.ManagePermissions))
		{
			MainMenu.AddMenuItem(MembersMenuItem);
			MenuController.BindMenuItem(MainMenu, CrewMembersMenuScript.MainMenu, MembersMenuItem);
		}
		if (num || MemberData.Permissions.HasAll(CrewPermissions.ManageCrewInfo))
		{
			MainMenu.AddMenuItem(EditMenuItem);
			MenuController.BindMenuItem(MainMenu, CrewInfoManagementMenuScript.MainMenu, EditMenuItem);
		}
		if (num)
		{
			MainMenu.AddMenuItem(CrewLogsMenuItem);
			MenuController.BindMenuItem(MainMenu, CrewLogsMenuScript.MainMenu, CrewLogsMenuItem);
			MainMenu.AddMenuItem(DeleteMenuItenm);
		}
		else
		{
			MainMenu.AddMenuItem(LeaveMenuItem);
		}
	}

	private async void OnMainMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (!IsSelected(InfoMenuItem))
		{
			Utils.PlaySelectSound();
		}
		if (IsSelected(CopyIdMenuItem))
		{
			if (CrewData != null)
			{
				await Utils.GetUserInput("Crew Id", "", "", 2048, "text", ((Guid)CrewData.Id).ToString());
			}
		}
		else if (IsSelected(DeleteMenuItenm))
		{
			string cancelMsg = "Operation ~r~canceled~s~. The crew was not deleted.";
			if (!(await Utils.ShowConfirm("You are about to ~r~PERMANENTLY DELETE ~s~your crew ~y~" + CrewData.Acronym + "~s~.\nThere's no way to recover it once this has been done, and the staff will ~r~NOT ~s~be able to help you. Do you want to proceed?")))
			{
				Utils.SendNotification(cancelMsg);
				return;
			}
			string answer;
			string text;
			do
			{
				answer = Gtacnr.Utils.GenerateAsciiString(6);
				text = await Utils.GetUserInput("Delete Crew", "Type " + answer + " (case sensitive) to DELETE " + CrewData.Acronym + " permanently.", "", 6);
				if (text == null)
				{
					Utils.SendNotification(cancelMsg);
					return;
				}
			}
			while (text != answer);
			CrewsDeleteResponse crewsDeleteResponse = (CrewsDeleteResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:delete", new object[0]));
			if (crewsDeleteResponse == CrewsDeleteResponse.Success)
			{
				Utils.DisplayHelpText("~y~" + CrewData.Acronym + " ~s~was successfully ~r~deleted~s~.");
			}
			else
			{
				Utils.DisplayErrorMessage(0, -1, $"Unable to delete crew. {crewsDeleteResponse}");
				Utils.SendNotification(cancelMsg);
			}
			MenuController.CloseAllMenus();
		}
		else if (IsSelected(LeaveMenuItem))
		{
			if (!(await Utils.ShowConfirm("Are you sure that you want to leave your crew ~r~permanently~s~? Unless you're invited or accepted again, you won't be able to join back.")))
			{
				Utils.PlayErrorSound();
				return;
			}
			BaseScript.TriggerServerEvent("gtacnr:crews:leave", new object[0]);
			MenuController.CloseAllMenus();
		}
		bool IsSelected(MenuItem keyItem)
		{
			return keyItem == menuItem;
		}
	}

	[EventHandler("gtacnr:crews:joinRequested")]
	private void OnCrewJoinRequested(int playerId, string intro)
	{
		if (CrewData != null)
		{
			PlayerState playerState = LatentPlayers.Get(playerId);
			Utils.DisplayHelpText(playerState.ColorNameAndId + " wants to join " + CrewData.Acronym + "!");
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"{playerState} wants to join {CrewData.Acronym}.");
		}
	}

	[Command("crew")]
	private void OnCrewCommand()
	{
		OpenCrewMenu(remote: true);
	}
}
