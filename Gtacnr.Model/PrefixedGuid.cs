using System;
using Newtonsoft.Json;

namespace Gtacnr.Model;

[JsonConverter(typeof(PrefixedGuidJsonConverter))]
public abstract class PrefixedGuid : IEquatable<PrefixedGuid>
{
	private Guid Value;

	protected abstract string Prefix { get; }

	public PrefixedGuid(Guid value)
	{
		Value = value;
	}

	public PrefixedGuid(string prefixedGuidString)
	{
		string text = Prefix + "-";
		if (prefixedGuidString.StartsWith(text))
		{
			string input = prefixedGuidString.Substring(text.Length);
			Value = Guid.Parse(input);
			return;
		}
		throw new FormatException("Invalid prefixed Guid format.");
	}

	public static explicit operator Guid(PrefixedGuid base64ID)
	{
		return base64ID.Value;
	}

	public override string ToString()
	{
		return $"{Prefix}-{Value}";
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		if (obj is PrefixedGuid other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(PrefixedGuid? other)
	{
		if ((object)other == null || other.GetType() != GetType())
		{
			return false;
		}
		return Value.Equals(other.Value);
	}

	public static bool operator ==(PrefixedGuid? a, PrefixedGuid? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return a.Equals(b);
	}

	public static bool operator !=(PrefixedGuid? a, PrefixedGuid? b)
	{
		return !(a == b);
	}
}
