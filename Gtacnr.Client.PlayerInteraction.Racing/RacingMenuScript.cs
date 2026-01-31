using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.PlayerInteraction.Racing;

public class RacingMenuScript : Script
{
	private static readonly Menu MainMenu = new Menu(LocalizationController.S(Entries.Player.RACING_MENU_MAIN));

	private static readonly MenuItem EditRacesItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_EDIT_TRACKS));

	private static MenuListItem SelectRaceItem = new MenuListItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_SELECT_TRACK), Enumerable.Empty<string>());

	private static readonly MenuListItem LapsItem = new MenuListItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_LAPS), new uint[5] { 1u, 2u, 3u, 4u, 5u }.Select((uint i) => i.ToString()));

	private static readonly MenuItem BetItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_BET));

	private static readonly MenuItem RegisterRaceItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_REGISTER_RACE));

	private static readonly MenuItem InvitePlayersItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_INVITE_PLAYERS));

	private static readonly MenuItem StartRaceItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_START_RACE));

	private static MenuListItem CheckpointsItem;

	private static readonly MenuItem JoinRaceItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_JOIN_RACE));

	private static readonly MenuItem LeaveRaceItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_LEAVE_RACE));

	private static int invitedBy = -1;

	private static ulong bet = 0uL;

	private static RaceTrack? selectedRaceTrack = null;

	private static Blip? _raceStartBlip;

	private static uint selectedLaps = 1u;

	private static HashSet<int> playersInvited = new HashSet<int>();

	private static List<Blip> checkPointBlips = new List<Blip>();

	private static bool m_editor = false;

	private static bool skip_reset = false;

	private static bool isBusy = false;

	private static Blip? RaceStartBlip
	{
		get
		{
			return _raceStartBlip;
		}
		set
		{
			Blip? raceStartBlip = _raceStartBlip;
			if (raceStartBlip != null)
			{
				((PoolObject)raceStartBlip).Delete();
			}
			_raceStartBlip = value;
		}
	}

	public RacingMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.OnMenuOpen += OnMenuOpen;
		MainMenu.OnMenuClose += OnMenuClose;
		MainMenu.OnItemSelect += OnItemSelect;
		MainMenu.OnIndexChange += OnIndexChange;
		MainMenu.OnListIndexChange += OnListIndexChange;
		RegisterRaceItem.Enabled = true;
		InvitePlayersItem.Enabled = false;
		StartRaceItem.Enabled = false;
		RacingScript.LeftStartingPosition += OnLeftStartingPosition;
		LocalizationController.LanguageChanged += OnLanguageChanged;
		Chat.AddSuggestion("/racing", "Opens the racing menu.");
	}

	private void OnLeftStartingPosition(object sender, EventArgs e)
	{
		if (MainMenu.Visible)
		{
			if (StartRaceItem.Enabled)
			{
				BaseScript.TriggerServerEvent("gtacnr:racing:leave", new object[0]);
			}
			MainMenu.CloseMenu();
		}
	}

	public static void ShowMenu(bool editor, Menu previousMenu = null)
	{
		m_editor = editor;
		MainMenu.ClearMenuItems();
		if (previousMenu != null)
		{
			MenuController.AddSubmenu(previousMenu, MainMenu);
		}
		else
		{
			MainMenu.ParentMenu = null;
		}
		if (RacingScript.IsInRace || RacingScript.StartingPosition.HasValue)
		{
			MainMenu.AddMenuItem(LeaveRaceItem);
		}
		else if (editor)
		{
			MainMenu.AddMenuItem(EditRacesItem);
			MainMenu.AddMenuItem(SelectRaceItem);
			MainMenu.AddMenuItem(LapsItem);
			MainMenu.AddMenuItem(BetItem);
			MainMenu.AddMenuItem(RegisterRaceItem);
			MainMenu.AddMenuItem(InvitePlayersItem);
			MainMenu.AddMenuItem(StartRaceItem);
		}
		else
		{
			MainMenu.AddMenuItem(CheckpointsItem);
			MainMenu.AddMenuItem(LapsItem);
			MainMenu.AddMenuItem(BetItem);
			MainMenu.AddMenuItem(JoinRaceItem);
		}
		if (MenuController.IsAnyMenuOpen())
		{
			MenuController.CloseAllMenus();
		}
		MainMenu.OpenMenu();
	}

	private void OnMenuOpen(Menu menu)
	{
		if (!skip_reset)
		{
			Reset();
		}
		skip_reset = false;
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		RaceStartBlip = null;
		checkPointBlips.ForEach(delegate(Blip b)
		{
			((PoolObject)b).Delete();
		});
		checkPointBlips.Clear();
		if (!skip_reset && m_editor && !RacingScript.IsInRace)
		{
			BaseScript.TriggerServerEvent("gtacnr:racing:abandonRace", new object[0]);
		}
	}

	private void Reset()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		EditRacesItem.Enabled = true;
		SelectRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_SELECT_TRACK);
		RaceStartBlip = null;
		SelectRaceItem.ListItems = RaceEditorMenuScript.GetRaceTrackNames();
		SelectRaceItem.ListIndex = 0;
		SelectRaceItem.Enabled = true;
		selectedRaceTrack = RaceEditorMenuScript.GetRaceTrackAtIndex(0);
		if (selectedRaceTrack != null)
		{
			RaceStartBlip = CreateStartBlip(selectedRaceTrack.Checkpoints[0]);
		}
		LapsItem.Enabled = true;
		selectedLaps = 1u;
		BetItem.Enabled = true;
		RegisterRaceItem.Enabled = true;
		InvitePlayersItem.Enabled = false;
		StartRaceItem.Enabled = false;
		playersInvited.Clear();
	}

	private static Blip CreateStartBlip(Vector3 checkpointPos)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Blip obj = World.CreateBlip(selectedRaceTrack.Checkpoints[0]);
		obj.Sprite = RaceEditorMenuScript.FinishBlipSprite;
		obj.Scale = 1f;
		return obj;
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu != MainMenu)
		{
			return;
		}
		if (menuItem == EditRacesItem)
		{
			RaceStartBlip = null;
			RaceEditorMenuScript.ShowMenu(MainMenu);
		}
		else
		{
			if (menuItem == BetItem)
			{
				if (isBusy)
				{
					return;
				}
				try
				{
					isBusy = true;
					if (await Money.GetCachedBalanceOrFetch(AccountType.Cash) <= 0)
					{
						return;
					}
					ulong amount = 0uL;
					string text = await Utils.GetUserInput(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_BET) + ": ", "", $"{amount:0.##}", 12, "number");
					if (text != null)
					{
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
						}
						else
						{
							bet = amount;
							BetItem.Label = "~g~" + bet.ToCurrencyString();
						}
					}
					return;
				}
				finally
				{
					isBusy = false;
				}
			}
			Vector3 val;
			if (menuItem == InvitePlayersItem)
			{
				skip_reset = true;
				Ped playerPed = Game.PlayerPed;
				Vector3 val2;
				if (playerPed == null)
				{
					val = default(Vector3);
					val2 = val;
				}
				else
				{
					val2 = ((Entity)playerPed).Position;
				}
				Vector3 currentPos = val2;
				PlayerListMenu.ShowMenu(MainMenu, LatentPlayers.All.Where(delegate(PlayerState ps)
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_000a: Unknown result type (might be due to invalid IL or missing references)
					Vector3 position = ps.Position;
					return ((Vector3)(ref position)).DistanceToSquared2D(currentPos) <= 25f.Square();
				}), async delegate(Menu m, int playerId)
				{
					if (!playersInvited.Contains(playerId))
					{
						MenuItem menuItem2 = m.GetCurrentMenuItem();
						menuItem2.LeftIcon = MenuItem.Icon.INV_STEER_WHEEL;
						playersInvited.Add(playerId);
						RacingSystemResponse racingSystemResponse3 = (RacingSystemResponse)(await TriggerServerEventAsync<int>("gtacnr:racing:invite", new object[1] { playerId }));
						if (racingSystemResponse3 != RacingSystemResponse.Success)
						{
							playersInvited.Remove(playerId);
							menuItem2.LeftIcon = MenuItem.Icon.NONE;
							Utils.DisplayErrorMessage(0, -1, "Failed to invite player: " + racingSystemResponse3);
						}
					}
				}, delegate(MenuItem menuItem2, int playerId)
				{
					if (playersInvited.Contains(playerId))
					{
						menuItem2.LeftIcon = MenuItem.Icon.INV_STEER_WHEEL;
					}
				}, exceptMe: true);
			}
			else if (menuItem == RegisterRaceItem)
			{
				if (selectedRaceTrack == null)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_NO_TRACK_SELECTED));
					Utils.PlayErrorSound();
					return;
				}
				if (selectedRaceTrack.Checkpoints.Count < 2)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_TRACK_TOO_FEW_CHECKPOINTS, 2));
					Utils.PlayErrorSound();
					return;
				}
				Ped playerPed2 = Game.PlayerPed;
				Vector3 val3;
				if (playerPed2 == null)
				{
					val = default(Vector3);
					val3 = val;
				}
				else
				{
					val3 = ((Entity)playerPed2).Position;
				}
				Vector3 val4 = val3;
				val = selectedRaceTrack.Checkpoints[0];
				if (((Vector3)(ref val)).DistanceToSquared(val4) > 400f)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_TOO_FAR_FROM_FIRST_CHECKPOINT));
					Utils.PlayErrorSound();
					return;
				}
				if (selectedLaps > 1)
				{
					Vector3 val5 = selectedRaceTrack.Checkpoints[0];
					Vector3 val6 = selectedRaceTrack.Checkpoints.Last();
					if (((Vector3)(ref val5)).DistanceToSquared(val6) > 1000000f)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_LAP_DISTANCE_TOO_FAR));
						Utils.PlayErrorSound();
						return;
					}
				}
				if (!isBusy)
				{
					try
					{
						isBusy = true;
						selectedRaceTrack.Laps = selectedLaps;
						RacingSystemResponse racingSystemResponse = (RacingSystemResponse)(await TriggerServerEventAsync<int>("gtacnr:racing:createRace", new object[2]
						{
							selectedRaceTrack.Json(),
							bet
						}));
						if (racingSystemResponse == RacingSystemResponse.Success)
						{
							EditRacesItem.Enabled = false;
							SelectRaceItem.Enabled = false;
							LapsItem.Enabled = false;
							BetItem.Enabled = false;
							RegisterRaceItem.Enabled = false;
							InvitePlayersItem.Enabled = true;
							StartRaceItem.Enabled = true;
							RacingScript.StartingPosition = selectedRaceTrack.Checkpoints[0];
							RaceStartBlip = null;
						}
						else
						{
							Utils.DisplayErrorMessage(0, -1, racingSystemResponse.ToString());
						}
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
			}
			else if (menuItem == StartRaceItem)
			{
				Ped playerPed3 = Game.PlayerPed;
				Vehicle val7 = ((playerPed3 != null) ? playerPed3.CurrentVehicle : null);
				if ((Entity)(object)val7 == (Entity)null || !RacingScript.IsVehicleClassAllowed(val7.ClassType))
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_NO_VEHICLE));
					Utils.PlayErrorSound();
				}
				else if (await Utils.ShowConfirm(LocalizationController.S(Entries.Player.RACING_START_CONFIRM_MESSAGE, Utils.GetVehicleFullName(((Entity)val7).Model.Hash))))
				{
					m_editor = false;
					MainMenu.CloseMenu();
					BaseScript.TriggerServerEvent("gtacnr:racing:startRace", new object[0]);
				}
			}
			else if (menuItem == JoinRaceItem)
			{
				if (isBusy)
				{
					Utils.PlayErrorSound();
					return;
				}
				Ped playerPed4 = Game.PlayerPed;
				Vehicle val8 = ((playerPed4 != null) ? playerPed4.CurrentVehicle : null);
				if ((Entity)(object)val8 == (Entity)null || !RacingScript.IsVehicleClassAllowed(val8.ClassType))
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_NO_VEHICLE));
					Utils.PlayErrorSound();
				}
				else
				{
					if (!(await Utils.ShowConfirm(LocalizationController.S(Entries.Player.RACING_JOIN_CONFIRM_MESSAGE, Utils.GetVehicleFullName(((Entity)val8).Model.Hash)))))
					{
						return;
					}
					try
					{
						isBusy = true;
						RacingSystemResponse racingSystemResponse2 = (RacingSystemResponse)(await TriggerServerEventAsync<int>("gtacnr:racing:join", new object[2] { invitedBy, bet }));
						if (racingSystemResponse2 == RacingSystemResponse.Success)
						{
							MainMenu.CloseMenu();
							return;
						}
						Utils.DisplayErrorMessage(0, -1, LocalizationController.S(Entries.Player.RACING_JOIN_ERROR, racingSystemResponse2.ToString()));
					}
					finally
					{
						isBusy = false;
					}
				}
			}
			else if (menuItem == LeaveRaceItem && await Utils.ShowConfirm(LocalizationController.S(Entries.Player.RACING_LEAVE_CONFIRM_MESSAGE)))
			{
				MainMenu.CloseMenu();
				BaseScript.TriggerServerEvent("gtacnr:racing:leave", new object[0]);
			}
		}
	}

	private void OnListIndexChange(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (listItem == LapsItem)
		{
			selectedLaps = (uint)(newSelectionIndex + 1);
		}
		else if (listItem == SelectRaceItem)
		{
			selectedRaceTrack = RaceEditorMenuScript.GetRaceTrackAtIndex(newSelectionIndex);
			if (selectedRaceTrack != null)
			{
				if (selectedRaceTrack.Checkpoints.Count == 0)
				{
					RaceStartBlip = null;
					Utils.PlayErrorSound();
				}
				else
				{
					RaceStartBlip = CreateStartBlip(selectedRaceTrack.Checkpoints[0]);
				}
			}
		}
		else if (listItem == CheckpointsItem)
		{
			if (selectedRaceTrack.Checkpoints.Count > newSelectionIndex)
			{
				Vector3 val = selectedRaceTrack.Checkpoints[newSelectionIndex];
				API.LockMinimapPosition(val.X, val.Y);
			}
			else
			{
				API.UnlockMinimapPosition();
			}
		}
	}

	private void OnIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		API.UnlockMinimapPosition();
	}

	private List<string> GetCheckpointNames(RaceTrack raceTrack)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		int num = 0;
		foreach (Vector3 checkpoint in raceTrack.Checkpoints)
		{
			_ = checkpoint;
			list.Add(LocalizationController.S(Entries.Player.RACING_CHECKPOINT, num++));
		}
		return list;
	}

	[EventHandler("gtacnr:racing:ignoredInvite")]
	private void OnIgnoredInvite(int playerId)
	{
		playersInvited.Remove(playerId);
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_IGNORED_YOUR_INVITE, playerState.ColorNameAndId));
		}
	}

	[EventHandler("gtacnr:racing:inviteExpired")]
	private void OnInviteExpired(int playerId)
	{
		if (invitedBy != playerId)
		{
			return;
		}
		if (MainMenu.Visible && !m_editor)
		{
			MainMenu.CloseMenu();
		}
		if (!RacingScript.IsInRace)
		{
			PlayerState playerState = LatentPlayers.Get(playerId);
			if (playerState != null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_INVITE_EXPIRED, playerState.ColorNameAndId));
			}
		}
	}

	[EventHandler("gtacnr:racing:invited")]
	private async void OnInvited(int hostId, string raceTrackJson, long hostBet)
	{
		RaceTrack raceTrack;
		if (hostId != 0)
		{
			raceTrack = raceTrackJson.Unjson<RaceTrack>();
		}
		else
		{
			raceTrack = DailyRaceTrackScript.CurrentDailyRaceTrack;
		}
		Vector3 startingPosition;
		if (raceTrack != null && !RacingScript.StartingPosition.HasValue)
		{
			startingPosition = raceTrack.Checkpoints[0];
			if (hostId == 0)
			{
				OnAccept();
			}
			PlayerState playerState = LatentPlayers.Get(hostId);
			if (playerState != null)
			{
				string text = (Utils.IsUsingKeyboard() ? LocalizationController.S(Entries.Businesses.STP_PRESS, "~INPUT_MP_TEXT_CHAT_TEAM~") : LocalizationController.S(Entries.Businesses.STP_HOLD, "~INPUT_REPLAY_SCREENSHOT~"));
				await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Player.RACING_GOT_INVITED, playerState.ColorNameAndId, hostBet.ToCurrencyString(), text), InteractiveNotificationType.HelpText, OnAccept);
			}
		}
		bool OnAccept()
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Unknown result type (might be due to invalid IL or missing references)
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)currentVehicle == (Entity)null || !RacingScript.IsVehicleClassAllowed(currentVehicle.ClassType))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_NO_VEHICLE));
				Utils.PlayErrorSound();
				return false;
			}
			selectedRaceTrack = raceTrack;
			CheckpointsItem = new MenuListItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_CHECKPOINTS), GetCheckpointNames(selectedRaceTrack));
			LapsItem.ListIndex = (int)(selectedRaceTrack.Laps - 1);
			int num = 0;
			foreach (Vector3 checkpoint in selectedRaceTrack.Checkpoints)
			{
				Blip val = World.CreateBlip(checkpoint);
				val.Sprite = ((num == selectedRaceTrack.Checkpoints.Count - 1) ? RaceEditorMenuScript.FinishBlipSprite : RaceEditorMenuScript.StandardBlipSprite);
				val.Scale = 0.75f;
				val.Color = (BlipColor)66;
				checkPointBlips.Add(val);
				num++;
			}
			skip_reset = true;
			ShowMenu(editor: false);
			LapsItem.Enabled = false;
			invitedBy = hostId;
			RacingScript.StartingPosition = startingPosition;
			if (hostId == 0)
			{
				DailyRaceTrackScript.WaitingForDailyRaceStart = true;
			}
			return true;
		}
	}

	[EventHandler("gtacnr:racing:hostLeft")]
	private void OnHostLeft(int hostId)
	{
		if (!m_editor)
		{
			if (MainMenu.Visible)
			{
				MainMenu.CloseMenu();
			}
			Reset();
		}
	}

	[Command("racing")]
	private void OnRacingCommand()
	{
		if (MenuController.IsAnyMenuOpen())
		{
			MenuController.CloseAllMenus();
		}
		ShowMenu(editor: true);
	}

	private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
	{
		MainMenu.MenuTitle = LocalizationController.S(Entries.Player.RACING_MENU_MAIN);
		EditRacesItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_EDIT_TRACKS);
		SelectRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_SELECT_TRACK);
		LapsItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_LAPS);
		BetItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_BET);
		RegisterRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_REGISTER_RACE);
		InvitePlayersItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_INVITE_PLAYERS);
		StartRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_START_RACE);
		JoinRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_JOIN_RACE);
		LeaveRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_LEAVE_RACE);
	}
}
