using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using LunarLabs.Parser;
using LunarLabs.Parser.XML;
using OpenCCNET;

namespace Gtacnr.Localization;

public static class LocalizationController
{
	public class LocalizationID
	{
		private string _value;

		public LocalizationID(string value)
		{
			_value = value;
		}

		public static explicit operator LocalizationID(string value)
		{
			return new LocalizationID(value);
		}

		public static explicit operator string(LocalizationID value)
		{
			return value._value;
		}
	}

	public static readonly string FALLBACK_LANG = "en-US";

	private static string currentLanguage = FALLBACK_LANG;

	private static readonly Dictionary<string, Dictionary<string, string>> localizedStrings = InitLocalizedStrings();

	public static string CurrentLanguage
	{
		get
		{
			return currentLanguage;
		}
		set
		{
			string oldLanguage = currentLanguage;
			currentLanguage = value;
			LocalizationController.LanguageChanged?.Invoke(null, new LanguageChangedEventArgs(oldLanguage, currentLanguage));
		}
	}

	public static event EventHandler<LanguageChangedEventArgs> LanguageChanged;

	public static Dictionary<string, string> Get(string language)
	{
		try
		{
			List<string> list = Utils.LoadJson<List<string>>("gtacnr_localization", "index.json");
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (string item in list)
			{
				foreach (DataNode item2 in XMLReader.ReadFromString(API.LoadResourceFile("gtacnr_localization", item)).Children.First().Children.Where((DataNode e) => e.Name == "Entry"))
				{
					foreach (DataNode item3 in item2.Children.Where((DataNode e) => e.Name == "String"))
					{
						try
						{
							if (item3["xml:lang"].Value == language)
							{
								string value = item2["Id"].Value;
								if (!dictionary.ContainsKey(value))
								{
									dictionary.Add(value, item3.Value);
									break;
								}
								Debug.WriteLine("^3Warning: found duplicate string for entry `" + value + "` in language " + language + " (" + item + ").");
							}
						}
						catch (Exception ex)
						{
							Debug.WriteLine("^1File: " + item);
							Debug.WriteLine("^1Entry: " + item2["Id"].Value);
							Debug.WriteLine("^1Language: " + item3["xml:lang"].Value);
							Debug.WriteLine("^1" + ex.ToString() + "^0");
						}
					}
				}
			}
			if (language == "zh-Hans")
			{
				ZhConverter.Initialize();
			}
			return dictionary;
		}
		catch (Exception ex2)
		{
			Debug.WriteLine("^1" + ex2.ToString() + "^0");
			return new Dictionary<string, string>();
		}
	}

	public static Dictionary<string, Dictionary<string, string>> GetAll()
	{
		try
		{
			List<string> list = Utils.LoadJson<List<string>>("gtacnr_localization", "index.json");
			Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
			foreach (string item in list)
			{
				foreach (DataNode item2 in XMLReader.ReadFromString(API.LoadResourceFile("gtacnr_localization", item)).Children.First().Children.Where((DataNode c) => c.Name == "Entry"))
				{
					foreach (DataNode item3 in item2.Children.Where((DataNode c) => c.Name == "String"))
					{
						try
						{
							string value = item3["xml:lang"].Value;
							string value2 = item2["Id"].Value;
							if (!dictionary.ContainsKey(value))
							{
								dictionary[value] = new Dictionary<string, string>();
							}
							if (dictionary[value].ContainsKey(value2))
							{
								Debug.WriteLine("^3Warning: found duplicate string for entry `" + value2 + "` in language " + value + " (" + item + ").");
							}
							else
							{
								dictionary[value].Add(value2, item3.Value);
							}
						}
						catch (Exception ex)
						{
							Debug.WriteLine("^1File: " + item);
							Debug.WriteLine("^1Entry: " + item2["Id"].Value);
							Debug.WriteLine("^1Language: " + item3["xml:lang"].Value);
							Debug.WriteLine("^1" + ex.ToString() + "^0");
						}
					}
				}
			}
			ZhConverter.Initialize();
			return dictionary;
		}
		catch (Exception ex2)
		{
			Debug.WriteLine("^1" + ex2.ToString() + "^0");
			return new Dictionary<string, Dictionary<string, string>>();
		}
	}

	private static Dictionary<string, Dictionary<string, string>> InitLocalizedStrings()
	{
		string value = Preferences.PreferredLanguage.Get();
		if (!string.IsNullOrEmpty(value))
		{
			CurrentLanguage = value;
		}
		Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>> { 
		{
			FALLBACK_LANG,
			Get(FALLBACK_LANG)
		} };
		if (CurrentLanguage != FALLBACK_LANG)
		{
			dictionary[CurrentLanguage] = Get(CurrentLanguage);
		}
		return dictionary;
	}

	public static void Load(string language)
	{
		localizedStrings[language] = Get(language);
		if (language == "zh-Hans")
		{
			Load("zh-Hant");
		}
	}

	public static void Unload(string language)
	{
		if (!(language == FALLBACK_LANG))
		{
			localizedStrings.Remove(language);
			if (language == "zh-Hans")
			{
				localizedStrings.Remove("zh-Hant");
			}
		}
	}

	public static string GetLocaleFromGTALanguage()
	{
		return API.GetCurrentLanguage() switch
		{
			0 => "en-US", 
			1 => "fr-FR", 
			2 => "de-DE", 
			3 => "it-IT", 
			4 => "es-ES", 
			5 => "pt-BR", 
			6 => "pl-PL", 
			9 => "zh-Hant", 
			11 => "es-ES", 
			12 => "zh-Hans", 
			_ => FALLBACK_LANG, 
		};
	}

	public static string GetLocalizedString(string language, string key, params object[] args)
	{
		try
		{
			if (!localizedStrings.ContainsKey(language))
			{
				if (!localizedStrings.ContainsKey(FALLBACK_LANG))
				{
					throw new InvalidOperationException("Fallback language `" + FALLBACK_LANG + "` has not been loaded.");
				}
				language = FALLBACK_LANG;
			}
			bool flag = false;
			if (!localizedStrings[language].ContainsKey(key))
			{
				if (language == "zh-Hans" && localizedStrings.ContainsKey("zh-Hant") && localizedStrings["zh-Hant"].ContainsKey(key))
				{
					flag = true;
					language = "zh-Hant";
				}
				else
				{
					if (!localizedStrings.ContainsKey(FALLBACK_LANG))
					{
						throw new InvalidOperationException("Fallback language `" + FALLBACK_LANG + "` has not been loaded. Unable to get entry with key `" + key + "`.");
					}
					if (!localizedStrings[FALLBACK_LANG].ContainsKey(key))
					{
						throw new InvalidOperationException("Fallback language `" + FALLBACK_LANG + "` does not have an entry with key `" + key + "`.");
					}
					language = FALLBACK_LANG;
				}
			}
			string text = localizedStrings[language][key].Replace("(C)", "<C>").Replace("(/C)", "</C>").Replace("\\n", "\n");
			if (flag)
			{
				text = ZhConverter.HantToHans(text);
			}
			return (args.Length != 0) ? string.Format(text, args) : text;
		}
		catch (Exception exception)
		{
			Debug.WriteLine("^1Error while retrieving entry with key `" + key + "` with args " + args.Json() + " for language `" + language + "`:");
			Utils.PrintException(exception);
			return "~r~[Missing string]~s~";
		}
	}

	public static string S(string key, params object[] args)
	{
		if (string.IsNullOrEmpty(currentLanguage))
		{
			Utils.PrintException(new InvalidOperationException("The current language has not been set."));
			return "~r~[Missing string]~s~";
		}
		return GetLocalizedString(currentLanguage, key, args);
	}
}
