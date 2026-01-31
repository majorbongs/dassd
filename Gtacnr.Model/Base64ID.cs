using System;

namespace Gtacnr.Model;

public abstract class Base64ID
{
	private ulong Id;

	protected abstract string Prefix { get; }

	public Base64ID(ulong id)
	{
		Id = id;
	}

	public Base64ID(string base64Id)
	{
		if (base64Id.StartsWith(Prefix))
		{
			string text = base64Id.Substring(Prefix.Length);
			text = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
			byte[] array = Convert.FromBase64String(text);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(array);
			}
			ulong id = BitConverter.ToUInt64(array, 0);
			Id = id;
			return;
		}
		throw new FormatException("Invalid base64 ID format.");
	}

	public static explicit operator ulong(Base64ID base64ID)
	{
		return base64ID.Id;
	}

	public override string ToString()
	{
		byte[] bytes = BitConverter.GetBytes(Id);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		string text = Convert.ToBase64String(bytes).TrimEnd('=');
		return Prefix + text;
	}
}
