using System;

namespace Gtacnr.Model;

public readonly struct DbTimeSpan : IEquatable<DbTimeSpan>
{
	public TimeSpan Value { get; }

	public DbTimeSpan(TimeSpan value)
	{
		Value = value;
	}

	public static implicit operator DbTimeSpan(TimeSpan ts)
	{
		return new DbTimeSpan(ts);
	}

	public static implicit operator TimeSpan(DbTimeSpan dts)
	{
		return dts.Value;
	}

	public override string ToString()
	{
		return Value.ToString();
	}

	public bool Equals(DbTimeSpan other)
	{
		return Value.Equals(other.Value);
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is DbTimeSpan other))
		{
			if (!(obj is TimeSpan obj2))
			{
				return false;
			}
			return Value.Equals(obj2);
		}
		return Equals(other);
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public static bool operator ==(DbTimeSpan left, DbTimeSpan right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DbTimeSpan left, DbTimeSpan right)
	{
		return !(left == right);
	}
}
