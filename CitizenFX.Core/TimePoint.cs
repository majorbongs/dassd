namespace CitizenFX.Core;

public readonly struct TimePoint
{
	private readonly ulong m_time;

	public TimePoint(ulong timeInMilliseconds)
	{
		m_time = timeInMilliseconds;
	}

	public static TimePoint operator +(TimePoint timePoint, ulong timeInMilliseconds)
	{
		return new TimePoint(timePoint.m_time + timeInMilliseconds);
	}

	public static TimePoint operator -(TimePoint timePoint, ulong timeInMilliseconds)
	{
		return new TimePoint(timePoint.m_time - timeInMilliseconds);
	}

	public static TimePoint operator *(TimePoint timePoint, ulong timeInMilliseconds)
	{
		return new TimePoint(timePoint.m_time * timeInMilliseconds);
	}

	public static TimePoint operator /(TimePoint timePoint, ulong timeInMilliseconds)
	{
		return new TimePoint(timePoint.m_time / timeInMilliseconds);
	}

	public static implicit operator ulong(TimePoint timePoint)
	{
		return timePoint.m_time;
	}

	public static explicit operator TimePoint(ulong timeInMilliseconds)
	{
		return new TimePoint(timeInMilliseconds);
	}

	public override string ToString()
	{
		return m_time.ToString();
	}
}
