using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Estates.Warehouses;
using Gtacnr.Client.HUD;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.Model.Robberies.Jewelry;
using NativeUI;

namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class JewelryRobberyUIScript : Script
{
	private static JewelryRobberyUIScript instance;

	private List<Tuple<string, Vector3>> visibleHints = new List<Tuple<string, Vector3>>();

	private Dictionary<int, RobberyPlayerHealthBar> healthBars = new Dictionary<int, RobberyPlayerHealthBar>();

	private TextTimerBar alarmBar;

	private TextTimerBar takeBar;

	private Blip orangeAreaBlip;

	private Blip policeAreaBlip;

	private bool isBreakGlassControlEnabled;

	private bool isStopBreakGlassControlEnabled;

	private bool isCheckTaskAttached;

	private bool isDrawTaskAttached;

	private JewelryRobberyStateScript State => JewelryRobberyStateScript.Instance;

	private Business JewelryStore { get; set; }

	private BusinessJewelryRobberyData RobberyData { get; set; }

	public static event EventHandler TeargasThrown;

	public static event EventHandler StoreLeft;

	public static event EventHandler<BreakGlassEventArgs> BreakGlassControlExecuted;

	public static event EventHandler CancelBreakGlassControlExecuted;

	public JewelryRobberyUIScript()
	{
		instance = this;
	}

	protected override async void OnStarted()
	{
		while (!BusinessScript.IsReady)
		{
			await BaseScript.Delay(0);
		}
		GetBusiness();
		AttachRobberyEvents();
		AttachCoreEvents();
	}

	private void AttachRobberyEvents()
	{
		JewelryRobberyScript.RobberyInitiated += OnRobberyInitiated;
		JewelryRobberyScript.StartedBreakingGlass += OnStartedBreakingGlass;
		JewelryRobberyScript.StoppedBreakingGlass += OnStoppedBreakingGlass;
		JewelryRobberyStateScript.AlarmTripped += OnAlarmTripped;
		JewelryRobberyStateScript.PlayerJoined += OnPlayerJoined;
		JewelryRobberyStateScript.PlayerLeft += OnPlayerLeft;
		JewelryRobberyStateScript.ItemsTaken += OnItemsTaken;
		JewelryRobberyStateScript.PhaseChanged += OnPhaseChanged;
	}

	private void AttachCoreEvents()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChanged;
	}

	private void GetBusiness()
	{
		JewelryStore = BusinessScript.Businesses.Values.FirstOrDefault((Business b) => b.JewelryRobbery != null);
		RobberyData = JewelryStore.JewelryRobbery;
	}

	private void OnRobberyInitiated(object sender, EventArgs e)
	{
		AddAlarmBar();
		AddTakeBar();
	}

	private void OnPlayerJoined(object sender, PlayerJoinedEventArgs e)
	{
		if (!State.Player.IsParticipating && e.PlayerId == Game.Player.ServerId)
		{
			AddAlarmBar();
			AddTakeBar();
			{
				foreach (int item in State.Robbery.Participants.Except(new int[1] { Game.Player.ServerId }))
				{
					AddPlayerHealthBar(item);
				}
				return;
			}
		}
		if (State.Player.IsParticipating)
		{
			PlayerState playerState = LatentPlayers.Get(e.PlayerId);
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.JOINED_ROBBERY, playerState.FullyFormatted));
			AddPlayerHealthBar(e.PlayerId);
		}
	}

	private void OnPlayerLeft(object sender, PlayerLeftEventArgs e)
	{
		if (e.PlayerId == Game.Player.ServerId)
		{
			string text = ((e.ReasonCode == 1) ? Entries.Jobs.JEWELRY_ROBBERY_RESPAWNED : ((e.ReasonCode == 2) ? Entries.Jobs.JEWELRY_ROBBERY_CUFFED : ((e.ReasonCode == 3) ? Entries.Jobs.JEWELRY_ROBBERY_NO_ITEMS_TAKEN : null)));
			if (text != null)
			{
				Utils.SendNotification(LocalizationController.S(text));
			}
			RemoveAllRobberyGUI();
			DeleteOrangeAreaBlip();
		}
		else if (State.Player.IsParticipating)
		{
			PlayerState playerState = LatentPlayers.Get(e.PlayerId);
			string text2 = ((e.ReasonCode == 1) ? Entries.Jobs.DIED_ROBBERY : ((e.ReasonCode == 2) ? Entries.Jobs.CUFFED_ROBBERY : null));
			if (text2 != null)
			{
				Utils.SendNotification(LocalizationController.S(text2, playerState.FullyFormatted));
			}
			RemovePlayerHealthBar(e.PlayerId);
		}
	}

	private void OnItemsTaken(object sender, ItemsTakenEventArgs e)
	{
		string text = "";
		foreach (InventoryEntry entry in e.Entries)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
			text += $"{entry.Amount} ~b~{itemDefinition.Name}~s~, ";
		}
		text = text.Trim().Trim(',');
		Utils.SendNotification(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_STOLE_ITEMS, text));
		UpdateTakeBar(State.Player.TakenItemsCount);
	}

	private void OnAlarmTripped(object sender, AlarmTrippedEventArgs e)
	{
		WarnPolice();
	}

	private async void OnPhaseChanged(object sender, PhaseChangedEventArgs e)
	{
		if (e.NewPhase == RobberyPhase.LeavingArea)
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_LEAVE_THE_AREA), 12000);
			CrimeScript.DeleteCrimeAreaBlip();
			RemoveAllPlayerHealthBars();
			RemoveAlarmBar();
			CreateOrangeAreaBlip();
		}
		else if (e.NewPhase == RobberyPhase.GoingToHideout)
		{
			DeleteOrangeAreaBlip();
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_GO_TO_HIDEOUT));
			await BaseScript.Delay(5000);
			int num = WarehouseScript.OwnedWarehouses.Count();
			if (num > 0)
			{
				await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_HIDEOUTS_AVAILABLE, num), InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(15.0), 0u, LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_LIST_HIDEOUTS_BTN), LocalizationController.S(Entries.Main.BTN_HOLD, LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_LIST_HIDEOUTS_BTN)));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_NO_HIDEOUTS_AVAILABLE));
			}
		}
		else if (e.NewPhase == RobberyPhase.Finished)
		{
			RemoveAllRobberyGUI();
		}
		static bool OnAccepted()
		{
			PropertiesMenuScript.OpenMenu(new PropertyType[1] { PropertyType.Warehouse }, LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_LIST_HIDEOUTS), LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_LIST_HIDEOUTS_SUBTITLE));
			return true;
		}
	}

	private void OnJobChanged(object sender, JobArgs e)
	{
		if (!e.CurrentJobEnum.IsPublicService())
		{
			AttachCheckTask();
			DestroyPoliceAreaBlip();
		}
		else
		{
			DetachCheckTask();
			DetachDrawTask();
		}
	}

	[EventHandler("gtacnr:police:gotCuffed")]
	private void OnGotCuffed(int officerId)
	{
		if (State.Robbery.IsInProgress && State.Player.IsParticipating)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_CUFFED));
		}
	}

	private void AddAlarmBar()
	{
		if (alarmBar != null)
		{
			RemoveAlarmBar();
		}
		alarmBar = new TextTimerBar(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_TIMER_BAR_ALARM), "00:00");
		TimerBarScript.AddTimerBar(alarmBar);
	}

	private void RemoveAlarmBar()
	{
		if (alarmBar != null)
		{
			TimerBarScript.RemoveTimerBar(alarmBar);
			alarmBar = null;
		}
	}

	private void UpdateAlarmBar(TimeSpan timeLeft)
	{
		if (alarmBar != null)
		{
			alarmBar.Text = $"{timeLeft.Minutes:00}:{timeLeft.Seconds:00}";
		}
	}

	private void UpdateAlarmBarLabel(bool isBeforeAlarm)
	{
		if (alarmBar != null)
		{
			alarmBar.Label = (isBeforeAlarm ? LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_TIMER_BAR_ALARM) : LocalizationController.S(Entries.Main.MISSION_TIME_LEFT));
		}
	}

	private void AddTakeBar()
	{
		if (takeBar != null)
		{
			RemoveTakeBar();
		}
		takeBar = new TextTimerBar(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_TIMER_BAR_TAKE), "0");
		TimerBarScript.AddTimerBar(takeBar);
	}

	private void RemoveTakeBar()
	{
		if (takeBar != null)
		{
			TimerBarScript.RemoveTimerBar(takeBar);
			takeBar = null;
		}
	}

	private void UpdateTakeBar(int amount)
	{
		if (takeBar != null)
		{
			takeBar.Text = $"{amount}";
		}
	}

	private void AddPlayerHealthBar(int playerId)
	{
		if (healthBars.Count < 8)
		{
			if (healthBars.ContainsKey(playerId))
			{
				healthBars[playerId].Remove();
			}
			healthBars[playerId] = new RobberyPlayerHealthBar(playerId);
		}
	}

	private void UpdatePlayerHealthBar(int playerId)
	{
		if (healthBars.ContainsKey(playerId))
		{
			healthBars[playerId].Refresh();
		}
	}

	private void UpdateAllPlayerHealthBars()
	{
		foreach (RobberyPlayerHealthBar value in healthBars.Values)
		{
			value.Refresh();
		}
	}

	private void RemovePlayerHealthBar(int playerId)
	{
		if (healthBars.ContainsKey(playerId))
		{
			healthBars[playerId].Remove();
			healthBars.Remove(playerId);
		}
	}

	private void RemoveAllPlayerHealthBars()
	{
		foreach (int item in healthBars.Keys.ToList())
		{
			RemovePlayerHealthBar(item);
		}
	}

	private void RemoveAllRobberyGUI()
	{
		RemoveAlarmBar();
		RemoveTakeBar();
		RemoveAllPlayerHealthBars();
	}

	private void CheckForTeargasGrenade()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		Vector4 gasTargetCoords = RobberyData.GasTargetCoords;
		if (API.IsProjectileTypeWithinDistance(gasTargetCoords.X, gasTargetCoords.Y, gasTargetCoords.Z, 4256991824u, 2.5f, true))
		{
			API.AddExplosion(gasTargetCoords.X, gasTargetCoords.Y, gasTargetCoords.Z, 20, 1f, false, false, 0f);
			API.ClearAreaOfProjectiles(gasTargetCoords.X, gasTargetCoords.Y, gasTargetCoords.Z, 2.5f, 0);
			JewelryRobberyUIScript.TeargasThrown?.Invoke(this, new EventArgs());
		}
	}

	private void CheckForDisplayGlassProximity()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (DeathScript.IsAlive == true && State.CanBreakGlass && !State.Player.IsBreakingGlass)
		{
			int num = -1;
			foreach (Vector4 glassCoord in RobberyData.GlassCoords)
			{
				num++;
				if (!State.Robbery.DisabledGlasses.Contains(num))
				{
					Vector3 val = glassCoord.XYZ();
					Vector3 position = ((Entity)Game.PlayerPed).Position;
					if (((Vector3)(ref position)).DistanceToSquared(val) <= 1.25f.Square())
					{
						EnableBreakGlassControl();
						State.Player.TargetGlassIndex = num;
						return;
					}
				}
			}
		}
		State.Player.TargetGlassIndex = -1;
		DisableBreakGlassControl();
	}

	private void EnableBreakGlassControl()
	{
		if (!isBreakGlassControlEnabled)
		{
			isBreakGlassControlEnabled = true;
			KeysScript.AttachListener((Control)29, OnBreakGlassKeyEvent, 10);
			Utils.AddInstructionalButton("glassBreak", new Gtacnr.Client.API.UI.InstructionalButton(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_INSTRUCTION_BREAK), 2, (Control)29));
		}
	}

	private void DisableBreakGlassControl()
	{
		if (isBreakGlassControlEnabled)
		{
			isBreakGlassControlEnabled = false;
			KeysScript.DetachListener((Control)29, OnBreakGlassKeyEvent);
			Utils.RemoveInstructionalButton("glassBreak");
		}
	}

	private void EnableStopBreakGlassControl()
	{
		if (!isStopBreakGlassControlEnabled)
		{
			isStopBreakGlassControlEnabled = true;
			KeysScript.AttachListener((Control)29, OnBreakGlassKeyEvent, 10);
			Utils.AddInstructionalButton("glassCancel", new Gtacnr.Client.API.UI.InstructionalButton(LocalizationController.S(Entries.Main.BTN_HOLD, LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_INSTRUCTION_STOP)), 2, (Control)29));
		}
	}

	private void DisableStopBreakGlassControl()
	{
		if (isStopBreakGlassControlEnabled)
		{
			isStopBreakGlassControlEnabled = false;
			KeysScript.DetachListener((Control)29, OnBreakGlassKeyEvent);
			Utils.RemoveInstructionalButton("glassCancel");
		}
	}

	private bool OnBreakGlassKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 29)
		{
			if (eventType == KeyEventType.JustPressed && isBreakGlassControlEnabled && State.Player.TargetGlassIndex > -1)
			{
				JewelryRobberyUIScript.BreakGlassControlExecuted?.Invoke(this, new BreakGlassEventArgs(State.Player.TargetGlassIndex));
				return true;
			}
			if (eventType == KeyEventType.Held && State.Player.IsBreakingGlass)
			{
				JewelryRobberyUIScript.CancelBreakGlassControlExecuted?.Invoke(this, new EventArgs());
				return true;
			}
		}
		return false;
	}

	private void OnStartedBreakingGlass(object sender, EventArgs e)
	{
		DisableBreakGlassControl();
		EnableStopBreakGlassControl();
	}

	private void OnStoppedBreakingGlass(object sender, EventArgs e)
	{
		DisableStopBreakGlassControl();
	}

	private async void DetermineHintsToDisplay()
	{
		int idx = 0;
		Vector3 pCoords = ((Entity)Game.PlayerPed).Position;
		List<Tuple<string, Vector3>> curVisibleHints = new List<Tuple<string, Vector3>>();
		foreach (Vector3 hintCoords in RobberyData.HintCoords)
		{
			idx++;
			if (!(Math.Abs(hintCoords.Z - pCoords.Z) > 6f) && !(((Vector3)(ref pCoords)).DistanceToSquared2D(hintCoords) > 20f.Square()))
			{
				int shapeTest = API.StartShapeTestLosProbe(pCoords.X, pCoords.Y, pCoords.Z, hintCoords.X, hintCoords.Y, hintCoords.Z, -1, ((PoolObject)Game.PlayerPed).Handle, 4);
				bool hit = false;
				Vector3 endCoords = Vector3.Zero;
				Vector3 normal = Vector3.Zero;
				int entityHit = 0;
				while (API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref normal, ref entityHit) == 1)
				{
					await BaseScript.Delay(0);
				}
				if (!hit)
				{
					string item = LocalizationController.S($"jewelry_robbery_hint_{idx}");
					curVisibleHints.Add(Tuple.Create<string, Vector3>(item, hintCoords));
				}
			}
		}
		visibleHints = curVisibleHints;
	}

	private void ClearHints()
	{
		visibleHints.Clear();
	}

	private void CreateOrangeAreaBlip()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		DeleteOrangeAreaBlip();
		orangeAreaBlip = World.CreateBlip(JewelryStore.Location, RobberyData.LeaveAreaRadius);
		orangeAreaBlip.IsShortRange = false;
		orangeAreaBlip.Sprite = (BlipSprite)(-1);
		orangeAreaBlip.Color = (BlipColor)47;
		orangeAreaBlip.Alpha = 85;
		Utils.SetBlipName(orangeAreaBlip, "Leave the Area", "heist_area");
		API.SetBlipDisplay(((PoolObject)orangeAreaBlip).Handle, 8);
	}

	private void DeleteOrangeAreaBlip()
	{
		if (orangeAreaBlip != (Blip)null)
		{
			((PoolObject)orangeAreaBlip).Delete();
			orangeAreaBlip = null;
		}
	}

	private void WarnPolice()
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
		{
			Utils.PlaySoundFrontendFromAudioBank(-1, "high_priority_warning", "frontend", "gtacnr_audio/frontend");
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_POLICE_NOTIFICATION), Gtacnr.Utils.Colors.HudPurpleDark);
			CreatePoliceAreaBlip();
		}
	}

	private void CreatePoliceAreaBlip()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		DestroyPoliceAreaBlip();
		policeAreaBlip = World.CreateBlip(JewelryStore.Location, 100f);
		policeAreaBlip.Color = (BlipColor)3;
		policeAreaBlip.Alpha = 64;
		policeAreaBlip.IsFlashing = true;
		Utils.SetBlipName(policeAreaBlip, "Robbery Call", "heist_area");
	}

	private void DestroyPoliceAreaBlip()
	{
		if (policeAreaBlip != (Blip)null)
		{
			((PoolObject)policeAreaBlip).Delete();
			policeAreaBlip = null;
		}
	}

	private void AttachCheckTask()
	{
		if (!isCheckTaskAttached)
		{
			isCheckTaskAttached = true;
			base.Update += RobberyCheckTask;
		}
	}

	private void DetachCheckTask()
	{
		if (isCheckTaskAttached)
		{
			isCheckTaskAttached = false;
			base.Update -= RobberyCheckTask;
		}
	}

	private async Coroutine RobberyCheckTask()
	{
		await BaseScript.Delay(50);
		if (!BusinessScript.IsReady)
		{
			return;
		}
		if (BusinessScript.ClosestBusiness != JewelryStore)
		{
			DetachDrawTask();
			return;
		}
		AttachDrawTask();
		if (!State.Robbery.IsInProgress)
		{
			DetermineHintsToDisplay();
			CheckForTeargasGrenade();
			return;
		}
		ClearHints();
		if (State.Player.IsParticipating)
		{
			UpdateAllPlayerHealthBars();
			if (State.Player.Phase == RobberyPhase.Stealing && State.Player.TakenItemsCount > 0)
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref position)).DistanceToSquared(RobberyData.ExitCoords) < 3f.Square())
				{
					JewelryRobberyUIScript.StoreLeft?.Invoke(this, new EventArgs());
				}
			}
		}
		CheckForDisplayGlassProximity();
	}

	[EventHandler("gtacnr:time")]
	private void OnTime(int hour, int minute, int dayOfWeek)
	{
		if (State.Robbery.IsInProgress && State.Player.IsParticipating && State.Player.Phase == RobberyPhase.Stealing)
		{
			if (!State.Robbery.WasAlarmTripped)
			{
				UpdateAlarmBar(TimeSpan.FromMilliseconds(State.Robbery.AlarmStartTimeLeft));
				UpdateAlarmBarLabel(isBeforeAlarm: true);
			}
			else if (!State.Robbery.DidAlarmEnd)
			{
				UpdateAlarmBar(TimeSpan.FromMilliseconds(State.Robbery.AlarmEndTimeLeft));
				UpdateAlarmBarLabel(isBeforeAlarm: false);
			}
			else
			{
				RemoveAlarmBar();
			}
		}
		else
		{
			RemoveAlarmBar();
		}
	}

	private void AttachDrawTask()
	{
		if (!isDrawTaskAttached)
		{
			isDrawTaskAttached = true;
			base.Update += DrawTask;
		}
	}

	private void DetachDrawTask()
	{
		if (isDrawTaskAttached)
		{
			isDrawTaskAttached = false;
			base.Update -= DrawTask;
		}
	}

	private async Coroutine DrawTask()
	{
		if (BusinessScript.ClosestBusiness != JewelryStore)
		{
			return;
		}
		foreach (Tuple<string, Vector3> visibleHint in visibleHints)
		{
			Utils.Draw3DText(visibleHint.Item1, visibleHint.Item2);
		}
		if (!State.Robbery.IsInProgress)
		{
			Vector3 val = RobberyData.GasTargetCoords.XYZ();
			float w = RobberyData.GasTargetCoords.W;
			World.DrawMarker((MarkerType)1, val, Vector3.Zero, Vector3.Zero, new Vector3(w, w, 1.2f), System.Drawing.Color.FromArgb(1622911488), false, false, false, (string)null, (string)null, false);
		}
		if (!State.CanBreakGlass)
		{
			return;
		}
		int num = -1;
		foreach (Vector4 glassCoord in RobberyData.GlassCoords)
		{
			num++;
			if (!State.Robbery.DisabledGlasses.Contains(num))
			{
				Vector3 val2 = glassCoord.XYZ();
				World.DrawMarker((MarkerType)1, val2, Vector3.Zero, Vector3.Zero, new Vector3(0.4f, 0.4f, 0.6f), System.Drawing.Color.FromArgb(817605120), false, false, false, (string)null, (string)null, false);
			}
		}
	}
}
