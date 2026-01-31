using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Gtacnr;
using Gtacnr.Client;

namespace OpenCCNET;

public static class ZhConverter
{
	public static class ZhDictionary
	{
		private static bool wasInitialized;

		public static IDictionary<string, string> TSCharacters { get; set; }

		public static void Initialize()
		{
			if (!wasInitialized)
			{
				wasInitialized = true;
				TSCharacters = LoadDictionary("TSCharacters");
			}
		}

		private static IDictionary<string, string> LoadDictionary(string name)
		{
			string[] array = Gtacnr.Utils.LoadEmbeddedResource(typeof(Script).Namespace + ".Libs.OpenCCNet." + name + ".txt").Replace("\r", "").Split('\n');
			Dictionary<string, string> dictionary = new Dictionary<string, string>(10000);
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					string[] array3 = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
					if (array3.Length >= 2)
					{
						dictionary[array3[0]] = array3[1];
					}
				}
			}
			return dictionary;
		}
	}

	public static class ZhSegment
	{
		public static IEnumerable<string> Segment(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return new _003C_003Ez__ReadOnlySingleElementList<string>(text);
			}
			return from c in text.ToCharArray()
				select c.ToString();
		}
	}

	public static void Initialize()
	{
		ZhDictionary.Initialize();
	}

	public static string HantToHans(string text)
	{
		return ZhSegment.Segment(text).ConvertBy(ZhDictionary.TSCharacters).Join();
	}

	public static string ToHansFromHant(this string text)
	{
		return HantToHans(text);
	}

	private static IEnumerable<string> ConvertBy(this IEnumerable<string> phrases, params IDictionary<string, string>[] dictionaries)
	{
		if (phrases == null)
		{
			throw new ArgumentNullException("phrases");
		}
		if (dictionaries.Length == 0)
		{
			return phrases;
		}
		IList<string> source;
		if (!(phrases is IList<string> list))
		{
			IList<string> list2 = phrases.ToList();
			source = list2;
		}
		else
		{
			source = list;
		}
		return source.Select((string phrase) => ConvertPhrase(phrase, dictionaries)).ToList();
	}

	private static string ConvertPhrase(string phrase, params IDictionary<string, string>[] dictionaries)
	{
		if (string.IsNullOrEmpty(phrase))
		{
			return phrase;
		}
		IDictionary<string, string>[] array = dictionaries;
		foreach (IDictionary<string, string> dictionary in array)
		{
			if (dictionary != null && dictionary.TryGetValue(phrase, out var value))
			{
				return value;
			}
		}
		TextElementEnumerator textElementEnumerator = StringInfo.GetTextElementEnumerator(phrase);
		StringBuilder stringBuilder = new StringBuilder(phrase.Length * 2);
		bool flag = false;
		while (textElementEnumerator.MoveNext())
		{
			string textElement = textElementEnumerator.GetTextElement();
			string value2 = textElement;
			array = dictionaries;
			foreach (IDictionary<string, string> dictionary2 in array)
			{
				if (dictionary2 != null && dictionary2.TryGetValue(textElement, out var value3))
				{
					value2 = value3;
					flag = true;
					break;
				}
			}
			stringBuilder.Append(value2);
		}
		if (!flag)
		{
			return phrase;
		}
		return stringBuilder.ToString();
	}

	private static string Join(this IEnumerable<string> values, string separator = "")
	{
		return string.Join(separator, values);
	}
}
