using System;
using Gtacnr.Model.Exceptions;

namespace Gtacnr;

public static class UTF32Polyfill
{
	private static class CharUnicodeInfo
	{
		internal const char HIGH_SURROGATE_START = '\ud800';

		internal const char HIGH_SURROGATE_END = '\udbff';

		internal const char LOW_SURROGATE_START = '\udc00';

		internal const char LOW_SURROGATE_END = '\udfff';

		internal const int UNICODE_CATEGORY_OFFSET = 0;

		internal const int BIDI_CATEGORY_OFFSET = 1;
	}

	internal const int UNICODE_PLANE00_END = 65535;

	internal const int UNICODE_PLANE01_START = 65536;

	internal const int UNICODE_PLANE16_END = 1114111;

	internal const int HIGH_SURROGATE_START = 55296;

	internal const int LOW_SURROGATE_END = 57343;

	public static bool IsHighSurrogate(char c)
	{
		if (c >= '\ud800')
		{
			return c <= '\udbff';
		}
		return false;
	}

	public static bool IsLowSurrogate(char c)
	{
		if (c >= '\udc00')
		{
			return c <= '\udfff';
		}
		return false;
	}

	public static string ConvertFromUtf32(int utf32)
	{
		if (utf32 < 0 || utf32 > 1114111 || (utf32 >= 55296 && utf32 <= 57343))
		{
			throw new CustomArgumentOutOfRangeException("utf32", "InvalidUTF32");
		}
		if (utf32 < 65536)
		{
			return char.ToString((char)utf32);
		}
		utf32 -= 65536;
		return new string(new char[2]
		{
			(char)(utf32 / 1024 + 55296),
			(char)(utf32 % 1024 + 56320)
		});
	}

	public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
	{
		if (!IsHighSurrogate(highSurrogate))
		{
			throw new CustomArgumentOutOfRangeException("highSurrogate", "InvalidHighSurrogate");
		}
		if (!IsLowSurrogate(lowSurrogate))
		{
			throw new CustomArgumentOutOfRangeException("lowSurrogate", "InvalidLowSurrogate");
		}
		return (highSurrogate - 55296) * 1024 + (lowSurrogate - 56320) + 65536;
	}

	public static int ConvertToUtf32(string s, int index)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (index < 0 || index >= s.Length)
		{
			throw new CustomArgumentOutOfRangeException("index", "Index");
		}
		int num = s[index] - 55296;
		if (num >= 0 && num <= 2047)
		{
			if (num <= 1023)
			{
				if (index < s.Length - 1)
				{
					int num2 = s[index + 1] - 56320;
					if (num2 >= 0 && num2 <= 1023)
					{
						return num * 1024 + num2 + 65536;
					}
					throw new ArgumentException("InvalidHighSurrogate", "s");
				}
				throw new ArgumentException("InvalidHighSurrogate", "s");
			}
			throw new ArgumentException("InvalidLowSurrogate", "s");
		}
		return s[index];
	}
}
