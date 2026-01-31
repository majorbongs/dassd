using System;
using System.Drawing;

namespace Gtacnr;

public struct Color
{
	public byte R { get; set; }

	public byte G { get; set; }

	public byte B { get; set; }

	public byte A { get; set; }

	public Color(byte r, byte g, byte b, byte a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public Color(byte r, byte g, byte b)
		: this(r, g, b, byte.MaxValue)
	{
	}

	public static implicit operator int(Color color)
	{
		return color.ToInt();
	}

	public static implicit operator Color(int value)
	{
		return FromInt(value);
	}

	public static implicit operator Color(uint value)
	{
		return FromInt((int)value);
	}

	public int ToInt()
	{
		return (R << 24) + (G << 16) + (B << 8) + A;
	}

	public static Color FromInt(int value)
	{
		byte r = (byte)((value >> 24) & 0xFF);
		byte g = (byte)((value >> 16) & 0xFF);
		byte b = (byte)((value >> 8) & 0xFF);
		byte a = (byte)(value & 0xFF);
		return new Color(r, g, b, a);
	}

	public static Color FromUint(uint value)
	{
		byte r = (byte)((value >> 24) & 0xFF);
		byte g = (byte)((value >> 16) & 0xFF);
		byte b = (byte)((value >> 8) & 0xFF);
		byte a = (byte)(value & 0xFF);
		return new Color(r, g, b, a);
	}

	public static Color FromHexString(string hexStringRgba)
	{
		return FromInt(Convert.ToInt32(hexStringRgba, 16));
	}

	public string ToHexString(bool alpha = true)
	{
		if (!alpha)
		{
			return $"#{R:X2}{G:X2}{B:X2}";
		}
		return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
	}

	public override string ToString()
	{
		return $"rgba({R}, {G}, {B}, {A})";
	}

	public System.Drawing.Color ToSystemColor()
	{
		return System.Drawing.Color.FromArgb(A, R, G, B);
	}
}
