using System;
using CitizenFX.Core.Native;
using Gtacnr.Client.Sync;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class GameTime
{
	[JsonIgnore]
	private DateTime dateTime = new DateTime(2023, 1, 1, 0, 0, 0);

	public DayOfWeek Day { get; set; }

	public int Hour
	{
		get
		{
			return dateTime.Hour;
		}
		set
		{
			dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, value, Minute, 0);
		}
	}

	public int Minute
	{
		get
		{
			return dateTime.Minute;
		}
		set
		{
			dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, Hour, value, 0);
		}
	}

	[JsonIgnore]
	public static GameTime Now => TimeSyncScript.GameTime;

	[JsonIgnore]
	public static GameTime RestartTime => new GameTime(DayOfWeek.Saturday, 23, 59);

	public GameTime(DayOfWeek day, int hour, int minute)
	{
		Day = day;
		Hour = hour;
		Minute = minute;
	}

	public GameTime(int hour, int minute)
		: this(DayOfWeek.Sunday, hour, minute)
	{
	}

	public GameTime(GameTime gt)
		: this(gt.Day, gt.Hour, gt.Minute)
	{
	}

	public GameTime()
		: this(DayOfWeek.Sunday, 0, 0)
	{
	}

	public static TimeSpan ToRealLifeTimeSpan(TimeSpan gameTimeSpan)
	{
		return TimeSpan.FromSeconds(gameTimeSpan.TotalMinutes * 1.0);
	}

	public void AddHours(int n)
	{
		dateTime = dateTime.AddHours(n);
	}

	public void AddHour()
	{
		AddHours(1);
	}

	public void AddMinutes(int n)
	{
		dateTime = dateTime.AddMinutes(n);
	}

	public void AddMinute()
	{
		AddMinutes(1);
	}

	public void AddDay()
	{
		if (Day != DayOfWeek.Saturday)
		{
			Day++;
		}
		else
		{
			Day = DayOfWeek.Sunday;
		}
	}

	public static bool operator ==(GameTime a, GameTime b)
	{
		if ((object)a == null || (object)b == null || a.Compare(b) != 0)
		{
			if ((object)a == null)
			{
				return (object)b == null;
			}
			return false;
		}
		return true;
	}

	public static bool operator !=(GameTime a, GameTime b)
	{
		return !(a == b);
	}

	public static bool operator <(GameTime a, GameTime b)
	{
		if ((object)a != null && (object)b != null)
		{
			return a.Compare(b) < 0;
		}
		return false;
	}

	public static bool operator >(GameTime a, GameTime b)
	{
		if ((object)a != null && (object)b != null)
		{
			return a.Compare(b) > 0;
		}
		return false;
	}

	public static bool operator <=(GameTime a, GameTime b)
	{
		if ((object)a != null && (object)b != null)
		{
			return a.Compare(b) <= 0;
		}
		return false;
	}

	public static bool operator >=(GameTime a, GameTime b)
	{
		if ((object)a != null && (object)b != null)
		{
			return a.Compare(b) >= 0;
		}
		return false;
	}

	public static TimeSpan operator -(GameTime a, GameTime b)
	{
		int num = a.Day - b.Day;
		int num2 = a.Hour - b.Hour + num * 24;
		return TimeSpan.FromMinutes(a.Minute - b.Minute + num2 * 60);
	}

	public int Compare(GameTime other)
	{
		if (Day != other.Day)
		{
			if (Day >= other.Day)
			{
				return 1;
			}
			return -1;
		}
		if (Hour != other.Hour)
		{
			if (Hour >= other.Hour)
			{
				return 1;
			}
			return -1;
		}
		if (Minute != other.Minute)
		{
			if (Minute >= other.Minute)
			{
				return 1;
			}
			return -1;
		}
		return 0;
	}

	public override bool Equals(object obj)
	{
		return this == (GameTime)obj;
	}

	public override int GetHashCode()
	{
		return API.GetHashKey($"{(int)Day}:{Hour:00}:{Minute:00}");
	}

	public override string ToString()
	{
		return $"{Day} {Hour:00}:{Minute:00}";
	}
}
