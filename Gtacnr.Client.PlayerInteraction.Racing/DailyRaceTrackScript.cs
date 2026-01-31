using System;
using System.Drawing;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Model;

namespace Gtacnr.Client.PlayerInteraction.Racing;

public class DailyRaceTrackScript : Script
{
	private static Blip? DailyBlip = null;

	private static DailyRaceTrack? StoredDailyRaceTrack = null;

	private static DailyRaceTrack? _currentDailyRaceTrack = null;

	private static DateTime lastRequestT = default(DateTime);

	private static DailyRaceTrackScript Instance;

	private static bool TasksAttached = false;

	private static DateTime TimeToStart = default(DateTime);

	private static string[] MarkerLines = new string[4];

	public static DailyRaceTrack? CurrentDailyRaceTrack
	{
		get
		{
			return _currentDailyRaceTrack;
		}
		private set
		{
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			_ = _currentDailyRaceTrack;
			_currentDailyRaceTrack = value;
			if (value != null)
			{
				if (DailyBlip == (Blip)null)
				{
					DailyBlip = World.CreateBlip(value.Checkpoints[0]);
					DailyBlip.Sprite = (BlipSprite)38;
					DailyBlip.Color = (BlipColor)1;
					DailyBlip.Name = "Daily Race";
					DailyBlip.IsShortRange = true;
					Instance.AttachTasks();
				}
				else
				{
					Vector3 position = value.Checkpoints[0];
					DailyBlip.Position = position;
				}
				lastRequestT = default(DateTime);
			}
			else
			{
				if (DailyBlip != (Blip)null)
				{
					Instance.DetachTasks();
					((PoolObject)DailyBlip).Delete();
					DailyBlip = null;
				}
				lastRequestT = default(DateTime);
			}
		}
	}

	public static bool WaitingForDailyRaceStart { get; set; }

	public DailyRaceTrackScript()
	{
		Instance = this;
		RacingScript.LeftStartingPosition += OnLeftStartingPosition;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		try
		{
			if (e.CurrentJobEnum.IsPublicService())
			{
				StoredDailyRaceTrack = CurrentDailyRaceTrack;
				CurrentDailyRaceTrack = null;
			}
			else
			{
				CurrentDailyRaceTrack = StoredDailyRaceTrack;
				FlashDailyBlip(10000);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void OnLeftStartingPosition(object sender, EventArgs e)
	{
		if (WaitingForDailyRaceStart)
		{
			BaseScript.TriggerServerEvent("gtacnr:racing:leave", new object[0]);
			WaitingForDailyRaceStart = false;
		}
	}

	protected override async void OnStarted()
	{
		string text = await TriggerServerEventAsync<string>("gtacnr:racing:getDaily", new object[0]);
		if (!string.IsNullOrEmpty(text))
		{
			StoredDailyRaceTrack = text.Unjson<DailyRaceTrack>();
			if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
			{
				CurrentDailyRaceTrack = StoredDailyRaceTrack;
				FlashDailyBlip();
			}
		}
	}

	[EventHandler("gtacnr:racing:newDaily")]
	private async void OnNewDailyRaceTrack(string dailyJson)
	{
		TimeToStart = default(DateTime);
		if (string.IsNullOrEmpty(dailyJson))
		{
			StoredDailyRaceTrack = null;
			CurrentDailyRaceTrack = null;
			return;
		}
		StoredDailyRaceTrack = dailyJson.Unjson<DailyRaceTrack>();
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			CurrentDailyRaceTrack = null;
			return;
		}
		CurrentDailyRaceTrack = StoredDailyRaceTrack;
		Utils.SendNotification("~y~New Daily Race!~s~\n~b~" + CurrentDailyRaceTrack.Name + "~s~ by ~c~" + CurrentDailyRaceTrack.AuthorUsername + "~s~\nReward: ~g~" + CurrentDailyRaceTrack.RewardAmount.ToCurrencyString() + "~s~");
		Game.PlaySound("Event_Message_Purple", "GTAO_FM_Events_Soundset");
		FlashDailyBlip();
	}

	[EventHandler("gtacnr:racing:dailyStarted")]
	private void OnDailyRaceStarted()
	{
		Debug.WriteLine("daily started");
		CurrentDailyRaceTrack = null;
	}

	[EventHandler("gtacnr:racing:dailyStarting")]
	private void OnDailyRaceStarted(int startingInSeconds)
	{
		TimeToStart = DateTime.UtcNow.AddSeconds(startingInSeconds);
	}

	[EventHandler("gtacnr:racing:dailyUpdatePlayerCount")]
	private void OnUpdatePlayerCount(int playersJoined)
	{
		if (CurrentDailyRaceTrack != null)
		{
			CurrentDailyRaceTrack.PlayersCount = playersJoined;
		}
		if (StoredDailyRaceTrack != null)
		{
			StoredDailyRaceTrack.PlayersCount = playersJoined;
		}
	}

	private static async void FlashDailyBlip(int timeMilis = 30000)
	{
		Blip blip = DailyBlip;
		if (!(blip == (Blip)null) && ((PoolObject)blip).Exists() && !blip.IsFlashing)
		{
			blip.IsShortRange = false;
			blip.IsFlashing = true;
			await BaseScript.Delay(timeMilis);
			if (!(blip == (Blip)null) && ((PoolObject)blip).Exists())
			{
				blip.IsShortRange = true;
				blip.IsFlashing = false;
			}
		}
	}

	public void AttachTasks()
	{
		if (!TasksAttached)
		{
			base.Update += DrawTask;
			base.Update += DistanceCheckTask;
			base.Update += UpdateTextTask;
			TasksAttached = true;
		}
	}

	public void DetachTasks()
	{
		if (TasksAttached)
		{
			base.Update -= DrawTask;
			base.Update -= DistanceCheckTask;
			base.Update -= UpdateTextTask;
			TasksAttached = false;
		}
	}

	private async Coroutine DrawTask()
	{
		if (CurrentDailyRaceTrack == null)
		{
			return;
		}
		Vector3 val = CurrentDailyRaceTrack.Checkpoints[0];
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (!(((Vector3)(ref position)).DistanceToSquared2D(val) > 200f.Square()))
		{
			if (!RacingScript.StartingPosition.HasValue)
			{
				World.DrawMarker((MarkerType)1, val, Vector3.Zero, Vector3.Zero, new Vector3(5f, 5f, 0.75f), System.Drawing.Color.FromArgb(-2135228416), false, false, false, (string)null, (string)null, false);
			}
			Utils.Draw3DText(MarkerLines, val + new Vector3(0f, 0f, 1.5f), null, 0.35f);
		}
	}

	private async Coroutine DistanceCheckTask()
	{
		await BaseScript.Delay(2000);
		if (CurrentDailyRaceTrack != null && Gtacnr.Utils.CheckTimePassed(lastRequestT, 10000.0) && !RacingScript.StartingPosition.HasValue)
		{
			Vector3 val = CurrentDailyRaceTrack.Checkpoints[0];
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(val) < 25f)
			{
				lastRequestT = DateTime.UtcNow;
				BaseScript.TriggerServerEvent("gtacnr:racing:getDailyRaceInvite", new object[0]);
			}
		}
	}

	private async Coroutine UpdateTextTask()
	{
		await BaseScript.Delay(1000);
		if (CurrentDailyRaceTrack != null)
		{
			string[] array = new string[4]
			{
				"Name: " + CurrentDailyRaceTrack.Name + "~n~Author: ~b~" + CurrentDailyRaceTrack.AuthorUsername + "~s~",
				"",
				"~n~~s~Reward: ~g~" + CurrentDailyRaceTrack.RewardAmount.ToCurrencyString(),
				""
			};
			if (CurrentDailyRaceTrack.BestLapTime != default(TimeSpan))
			{
				array[1] = "~n~~s~Best Lap: ~y~" + Gtacnr.Utils.FormatTimerString(CurrentDailyRaceTrack.BestLapTime, includeMiliseconds: true) + " ~s~by ~b~" + CurrentDailyRaceTrack.BestLapTimeUsername + "~s~";
			}
			else
			{
				array[1] = "~n~~s~Best Lap: ~y~N/A~s~";
			}
			if (TimeToStart == default(DateTime))
			{
				array[3] = $"~n~~s~Waiting for players... ({CurrentDailyRaceTrack.PlayersCount}/{CurrentDailyRaceTrack.PlayersNeeded})";
			}
			else if (DateTime.UtcNow > TimeToStart)
			{
				array[3] = "~n~~r~Already started!";
			}
			else
			{
				array[3] = "~n~~s~Countdown ~y~" + Gtacnr.Utils.CalculateTimeIn(TimeToStart);
			}
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Substring(0, Math.Min(array[i].Length, 99));
			}
			MarkerLines = array;
		}
	}
}
