using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Model;

namespace Gtacnr.Client.Sync;

public class TimeSyncScript : Script
{
	private static GameTime gameTime = new GameTime();

	private static GameTime? overrideTime = null;

	private static TimeSyncScript instance;

	private DateTime lastPressTime = DateTime.MinValue;

	private DateTime startDateTime;

	private bool isHudShown;

	private bool firstSyncDone;

	public static GameTime GameTime => new GameTime(gameTime);

	public static GameTime? OverrideTime
	{
		get
		{
			return overrideTime;
		}
		set
		{
			if (value != null && overrideTime == null)
			{
				instance.AttachOverrideTimeTask();
			}
			else if (value == null && overrideTime != null)
			{
				instance.DetachOverrideTimeTask();
			}
			overrideTime = value;
		}
	}

	public TimeSyncScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		Display();
	}

	private void AttachOverrideTimeTask()
	{
		base.Update += OverrideTimeTask;
	}

	private void DetachOverrideTimeTask()
	{
		base.Update -= OverrideTimeTask;
	}

	private async Coroutine OverrideTimeTask()
	{
		if (!(OverrideTime == null))
		{
			int hour = OverrideTime.Hour;
			int minute = OverrideTime.Minute;
			API.SetClockTime(hour, minute, 0);
			API.NetworkOverrideClockTime(hour, minute, 0);
		}
	}

	private void SetTime(int hour, int minute)
	{
		try
		{
			if (!(OverrideTime != null))
			{
				gameTime.Hour = hour;
				gameTime.Minute = minute;
				API.SetClockTime(hour, minute, 0);
				API.NetworkOverrideClockTime(hour, minute, 0);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:time")]
	private void OnTime(int hour, int minute, int dayOfWeek)
	{
		if (hour >= 0 && minute >= 0 && hour <= 23 && minute <= 59)
		{
			API.SetMillisecondsPerGameMinute(1000);
			API.NetworkOverrideClockMillisecondsPerGameMinute(1000);
			BaseScript.TriggerEvent("gtacnr:hud:setTime", new object[3] { hour, minute, dayOfWeek });
			SetTime(hour, minute);
			gameTime.Day = (DayOfWeek)dayOfWeek;
			API.SetClockDate(startDateTime.Day + dayOfWeek * 2, startDateTime.Month, startDateTime.Year);
			if (minute == 0 || !firstSyncDone)
			{
				firstSyncDone = true;
				Print(gameTime.ToString());
				Display();
			}
		}
	}

	[EventHandler("gtacnr:day")]
	private void OnDay(int dayOfWeek)
	{
		gameTime.Day = (DayOfWeek)dayOfWeek;
		API.SetClockDate(startDateTime.Day + dayOfWeek * 2, startDateTime.Month, startDateTime.Year);
	}

	[Update]
	private async Coroutine OnTick()
	{
		if (API.IsControlJustPressed(2, 20))
		{
			lastPressTime = DateTime.UtcNow;
		}
		if (!Gtacnr.Utils.CheckTimePassed(lastPressTime, 4250.0))
		{
			if (!isHudShown)
			{
				isHudShown = true;
				BaseScript.TriggerEvent("gtacnr:hud:toggleTime", new object[1] { true });
			}
		}
		else if (isHudShown)
		{
			isHudShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleTime", new object[1] { false });
		}
	}

	private void Display()
	{
		lastPressTime = DateTime.UtcNow;
	}

	[EventHandler("onClientResourceStart")]
	private void OnClientResourceStart(string resourceName)
	{
		if (resourceName == "gtacnr_ui" && SpawnScript.HasSpawned)
		{
			Display();
		}
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		Display();
	}
}
