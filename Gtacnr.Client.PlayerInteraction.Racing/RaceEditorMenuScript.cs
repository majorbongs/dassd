using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.PlayerInteraction.Racing;

public class RaceEditorMenuScript : Script
{
	private static Menu MainMenu = new Menu(LocalizationController.S(Entries.Player.RACING_MENU_EDITOR_MAIN));

	private static MenuItem CreateRaceItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_CREATE_TRACK));

	private static List<MenuItem> RaceMenuEntries = new List<MenuItem>();

	private static RaceTrack? currentTrack;

	private static Menu EditorMenu = new Menu(LocalizationController.S(Entries.Player.RACING_MENU_EDITOR_EDITOR));

	private static MenuItem AddCheckpointItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_MENU_ITEM_ADD_CHECKPOINT_HERE));

	private static List<MenuItem> CheckpointMenuEntries = new List<MenuItem>();

	private static List<Blip> checkPointBlips = new List<Blip>();

	private static Blip? _trackStartBlip = null;

	public static readonly BlipSprite StandardBlipSprite = (BlipSprite)271;

	public static readonly BlipSprite FinishBlipSprite = (BlipSprite)38;

	public static Color CheckpointGroundColor = new Color(byte.MaxValue, byte.MaxValue, 0);

	public static Color CheckpointArrowColor = new Color(0, byte.MaxValue, byte.MaxValue);

	public static Color CheckpointSelectedColor = new Color(byte.MaxValue, 0, 0);

	public const int CheckpointGroundAlpha = 50;

	public const int CheckpointArrowAlpha = 127;

	private int selectedCheckpointIndex;

	private static int replaceItemIndex = -1;

	private static Blip? TrackStartBlip
	{
		get
		{
			return _trackStartBlip;
		}
		set
		{
			Blip? trackStartBlip = _trackStartBlip;
			if (trackStartBlip != null)
			{
				((PoolObject)trackStartBlip).Delete();
			}
			_trackStartBlip = value;
		}
	}

	public RaceEditorMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MenuController.AddMenu(EditorMenu);
		MainMenu.AddMenuItem(CreateRaceItem);
		EditorMenu.ParentMenu = MainMenu;
		MainMenu.OnItemSelect += OnItemSelect;
		EditorMenu.OnItemSelect += OnItemSelect;
		EditorMenu.OnIndexChange += OnIndexChange;
		MainMenu.OnIndexChange += OnIndexChange;
		EditorMenu.OnMenuOpen += OnMenuOpen;
		MainMenu.OnMenuOpen += OnMenuOpen;
		EditorMenu.OnMenuClose += OnMenuClose;
		MainMenu.OnMenuClose += OnMenuClose;
		MainMenu.InstructionalButtons.Add((Control)214, LocalizationController.S(Entries.Main.BTN_DELETE));
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)214, Menu.ControlPressCheckType.JUST_PRESSED, OnDeleteTrack, disableControl: true));
		MainMenu.InstructionalButtons.Add((Control)22, LocalizationController.S(Entries.Main.BTN_EXPORT));
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)22, Menu.ControlPressCheckType.JUST_PRESSED, OnExportTrack, disableControl: true));
		EditorMenu.InstructionalButtons.Add((Control)214, LocalizationController.S(Entries.Main.BTN_DELETE));
		EditorMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)214, Menu.ControlPressCheckType.JUST_PRESSED, OnDeleteCheckpoint, disableControl: true));
		EditorMenu.InstructionalButtons.Add((Control)154, LocalizationController.S(Entries.Player.RACING_EDITOR_INSTRUCTIONS_REPLACE));
		EditorMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)154, Menu.ControlPressCheckType.JUST_PRESSED, OnReplaceCheckpoint, disableControl: true));
		EditorMenu.InstructionalButtons.Add((Control)152, LocalizationController.S(Entries.Player.RACING_EDITOR_INSTRUCTIONS_MOVE_HERE));
		EditorMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)152, Menu.ControlPressCheckType.JUST_PRESSED, OnMoveCheckpointHere, disableControl: true));
		LoadTracks();
		LocalizationController.LanguageChanged += OnLanguageChanged;
	}

	private static void LoadTracks()
	{
		List<RaceTrack> preference = Utils.GetPreference("gtacnr:racing:tracks", new List<RaceTrack>());
		int num = 0;
		foreach (RaceTrack item in preference)
		{
			MenuItem menuItem = new MenuItem(LocalizationController.S(Entries.Player.RACING_TRACK, num));
			menuItem.ItemData = item;
			MainMenu.InsertMenuItem(menuItem, num);
			MenuController.BindMenuItem(MainMenu, EditorMenu, menuItem);
			RaceMenuEntries.Add(menuItem);
			num++;
		}
	}

	private static void SaveTracks()
	{
		List<RaceTrack> list = new List<RaceTrack>();
		foreach (MenuItem raceMenuEntry in RaceMenuEntries)
		{
			if (raceMenuEntry.ItemData is RaceTrack item)
			{
				list.Add(item);
			}
		}
		Utils.SetPreference("gtacnr:racing:tracks", list);
	}

	public static void ShowMenu(Menu previousMenu)
	{
		if (previousMenu != null)
		{
			MenuController.AddSubmenu(previousMenu, MainMenu);
		}
		else
		{
			MainMenu.ParentMenu = null;
		}
		if (MenuController.IsAnyMenuOpen())
		{
			MenuController.CloseAllMenus();
		}
		MainMenu.OpenMenu();
	}

	public static List<string> GetRaceTrackNames()
	{
		List<string> list = new List<string>();
		foreach (MenuItem raceMenuEntry in RaceMenuEntries)
		{
			list.Add(raceMenuEntry.Text);
		}
		return list;
	}

	public static RaceTrack? GetRaceTrackAtIndex(int index)
	{
		if (index >= RaceMenuEntries.Count || index < 0)
		{
			return null;
		}
		return RaceMenuEntries[index].ItemData as RaceTrack;
	}

	private void OnMenuOpen(Menu menu)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		TrackStartBlip = null;
		if (menu == EditorMenu)
		{
			selectedCheckpointIndex = 0;
			replaceItemIndex = -1;
		}
		else if (menu == MainMenu)
		{
			int currentIndex = menu.CurrentIndex;
			if (RaceMenuEntries.Count > currentIndex && RaceMenuEntries[currentIndex].ItemData is RaceTrack raceTrack && raceTrack.Checkpoints.Count > 0)
			{
				TrackStartBlip = World.CreateBlip(raceTrack.Checkpoints[0]);
				TrackStartBlip.Sprite = FinishBlipSprite;
				TrackStartBlip.Scale = 1f;
			}
		}
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		TrackStartBlip = null;
		if (menu == MainMenu)
		{
			SaveTracks();
		}
		else if (menu == EditorMenu)
		{
			currentTrack = null;
			checkPointBlips.ForEach(delegate(Blip b)
			{
				((PoolObject)b).Delete();
			});
			checkPointBlips.Clear();
			API.UnlockMinimapPosition();
			SaveTracks();
		}
	}

	private void OnIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (menu == EditorMenu)
		{
			if (currentTrack != null)
			{
				if (currentTrack.Checkpoints.Count > oldIndex)
				{
					checkPointBlips[oldIndex].Scale = 0.75f;
				}
				if (currentTrack.Checkpoints.Count > newIndex)
				{
					Vector3 val = currentTrack.Checkpoints[newIndex];
					checkPointBlips[newIndex].Scale = 1f;
					API.LockMinimapPosition(val.X, val.Y);
					selectedCheckpointIndex = newIndex;
				}
				else
				{
					API.UnlockMinimapPosition();
				}
			}
		}
		else if (menu == MainMenu)
		{
			TrackStartBlip = null;
			if (RaceMenuEntries.Count > newIndex && RaceMenuEntries[newIndex].ItemData is RaceTrack raceTrack && raceTrack.Checkpoints.Count > 0)
			{
				TrackStartBlip = World.CreateBlip(raceTrack.Checkpoints[0]);
				TrackStartBlip.Sprite = FinishBlipSprite;
				TrackStartBlip.Scale = 1f;
			}
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu == MainMenu)
		{
			if (menuItem == CreateRaceItem)
			{
				if (RaceMenuEntries.Count >= 10)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_TRACK_LIMIT, 10));
					Utils.PlayErrorSound();
					return;
				}
				MenuItem menuItem2 = new MenuItem(LocalizationController.S(Entries.Player.RACING_TRACK, RaceMenuEntries.Count));
				menuItem2.ItemData = new RaceTrack();
				RaceMenuEntries.Add(menuItem2);
				MainMenu.InsertMenuItem(menuItem2, RaceMenuEntries.Count - 1);
				MenuController.BindMenuItem(MainMenu, EditorMenu, menuItem2);
			}
			else
			{
				if (!RaceMenuEntries.Contains(menuItem))
				{
					return;
				}
				currentTrack = menuItem.ItemData as RaceTrack;
				if (currentTrack == null)
				{
					return;
				}
				EditorMenu.ClearMenuItems();
				CheckpointMenuEntries.Clear();
				checkPointBlips.ForEach(delegate(Blip b)
				{
					((PoolObject)b).Delete();
				});
				checkPointBlips.Clear();
				int num = 0;
				foreach (Vector3 checkpoint in currentTrack.Checkpoints)
				{
					MenuItem item = new MenuItem(LocalizationController.S(Entries.Player.RACING_CHECKPOINT, num));
					EditorMenu.AddMenuItem(item);
					CheckpointMenuEntries.Add(item);
					Blip val = World.CreateBlip(checkpoint);
					val.Sprite = ((num == currentTrack.Checkpoints.Count - 1) ? FinishBlipSprite : StandardBlipSprite);
					val.Scale = 0.75f;
					val.Color = (BlipColor)66;
					if (num != currentTrack.Checkpoints.Count - 1)
					{
						val.NumberLabel = num;
					}
					checkPointBlips.Add(val);
					num++;
				}
				EditorMenu.AddMenuItem(AddCheckpointItem);
			}
		}
		else
		{
			if (menu != EditorMenu || menuItem != AddCheckpointItem)
			{
				return;
			}
			if (currentTrack.Checkpoints.Count >= 100)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_CHECKPOINT_LIMIT, 100));
				Utils.PlayErrorSound();
				return;
			}
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if ((int)Game.PlayerPed.SeatIndex != -1 || (Entity)(object)currentVehicle == (Entity)null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_DRIVER));
				Utils.PlayErrorSound();
				return;
			}
			if (((Entity)currentVehicle).HeightAboveGround > 1.5f)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_GROUND));
				Utils.PlayErrorSound();
				return;
			}
			Vector3 newCheckpointPos = ((Entity)currentVehicle).Position;
			float z = newCheckpointPos.Z;
			if (API.GetGroundZFor_3dCoord(newCheckpointPos.X, newCheckpointPos.Y, newCheckpointPos.Z + 0.5f, ref z, false))
			{
				newCheckpointPos.Z = z;
			}
			if (currentTrack.Checkpoints.Any((Vector3 c) => ((Vector3)(ref c)).DistanceToSquared(newCheckpointPos) <= 900f))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_NEW_POS_TOO_CLOSE));
				Utils.PlayErrorSound();
				return;
			}
			if (currentTrack.Checkpoints.Count > 0)
			{
				Vector3 val2 = currentTrack.Checkpoints.Last();
				if (((Vector3)(ref val2)).DistanceToSquared(newCheckpointPos) > 1000000f)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_NEW_POS_TOO_FAR_AWAY));
					Utils.PlayErrorSound();
					return;
				}
			}
			MenuItem item2 = new MenuItem(LocalizationController.S(Entries.Player.RACING_CHECKPOINT, currentTrack.Checkpoints.Count));
			EditorMenu.InsertMenuItem(item2, currentTrack.Checkpoints.Count);
			CheckpointMenuEntries.Add(item2);
			currentTrack.Checkpoints.Add(newCheckpointPos);
			Blip val3 = checkPointBlips.LastOrDefault();
			if (val3 != (Blip)null)
			{
				val3.Sprite = StandardBlipSprite;
				val3.Scale = 0.75f;
				val3.Color = (BlipColor)66;
				val3.NumberLabel = checkPointBlips.Count - 1;
			}
			Blip val4 = World.CreateBlip(newCheckpointPos);
			val4.Sprite = FinishBlipSprite;
			val4.Scale = 0.75f;
			val4.Color = (BlipColor)66;
			EditorMenu.CurrentIndex++;
			checkPointBlips.Add(val4);
		}
	}

	private void OnDeleteTrack(Menu menu, Control control)
	{
		int currentIndex = menu.CurrentIndex;
		if (RaceMenuEntries.Count > currentIndex)
		{
			MenuItem item = RaceMenuEntries[currentIndex];
			MainMenu.RemoveMenuItem(item);
			RaceMenuEntries.RemoveAt(currentIndex);
			for (int i = currentIndex; i < RaceMenuEntries.Count; i++)
			{
				RaceMenuEntries[i].Text = LocalizationController.S(Entries.Player.RACING_TRACK, i);
			}
		}
	}

	private async void OnExportTrack(Menu menu, Control control)
	{
		int currentIndex = menu.CurrentIndex;
		if (RaceMenuEntries.Count > currentIndex && RaceMenuEntries[currentIndex].ItemData is RaceTrack obj)
		{
			await Utils.GetUserInput(LocalizationController.S(Entries.Player.RACING_TRACK_EXPORT_TITLE), LocalizationController.S(Entries.Player.RACING_TRACK_EXPORT_TEXT), "", 2048, "text", obj.Json());
		}
	}

	private void OnDeleteCheckpoint(Menu menu, Control control)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		int currentIndex = menu.CurrentIndex;
		if (CheckpointMenuEntries.Count <= currentIndex)
		{
			return;
		}
		if (currentTrack.Checkpoints.Count > 2 && currentIndex > 0 && currentIndex < currentTrack.Checkpoints.Count - 1)
		{
			Vector3 val = currentTrack.Checkpoints[currentIndex - 1];
			Vector3 val2 = currentTrack.Checkpoints[currentIndex + 1];
			if (((Vector3)(ref val)).DistanceToSquared(val2) > 1000000f)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_NEW_POS_TOO_FAR_AWAY));
				Utils.PlayErrorSound();
				return;
			}
		}
		EditorMenu.RemoveMenuItem(currentIndex);
		if (replaceItemIndex == currentIndex)
		{
			replaceItemIndex = -1;
		}
		CheckpointMenuEntries.RemoveAt(currentIndex);
		int num = 0;
		foreach (MenuItem checkpointMenuEntry in CheckpointMenuEntries)
		{
			if (num >= currentIndex)
			{
				checkpointMenuEntry.Text = LocalizationController.S(Entries.Player.RACING_CHECKPOINT, num);
			}
			num++;
		}
		currentTrack.Checkpoints.RemoveAt(currentIndex);
		if (checkPointBlips.Count == currentIndex + 1 && checkPointBlips.Count > 1)
		{
			checkPointBlips[currentIndex - 1].Sprite = FinishBlipSprite;
			checkPointBlips[currentIndex - 1].Color = (BlipColor)66;
		}
		((PoolObject)checkPointBlips[currentIndex]).Delete();
		checkPointBlips.RemoveAt(currentIndex);
		num = 0;
		foreach (Blip checkPointBlip in checkPointBlips)
		{
			if (num >= currentIndex)
			{
				checkPointBlip.NumberLabel = num;
			}
			num++;
		}
		checkPointBlips[checkPointBlips.Count - 1].RemoveNumberLabel();
	}

	private void OnReplaceCheckpoint(Menu menu, Control control)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		int currentIndex = menu.CurrentIndex;
		if (CheckpointMenuEntries.Count <= currentIndex)
		{
			return;
		}
		MenuItem menuItem = CheckpointMenuEntries[currentIndex];
		if (replaceItemIndex == -1)
		{
			menuItem.LeftIcon = MenuItem.Icon.LOCK;
			replaceItemIndex = currentIndex;
			return;
		}
		if (replaceItemIndex == currentIndex)
		{
			menuItem.LeftIcon = MenuItem.Icon.NONE;
			replaceItemIndex = -1;
			return;
		}
		checkPointBlips[currentIndex].Sprite = StandardBlipSprite;
		checkPointBlips[currentIndex].Color = (BlipColor)66;
		checkPointBlips[replaceItemIndex].Sprite = StandardBlipSprite;
		checkPointBlips[replaceItemIndex].Color = (BlipColor)66;
		checkPointBlips[currentIndex].NumberLabel = replaceItemIndex;
		checkPointBlips[replaceItemIndex].NumberLabel = currentIndex;
		Blip val = null;
		if (replaceItemIndex + 1 == CheckpointMenuEntries.Count)
		{
			val = checkPointBlips[currentIndex];
		}
		else if (currentIndex + 1 == CheckpointMenuEntries.Count)
		{
			val = checkPointBlips[replaceItemIndex];
		}
		if (val != (Blip)null)
		{
			val.Sprite = FinishBlipSprite;
			val.Color = (BlipColor)66;
			val.RemoveNumberLabel();
		}
		CheckpointMenuEntries[replaceItemIndex].LeftIcon = MenuItem.Icon.NONE;
		CheckpointMenuEntries[replaceItemIndex].Text = LocalizationController.S(Entries.Player.RACING_CHECKPOINT, currentIndex);
		CheckpointMenuEntries[currentIndex].Text = LocalizationController.S(Entries.Player.RACING_CHECKPOINT, replaceItemIndex);
		CheckpointMenuEntries.Swap(currentIndex, replaceItemIndex);
		currentTrack.Checkpoints.Swap(currentIndex, replaceItemIndex);
		checkPointBlips.Swap(currentIndex, replaceItemIndex);
		EditorMenu.SwapMenuItems(currentIndex, replaceItemIndex);
		replaceItemIndex = -1;
	}

	private void OnMoveCheckpointHere(Menu menu, Control control)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		int currentIndex = menu.CurrentIndex;
		if (CheckpointMenuEntries.Count <= currentIndex)
		{
			return;
		}
		Vector3 newPos = ((Entity)Game.PlayerPed).Position;
		if (currentTrack.Checkpoints.Any((Vector3 c) => c != currentTrack.Checkpoints[currentIndex] && ((Vector3)(ref c)).DistanceToSquared(newPos) <= 900f))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_NEW_POS_TOO_CLOSE));
			Utils.PlayErrorSound();
			return;
		}
		if (currentTrack.Checkpoints.Count > 1)
		{
			Vector3? val = ((currentIndex > 0) ? new Vector3?(currentTrack.Checkpoints[currentIndex - 1]) : ((Vector3?)null));
			Vector3? val2 = ((currentIndex < currentTrack.Checkpoints.Count - 1) ? new Vector3?(currentTrack.Checkpoints[currentIndex + 1]) : ((Vector3?)null));
			Vector3 value;
			if (val.HasValue)
			{
				value = val.Value;
				if (((Vector3)(ref value)).DistanceToSquared(newPos) > 1000000f)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_NEW_POS_TOO_FAR_AWAY));
					Utils.PlayErrorSound();
					return;
				}
			}
			if (val2.HasValue)
			{
				value = val2.Value;
				if (((Vector3)(ref value)).DistanceToSquared(newPos) > 1000000f)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_EDITOR_ERROR_NEW_POS_TOO_FAR_AWAY));
					Utils.PlayErrorSound();
					return;
				}
			}
		}
		currentTrack.Checkpoints[currentIndex] = newPos;
		checkPointBlips[currentIndex].Position = newPos;
	}

	[Update]
	private async Coroutine DrawCheckpointMarkers()
	{
		if (currentTrack == null)
		{
			return;
		}
		int num = 0;
		foreach (Vector3 checkpoint in currentTrack.Checkpoints)
		{
			float num2 = 0f;
			if (!API.GetGroundZFor_3dCoord(checkpoint.X, checkpoint.Y, checkpoint.Z, ref num2, false))
			{
				num2 = checkpoint.Z;
			}
			Vector3 val = Vector3.Zero;
			float num3 = 0f;
			bool flag = true;
			float num4 = 0f;
			if (currentTrack.Checkpoints.Count > num + 1)
			{
				val = currentTrack.Checkpoints[num + 1] - checkpoint;
				num4 = 90f;
				Vector3 val2 = currentTrack.Checkpoints[num + 1];
				num3 = ((Vector3)(ref val2)).DistanceToSquared(checkpoint);
				flag = false;
			}
			int num5 = 22;
			if (num == currentTrack.Checkpoints.Count - 1)
			{
				num5 = 4;
			}
			else if (num3 < (float)50.Square())
			{
				num5 = 20;
			}
			else if (num3 < (float)150.Square())
			{
				num5 = 21;
			}
			Color color = CheckpointGroundColor;
			if (num == selectedCheckpointIndex)
			{
				color = CheckpointSelectedColor;
			}
			API.DrawMarker(num5, checkpoint.X, checkpoint.Y, num2 + 2f, val.X, val.Y, val.Z, num4, 0f, 0f, 2.5f, 2.5f, 2.5f, (int)CheckpointArrowColor.R, (int)CheckpointArrowColor.G, (int)CheckpointArrowColor.B, 127, false, flag, 2, false, (string)null, (string)null, false);
			API.DrawMarker(1, checkpoint.X, checkpoint.Y, num2 + 0.05f, 0f, 0f, 0f, 0f, 0f, 0f, 6.25f, 6.25f, 6.25f, (int)color.R, (int)color.G, (int)color.B, 50, false, true, 2, false, (string)null, (string)null, false);
			num++;
		}
	}

	private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
	{
		MainMenu.MenuTitle = LocalizationController.S(Entries.Player.RACING_MENU_EDITOR_MAIN);
		CreateRaceItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_CREATE_TRACK);
		for (int i = 0; i < RaceMenuEntries.Count; i++)
		{
			RaceMenuEntries[i].Text = LocalizationController.S(Entries.Player.RACING_TRACK, i);
		}
		EditorMenu.MenuTitle = LocalizationController.S(Entries.Player.RACING_MENU_EDITOR_EDITOR);
		AddCheckpointItem.Text = LocalizationController.S(Entries.Player.RACING_MENU_ITEM_ADD_CHECKPOINT_HERE);
		for (int j = 0; j < CheckpointMenuEntries.Count; j++)
		{
			CheckpointMenuEntries[j].Text = LocalizationController.S(Entries.Player.RACING_CHECKPOINT, j);
		}
		MainMenu.InstructionalButtons[(Control)214] = LocalizationController.S(Entries.Main.BTN_DELETE);
		EditorMenu.InstructionalButtons[(Control)214] = LocalizationController.S(Entries.Main.BTN_DELETE);
		EditorMenu.InstructionalButtons[(Control)154] = LocalizationController.S(Entries.Player.RACING_EDITOR_INSTRUCTIONS_REPLACE);
		EditorMenu.InstructionalButtons[(Control)152] = LocalizationController.S(Entries.Player.RACING_EDITOR_INSTRUCTIONS_MOVE_HERE);
	}
}
