using System.Text;
using Newtonsoft.Json;

namespace Gtacnr.Client.Libs;

public static class Utils
{
	public static string Json(this object obj)
	{
		return JsonConvert.SerializeObject(obj);
	}

	public static T Unjson<T>(this string json)
	{
		return JsonConvert.DeserializeObject<T>(json);
	}

	public static string ReplaceNonAsciiChars(string input, char replacementChar)
	{
		StringBuilder stringBuilder = new StringBuilder(input.Length);
		foreach (char c in input)
		{
			if (c >= ' ' && c <= '~')
			{
				stringBuilder.Append(c);
			}
			else
			{
				stringBuilder.Append(replacementChar);
			}
		}
		return stringBuilder.ToString();
	}
}
