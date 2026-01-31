using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Model;
using Gtacnr.Model.Robberies.Jewelry;

namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class JewelryRobberyStateScript : Script
{
	public static JewelryRobberyStateScript Instance { get; private set; }

	private Business JewelryStore { get; set; }

	private BusinessJewelryRobberyData RobberyData { get; set; }

	public RobberyState Robbery { get; private set; } = new RobberyState();

	public PlayerRobberyState Player { get; private set; } = new PlayerRobberyState();

	public bool CanBreakGlass
	{
		get
		{
			if (Robbery.IsInProgress && (Player.Phase == RobberyPhase.Stealing || !Player.IsParticipating) && !Robbery.DidAlarmEnd)
			{
				return !Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService();
			}
			return false;
		}
	}

	public static bool IsPlayerRobbing => Instance.Player.IsParticipating;

	public static event EventHandler<RobberyStartedEventArgs> RobberyStarted;

	public static event EventHandler<AlarmTrippedEventArgs> AlarmTripped;

	public static event EventHandler AlarmEnded;

	public static event EventHandler<PlayerJoinedEventArgs> PlayerJoined;

	public static event EventHandler<PlayerLeftEventArgs> PlayerLeft;

	public static event EventHandler<GlassDisabledEventArgs> GlassDisabled;

	public static event EventHandler<ItemsTakenEventArgs> ItemsTaken;

	public static event EventHandler<PhaseChangedEventArgs> PhaseChanged;

	public JewelryRobberyStateScript()
	{
		Instance = this;
	}

	public void ChangePhase(RobberyPhase newPhase)
	{
		RobberyPhase phase = Player.Phase;
		Player.Phase = newPhase;
		JewelryRobberyStateScript.PhaseChanged?.Invoke(this, new PhaseChangedEventArgs(phase, newPhase));
	}

	protected override async void OnStarted()
	{
		while (!BusinessScript.IsReady)
		{
			await BaseScript.Delay(0);
		}
		GetBusiness();
	}

	private void GetBusiness()
	{
		JewelryStore = BusinessScript.Businesses.Values.FirstOrDefault((Business b) => b.JewelryRobbery != null);
		if (JewelryStore != null)
		{
			RobberyData = JewelryStore.JewelryRobbery;
		}
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:onStarted")]
	private void OnRobberyStarted(int starterId)
	{
		if (Player.IsParticipating)
		{
			Utils.DisplayHelpText("~r~ERROR: The robbery state has been reset because someone has started another jewelry robbery.", playSound: false);
			Utils.PlayErrorSound();
		}
		Robbery = new RobberyState();
		Player = new PlayerRobberyState();
		Robbery.IsInProgress = true;
		Robbery.AlarmStartTimeLeft = RobberyData.TimeToAlarm;
		JewelryStore.IsBeingRobbed = true;
		JewelryRobberyStateScript.RobberyStarted?.Invoke(this, new RobberyStartedEventArgs(starterId));
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:onAlarmTripped")]
	private void OnAlarmTripped(int reasonCode)
	{
		JewelryRobberyStateScript.AlarmTripped?.Invoke(this, new AlarmTrippedEventArgs(reasonCode));
		Robbery.AlarmStartTimeLeft = 0;
		Robbery.AlarmEndTimeLeft = RobberyData.AlarmDuration;
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:onAlarmEnded")]
	private void OnAlarmEnded()
	{
		JewelryRobberyStateScript.AlarmEnded?.Invoke(this, new EventArgs());
		Robbery.IsInProgress = false;
		JewelryStore.IsBeingRobbed = false;
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:onPlayerJoined")]
	private void OnPlayerJoined(int playerId)
	{
		if (!Robbery.Participants.Contains(playerId))
		{
			if (playerId == Game.Player.ServerId)
			{
				Player.StartParticipating();
			}
			Robbery.Participants.Add(playerId);
			JewelryRobberyStateScript.PlayerJoined?.Invoke(this, new PlayerJoinedEventArgs(playerId));
		}
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:onPlayerLeft")]
	private void OnPlayerLeft(int playerId, int reasonCode)
	{
		JewelryRobberyStateScript.PlayerLeft?.Invoke(this, new PlayerLeftEventArgs(playerId, reasonCode));
		if (playerId == Game.Player.ServerId)
		{
			Player.StopParticipating();
		}
		if (Robbery.Participants.Contains(playerId))
		{
			Robbery.Participants.Remove(playerId);
		}
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:onGlassDisabled")]
	private void OnGlassDisabled(int index, int playerId, bool broken)
	{
		Robbery.DisabledGlasses.Add(index);
		JewelryRobberyStateScript.GlassDisabled(this, new GlassDisabledEventArgs(index, playerId, broken));
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:takeItems")]
	private void OnTakeItems(string jItems)
	{
		List<InventoryEntry> list = jItems.Unjson<List<InventoryEntry>>();
		foreach (InventoryEntry item in list)
		{
			Player.TakenItems.Add(item);
			Player.TakenItemsCount += item.Amount.ToInt();
		}
		JewelryRobberyStateScript.ItemsTaken?.Invoke(this, new ItemsTakenEventArgs(list));
	}

	[EventHandler("gtacnr:time")]
	private void OnTime(int hour, int minute, int dayOfWeek)
	{
		if (Robbery.IsInProgress)
		{
			if (!Robbery.WasAlarmTripped)
			{
				Robbery.AlarmStartTimeLeft -= 1000;
			}
			else if (!Robbery.DidAlarmEnd)
			{
				Robbery.AlarmEndTimeLeft -= 1000;
			}
		}
	}
}
