using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.HUD;
using Gtacnr.Localization;
using Gtacnr.Model;
using NativeUI;

namespace Gtacnr.Client.PlayerInteraction.Racing;

public class RacingScript : Script
{
	private class CountDown : IDisposable
	{
		private Scaleform _scaleform;

		private int _secondsLeft = -1;

		private bool _active;

		private readonly RacingScript _script;

		public Color DisplayColor;

		public CountDown(int initialSeconds, Color color)
		{
			DisplayColor = color;
			_active = false;
			UpdateTime(initialSeconds);
		}

		public void UpdateTime(int secondsLeft)
		{
			if (!_active && secondsLeft > 0)
			{
				_active = true;
				instance.Update += Draw;
			}
			if (_secondsLeft != secondsLeft)
			{
				_secondsLeft = secondsLeft;
				Scaleform scaleform = _scaleform;
				if (scaleform != null)
				{
					scaleform.Dispose();
				}
				string message = ((_secondsLeft <= 0) ? "GO" : _secondsLeft.ToString());
				_scaleform = CreateCountdown(message, DisplayColor.R, DisplayColor.G, DisplayColor.B);
				if (_secondsLeft > 0)
				{
					API.PlaySoundFrontend(-1, "3_2_1", "HUD_MINI_GAME_SOUNDSET", true);
				}
				else
				{
					API.PlaySoundFrontend(-1, "GO", "HUD_MINI_GAME_SOUNDSET", true);
				}
			}
		}

		private Scaleform CreateCountdown(string message, int _r, int _g, int _b)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			Scaleform val = new Scaleform("COUNTDOWN");
			val.CallFunction("SET_MESSAGE", new object[5] { message, _r, _g, _b, true });
			val.CallFunction("FADE_MP", new object[4] { message, _r, _g, _b });
			return val;
		}

		private async Coroutine Draw()
		{
			if (_scaleform == null)
			{
				return;
			}
			while (_active)
			{
				if (_scaleform != null)
				{
					API.DrawScaleformMovieFullscreen(_scaleform.Handle, 255, 255, 255, 255, 0);
				}
				await Script.Yield();
			}
		}

		public void Dispose()
		{
			instance.Update -= Draw;
			Scaleform scaleform = _scaleform;
			if (scaleform != null)
			{
				scaleform.Dispose();
			}
			_scaleform = null;
		}
	}

	private static RaceTrack? currentTrack;

	private static List<int> allParticipants = new List<int>();

	private static int raceVehicleModel;

	private static DateTime lastVehicleModelError;

	private static List<Blip> checkPointBlips = new List<Blip>();

	private static int currentCheckpoint = 1;

	private static DateTime raceEndTime = DateTime.MinValue;

	private static TimerBarWrapper<TextTimerBar> timeLeftBar;

	private static TimerBarWrapper<TextTimerBar> checkpointBar;

	private static TimerBarWrapper<TextTimerBar> lapsBar;

	private static TimerBarWrapper<TextTimerBar> participantsBar;

	private static DateTime lapStartTime = DateTime.MinValue;

	private static TimerBarWrapper<TextTimerBar> lapTimeBar;

	private static DateTime raceStartTime = DateTime.MinValue;

	private static TimerBarWrapper<TextTimerBar> raceTimeBar;

	public static Vector3? StartingPosition;

	private static RacingScript instance;

	public static bool IsInRace { get; private set; } = false;

	public static event EventHandler LeftStartingPosition;

	public RacingScript()
	{
		instance = this;
	}

	public static bool IsVehicleClassAllowed(VehicleClass vehClass)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		if ((int)vehClass == 15 || (int)vehClass == 16)
		{
			return false;
		}
		return true;
	}

	private static void Reset()
	{
		StartingPosition = null;
		currentTrack = null;
		IsInRace = false;
		currentCheckpoint = 1;
		checkPointBlips.ForEach(delegate(Blip b)
		{
			((PoolObject)b).Delete();
		});
		checkPointBlips.Clear();
		allParticipants.Clear();
		checkpointBar.Value = null;
		lapsBar.Value = null;
		participantsBar.Value = null;
		timeLeftBar.Value = null;
		lapTimeBar.Value = null;
		raceTimeBar.Value = null;
	}

	public Vector3? GetCurrentCheckpoint()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (currentTrack == null)
		{
			return null;
		}
		List<Vector3> checkpoints = currentTrack.Checkpoints;
		int index = currentCheckpoint % checkpoints.Count;
		return currentTrack.Checkpoints[index];
	}

	[EventHandler("gtacnr:racing:raceStarted")]
	private async void OnRaceStarted(string allParticipantsJson, string raceTrackJson)
	{
		if (IsInRace)
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !IsVehicleClassAllowed(currentVehicle.ClassType))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_NO_VEHICLE));
			Utils.PlayErrorSound();
			BaseScript.TriggerServerEvent("gtacnr:racing:leave", new object[0]);
			return;
		}
		raceVehicleModel = ((Entity)currentVehicle).Model.Hash;
		IsInRace = true;
		allParticipants = allParticipantsJson.Unjson<List<int>>();
		StartingPosition = (currentTrack = raceTrackJson.Unjson<RaceTrack>()).Checkpoints[0];
		int num = 0;
		foreach (Vector3 checkpoint in currentTrack.Checkpoints)
		{
			Blip val = World.CreateBlip(checkpoint);
			val.Sprite = ((currentTrack.Laps == 1 && num == currentTrack.Checkpoints.Count - 1) ? RaceEditorMenuScript.FinishBlipSprite : RaceEditorMenuScript.StandardBlipSprite);
			val.Scale = 0.5f;
			val.Color = (BlipColor)66;
			if (currentTrack.Laps != 1 || num != currentTrack.Checkpoints.Count - 1)
			{
				val.NumberLabel = num;
			}
			checkPointBlips.Add(val);
			num++;
		}
		checkPointBlips[1].Scale = 1f;
		DateTime startTime = DateTime.UtcNow.AddSeconds(10.0);
		int secondsLeft = (startTime - DateTime.UtcNow).TotalSeconds.ToIntCeil();
		using CountDown countDown = new CountDown(secondsLeft, new Color(byte.MaxValue, byte.MaxValue, 0));
		while (DateTime.UtcNow < startTime)
		{
			await Script.Wait(50);
			if (StartingPosition.HasValue)
			{
				Vehicle currentVehicle2 = Game.PlayerPed.CurrentVehicle;
				if (((currentVehicle2 != null) ? new int?(((Entity)currentVehicle2).Model.Hash) : ((int?)null)) == raceVehicleModel)
				{
					int num2 = (startTime - DateTime.UtcNow).TotalSeconds.ToIntCeil();
					if (num2 != secondsLeft)
					{
						if (num2 == 0)
						{
							countDown.DisplayColor = new Color(0, byte.MaxValue, 0);
							countDown.UpdateTime(num2);
							break;
						}
						countDown.UpdateTime(num2);
						secondsLeft = num2;
					}
					continue;
				}
			}
			BaseScript.TriggerServerEvent("gtacnr:racing:leave", new object[0]);
			IsInRace = false;
			StartingPosition = null;
			checkPointBlips.ForEach(delegate(Blip b)
			{
				((PoolObject)b).Delete();
			});
			checkPointBlips.Clear();
			return;
		}
		StartingPosition = null;
		checkpointBar.Value = new TextTimerBar(LocalizationController.S(Entries.Player.RACING_TIMER_BAR_CHECKPOINT_LABEL), $"{currentCheckpoint}/{currentTrack.Checkpoints.Count}");
		lapsBar.Value = new TextTimerBar(LocalizationController.S(Entries.Player.RACING_TIMER_BAR_LAP_LABEL), $"1/{currentTrack.Laps}");
		participantsBar.Value = new TextTimerBar(LocalizationController.S(Entries.Player.RACING_TIMER_BAR_PARTICIPANTS_LABEL), $"{allParticipants.Count}");
		lapStartTime = DateTime.UtcNow;
		lapTimeBar.Value = new TextTimerBar(LocalizationController.S(Entries.Player.RACING_TIMER_BAR_LAP_TIME_LABEL), "00:00.00");
		raceStartTime = DateTime.UtcNow;
		raceTimeBar.Value = new TextTimerBar(LocalizationController.S(Entries.Player.RACING_TIMER_BAR_RACE_TIME_LABEL), "00:00.00");
		TimerBarScript.AddTimerBar(participantsBar.Value);
		TimerBarScript.AddTimerBar(lapsBar.Value);
		TimerBarScript.AddTimerBar(checkpointBar.Value);
		TimerBarScript.AddTimerBar(raceTimeBar.Value);
		TimerBarScript.AddTimerBar(lapTimeBar.Value);
		await Script.Wait(500);
	}

	[EventHandler("gtacnr:racing:timeWarning")]
	private void OnTimeWarning()
	{
		raceEndTime = DateTime.UtcNow.AddSeconds(30.0);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_TIME_WARNING, Gtacnr.Utils.CalculateDetailedTimeIn(raceEndTime)));
		timeLeftBar.Value = new TextTimerBar(LocalizationController.S(Entries.Player.RACING_TIMER_BAR_TIME_LEFT_LABEL), Gtacnr.Utils.FormatTimerString(raceEndTime - DateTime.UtcNow, includeMiliseconds: true));
		TimerBarScript.AddTimerBar(timeLeftBar.Value);
	}

	[EventHandler("gtacnr:racing:checkpointPassed")]
	private void OnCheckpointPassed(int playerId, int newCheckpoint)
	{
		raceEndTime = DateTime.UtcNow.AddSeconds(60.0);
		timeLeftBar.Value = null;
	}

	[EventHandler("gtacnr:racing:invalidCheckpoint")]
	private void OnInvalidCheckpoint(int serverSideCheckpoint)
	{
		Utils.PlayErrorSound();
		int index = currentCheckpoint % currentTrack.Checkpoints.Count;
		checkPointBlips[index].Scale = 0.5f;
		currentCheckpoint = serverSideCheckpoint;
		index = currentCheckpoint % currentTrack.Checkpoints.Count;
		checkPointBlips[index].Scale = 1f;
		if (checkpointBar.Value != null && lapsBar.Value != null)
		{
			checkpointBar.Value.Text = $"{index}/{currentTrack.Checkpoints.Count}";
			int num = currentCheckpoint / currentTrack.Checkpoints.Count;
			lapsBar.Value.Text = $"{num + 1}/{currentTrack.Laps}";
		}
	}

	[EventHandler("gtacnr:racing:finished")]
	private void OnRaceFinished(int playerId)
	{
		Reset();
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState == null)
		{
			Utils.DisplayHelpText("Someone has finished this race!");
		}
		else if (playerId == Game.Player.ServerId)
		{
			Utils.DisplayHelpText("You have finished this race!");
		}
		else
		{
			Utils.DisplayHelpText(playerState.ColorNameAndId + " has finished this race!");
		}
	}

	[EventHandler("gtacnr:racing:hostLeft")]
	private void OnHostLeft(int hostId)
	{
		Reset();
		if (Game.Player.ServerId != hostId)
		{
			Utils.DisplayHelpText("~r~Host has disbanded this race!");
		}
	}

	[EventHandler("gtacnr:racing:participantJoined")]
	private void OnParticipantJoined(int playerId, long betAmount)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Player.RACING_PARTICIPANT_JOINED, "^5" + playerState.NameAndId + "^0", "^2" + betAmount.ToCurrencyString() + "^0"));
		}
	}

	[EventHandler("gtacnr:racing:participantLeft")]
	private void OnParticipantLeft(int playerId)
	{
		allParticipants.Remove(playerId);
		if (participantsBar.Value != null)
		{
			participantsBar.Value.Text = $"{allParticipants.Count}";
		}
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_PARTICIPANT_LEFT, playerState.ColorNameAndId));
		}
	}

	[EventHandler("gtacnr:racing:abort")]
	private void OnAbort()
	{
		Reset();
	}

	[Update]
	private async Coroutine DrawTask()
	{
		if (StartingPosition.HasValue)
		{
			Vector3 value = StartingPosition.Value;
			float z = value.Z;
			API.GetGroundZFor_3dCoord(value.X, value.Y, value.Z, ref z, true);
			World.DrawMarker((MarkerType)1, new Vector3(value.X, value.Y, z - 5f), Vector3.Zero, Vector3.Zero, new Vector3(40f, 40f, 35f), System.Drawing.Color.FromArgb(545259648), false, false, false, (string)null, (string)null, false);
		}
		if (currentTrack == null)
		{
			return;
		}
		bool flag = currentCheckpoint / currentTrack.Checkpoints.Count == currentTrack.Laps - 1;
		int num = currentCheckpoint % currentTrack.Checkpoints.Count;
		int num2 = 0;
		foreach (Vector3 checkpoint in currentTrack.Checkpoints)
		{
			if (num2 < num)
			{
				num2++;
				continue;
			}
			float num3 = 0f;
			if (!API.GetGroundZFor_3dCoord(checkpoint.X, checkpoint.Y, checkpoint.Z, ref num3, false))
			{
				num3 = checkpoint.Z;
			}
			Vector3 val = Vector3.Zero;
			float num4 = 0f;
			bool flag2 = true;
			float num5 = 0f;
			Vector3 val2;
			if (currentTrack.Checkpoints.Count > num2 + 1)
			{
				val = currentTrack.Checkpoints[num2 + 1] - checkpoint;
				num5 = 90f;
				val2 = currentTrack.Checkpoints[num2 + 1];
				num4 = ((Vector3)(ref val2)).DistanceToSquared(checkpoint);
				flag2 = false;
			}
			else if (currentTrack.Checkpoints.Count == num2 + 1 && !flag)
			{
				val = currentTrack.Checkpoints[0] - checkpoint;
				num5 = 90f;
				val2 = currentTrack.Checkpoints[0];
				num4 = ((Vector3)(ref val2)).DistanceToSquared(checkpoint);
				flag2 = false;
			}
			int num6 = 22;
			if (num2 == currentTrack.Checkpoints.Count - 1 && flag)
			{
				num6 = 4;
			}
			else if (num4 < (float)50.Square())
			{
				num6 = 20;
			}
			else if (num4 < (float)150.Square())
			{
				num6 = 21;
			}
			Color checkpointGroundColor = RaceEditorMenuScript.CheckpointGroundColor;
			Color checkpointArrowColor = RaceEditorMenuScript.CheckpointArrowColor;
			float num7 = 1f / (float)((num2 == num) ? 1 : 3);
			API.DrawMarker(num6, checkpoint.X, checkpoint.Y, num3 + 2f, val.X, val.Y, val.Z, num5, 0f, 0f, 2.5f, 2.5f, 2.5f, (int)checkpointArrowColor.R, (int)checkpointArrowColor.G, (int)checkpointArrowColor.B, (int)(127f * num7), false, flag2, 2, false, (string)null, (string)null, false);
			API.DrawMarker(1, checkpoint.X, checkpoint.Y, num3 + 0.05f, 0f, 0f, 0f, 0f, 0f, 0f, 6.25f, 6.25f, 6.25f, (int)checkpointGroundColor.R, (int)checkpointGroundColor.G, (int)checkpointGroundColor.B, (int)(50f * num7), false, true, 2, false, (string)null, (string)null, false);
			num2++;
		}
	}

	[Update]
	private async Coroutine UpdateTimers()
	{
		await BaseScript.Delay(5);
		if (IsInRace)
		{
			if (timeLeftBar.Value != null)
			{
				timeLeftBar.Value.Text = Gtacnr.Utils.FormatTimerString(raceEndTime - DateTime.UtcNow, includeMiliseconds: true);
			}
			if (lapTimeBar.Value != null && lapStartTime != DateTime.MinValue)
			{
				lapTimeBar.Value.Text = Gtacnr.Utils.FormatTimerString(DateTime.UtcNow - lapStartTime, includeMiliseconds: true);
			}
			if (raceTimeBar.Value != null && raceStartTime != DateTime.MinValue)
			{
				raceTimeBar.Value.Text = Gtacnr.Utils.FormatTimerString(DateTime.UtcNow - raceStartTime, includeMiliseconds: true);
			}
		}
	}

	[Update]
	private async Coroutine PositionChecks()
	{
		await Script.Wait(100);
		if (StartingPosition.HasValue)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(StartingPosition.Value) > 400f)
			{
				Utils.PlayErrorSound();
				RacingScript.LeftStartingPosition?.Invoke(this, EventArgs.Empty);
				StartingPosition = null;
			}
		}
		Vector3? val = GetCurrentCheckpoint();
		if (!val.HasValue || currentTrack == null || StartingPosition.HasValue)
		{
			return;
		}
		Vector3 position2 = ((Entity)Game.PlayerPed).Position;
		if (!(((Vector3)(ref position2)).DistanceToSquared(val.Value) < 100f))
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (((currentVehicle != null) ? new int?(((Entity)currentVehicle).Model.Hash) : ((int?)null)) != raceVehicleModel)
		{
			if (Gtacnr.Utils.CheckTimePassed(lastVehicleModelError, 5000.0))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.RACING_ERROR_WRONG_VEHICLE, Utils.GetVehicleFullName(raceVehicleModel)));
				Utils.PlayErrorSound();
				lastVehicleModelError = DateTime.UtcNow;
			}
			return;
		}
		int index = currentCheckpoint % currentTrack.Checkpoints.Count;
		checkPointBlips[index].Scale = 0.5f;
		currentCheckpoint++;
		index = currentCheckpoint % currentTrack.Checkpoints.Count;
		checkPointBlips[index].Scale = 1f;
		if (index == 1)
		{
			lapStartTime = DateTime.UtcNow;
			lapTimeBar.Value.Text = "00:00";
		}
		if (currentCheckpoint == (currentTrack.Laps - 1) * currentTrack.Checkpoints.Count)
		{
			Blip obj = checkPointBlips.Last();
			obj.Sprite = RaceEditorMenuScript.FinishBlipSprite;
			obj.Color = (BlipColor)66;
			obj.RemoveNumberLabel();
		}
		if (currentCheckpoint == currentTrack.CheckpointsToPass)
		{
			Reset();
			API.PlaySoundFrontend(-1, "CHECKPOINT_PERFECT", "HUD_MINI_GAME_SOUNDSET", true);
		}
		else
		{
			if (checkpointBar.Value != null && lapsBar.Value != null)
			{
				checkpointBar.Value.Text = $"{index}/{currentTrack.Checkpoints.Count}";
				int num = currentCheckpoint / currentTrack.Checkpoints.Count;
				lapsBar.Value.Text = $"{num + 1}/{currentTrack.Laps}";
			}
			API.PlaySoundFrontend(-1, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET", true);
		}
		BaseScript.TriggerServerEvent("gtacnr:racing:checkpointPassed", new object[1] { currentCheckpoint });
	}
}
