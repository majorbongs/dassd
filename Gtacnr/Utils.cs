using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Jobs.PrivateMedic;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Newtonsoft.Json.Linq;

namespace Gtacnr;

public static class Utils
{
	public static class JobMapper
	{
		private static readonly Dictionary<string, JobsEnum> _jobMap = new Dictionary<string, JobsEnum>
		{
			{
				"none",
				JobsEnum.None
			},
			{
				"staff",
				JobsEnum.Staff
			},
			{
				"police",
				JobsEnum.Police
			},
			{
				"paramedic",
				JobsEnum.Paramedic
			},
			{
				"firefighter",
				JobsEnum.Firefighter
			},
			{
				"security",
				JobsEnum.Security
			},
			{
				"mechanic",
				JobsEnum.Mechanic
			},
			{
				"taxiDriver",
				JobsEnum.TaxiDriver
			},
			{
				"deliveryDriver",
				JobsEnum.DeliveryDriver
			},
			{
				"drugDealer",
				JobsEnum.DrugDealer
			},
			{
				"hitman",
				JobsEnum.Hitman
			},
			{
				"privateMedic",
				JobsEnum.PrivateMedic
			},
			{
				"armsDealer",
				JobsEnum.ArmsDealer
			}
		};

		private static readonly Dictionary<JobsEnum, string> _reverseJobMap = _jobMap.ToDictionary<KeyValuePair<string, JobsEnum>, JobsEnum, string>((KeyValuePair<string, JobsEnum> kvp) => kvp.Value, (KeyValuePair<string, JobsEnum> kvp) => kvp.Key);

		public static JobsEnum JobToEnum(string? job)
		{
			if (string.IsNullOrEmpty(job))
			{
				return JobsEnum.Invalid;
			}
			if (!_jobMap.TryGetValue(job, out var value))
			{
				return JobsEnum.Invalid;
			}
			return value;
		}

		public static string EnumToJob(JobsEnum job)
		{
			if (!_reverseJobMap.TryGetValue(job, out string value))
			{
				return string.Empty;
			}
			return value;
		}
	}

	public static class Colors
	{
		public static Color PlainText = uint.MaxValue;

		public static Color Connection = 2947526655u;

		public static Color Info = 13697023;

		public static Color Message = 3990947071u;

		public static Color Error = 3992977663u;

		public static Color Warning = 3986883839u;

		public static Color Anticheat = 885080831;

		public static Color Chatbot = 885080831;

		public static Color Kick = 2906077183u;

		public static Color Mute = 2906077183u;

		public static Color CayoPerico = 2906077183u;

		public static Color Detonation = 2906077183u;

		public static Color JobBan = 2906077183u;

		public static Color Moderation = 885080831;

		public static Color Radio = 2878108415u;

		public static Color HudRed = new Color(215, 61, 56);

		public static Color HudGreenDark = new Color(61, 101, 58);

		public static Color HudPurpleDark = new Color(68, 57, 110);

		public static Color HudBlueDark = new Color(64, 89, 118);

		public static Color HudYellowDark = new Color(126, 107, 41);
	}

	private static readonly JobsEnum[] EMSOrFDJobEnums = new JobsEnum[2]
	{
		JobsEnum.Paramedic,
		JobsEnum.Firefighter
	};

	private static readonly JobsEnum[] ReviveJobEnums = new JobsEnum[3]
	{
		JobsEnum.Paramedic,
		JobsEnum.Firefighter,
		JobsEnum.PrivateMedic
	};

	private static readonly JobsEnum[] PublicJobEnums = new JobsEnum[4]
	{
		JobsEnum.Paramedic,
		JobsEnum.Firefighter,
		JobsEnum.Police,
		JobsEnum.Security
	};

	private static readonly int[] PLAYER_LEVELS = new int[225]
	{
		8, 20, 45, 80, 140, 230, 450, 725, 1250, 1600,
		1900, 2400, 3150, 4920, 5485, 6000, 7900, 9100, 10700, 11500,
		12700, 14200, 16600, 18100, 20600, 22200, 24200, 27500, 30710, 33000,
		36740, 39540, 43700, 47580, 51400, 55755, 60000, 65000, 70000, 75000,
		80000, 85000, 90000, 95000, 100000, 105000, 110000, 115000, 120000, 125000,
		130000, 135000, 140000, 145000, 150000, 155000, 160000, 165000, 170000, 175000,
		180000, 185000, 190000, 195000, 200000, 205000, 210000, 215000, 220000, 225000,
		230000, 235000, 240000, 245000, 250000, 255000, 260000, 265000, 270000, 275000,
		280000, 285000, 290000, 295000, 300000, 305000, 310000, 315000, 320000, 325000,
		330000, 335000, 340000, 345000, 350000, 355000, 360000, 365000, 370000, 375000,
		380000, 385000, 390000, 395000, 400000, 405000, 410000, 415000, 420000, 425000,
		430000, 435000, 440000, 445000, 450000, 455000, 460000, 465000, 470000, 475000,
		480000, 485000, 490000, 495000, 500000, 505000, 510000, 515000, 520000, 525000,
		530000, 535000, 540000, 545000, 550000, 555000, 560000, 565000, 570000, 575000,
		580000, 585000, 590000, 595000, 600000, 605000, 610000, 615000, 620000, 625000,
		630000, 635000, 640000, 645000, 650000, 655000, 660000, 665000, 670000, 675000,
		680000, 685000, 690000, 695000, 700000, 705000, 710000, 715000, 720000, 725000,
		730000, 735000, 740000, 745000, 750000, 755000, 760000, 765000, 770000, 775000,
		780000, 785000, 790000, 795000, 800000, 805000, 810000, 815000, 820000, 825000,
		830000, 835000, 840000, 845000, 850000, 855000, 860000, 865000, 870000, 875000,
		880000, 885000, 890000, 895000, 900000, 905000, 910000, 915000, 920000, 925000,
		930000, 935000, 940000, 945000, 950000, 955000, 960000, 965000, 970000, 975000,
		980000, 985000, 990000, 995000, 1000000
	};

	private static Dictionary<string, HashSet<int>> restrictedVehicles = LoadJson<Dictionary<string, HashSet<string>>>("data/vehicles/restrictedVehicles.json").ToDictionary((KeyValuePair<string, HashSet<string>> kvp) => kvp.Key, (KeyValuePair<string, HashSet<string>> kvp) => new HashSet<int>(kvp.Value.Select((string i) => API.GetHashKey(i))));

	public const int MARSHAL_WAIT_TIME = 0;

	private static readonly Random random = new Random();

	public static T? Random<T>(this IEnumerable<T> collection)
	{
		int num = collection.Count();
		if (num == 0)
		{
			return default(T);
		}
		int index = random.Next(num);
		return collection.ElementAt(index);
	}

	public static bool In<T>(this T o, IEnumerable<T> collection)
	{
		return collection.Contains(o);
	}

	public static bool In<T>(this T o, params T[] collection)
	{
		return collection.Contains<T>(o);
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
	{
		Random random = new Random();
		return list.OrderBy<T, int>((T x) => random.Next());
	}

	public static void Swap<T>(this List<T> list, int indexA, int indexB)
	{
		T value = list[indexA];
		list[indexA] = list[indexB];
		list[indexB] = value;
	}

	public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
	{
		return new HashSet<T>(enumerable);
	}

	public static V? TryGetValueOrNull<K, V>(this IDictionary<K, V> dictionary, K key) where V : struct
	{
		if (!dictionary.ContainsKey(key))
		{
			return null;
		}
		return dictionary[key];
	}

	public static V? TryGetRefOrNull<K, V>(this IDictionary<K, V> dictionary, K key) where V : class
	{
		if (!dictionary.ContainsKey(key))
		{
			return null;
		}
		return dictionary[key];
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class
	{
		return enumerable.Where((T t) => t != null);
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct
	{
		return (IEnumerable<T>)enumerable.Where((T? t) => t.HasValue);
	}

	public static IEnumerable<T> UnionNotNull<T>(this IEnumerable<T> enumerable, IEnumerable<T?> otherEnumerable) where T : class
	{
		return enumerable.Union<T>(otherEnumerable.WhereNotNull());
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable, IEnumerable<T?> otherEnumerable) where T : struct
	{
		return enumerable.Union(otherEnumerable.WhereNotNull());
	}

	public static string ToHexString(this byte[] bytes)
	{
		return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
	}

	public static double ConvertRange(this double value, double fromA, double fromB, double toA, double toB)
	{
		double num = fromB - fromA;
		double num2 = toB - toA;
		if (num == 0.0 || num2 == 0.0)
		{
			return 0.0;
		}
		double num3 = num2 / num;
		double num4 = -1.0 * fromA * num3 + toA;
		return value * num3 + num4;
	}

	public static float ConvertRange(this float value, float fromA, float fromB, float toA, float toB)
	{
		return (float)((double)value).ConvertRange((double)fromA, (double)fromB, (double)toA, (double)toB);
	}

	public static int ToInt(this float f)
	{
		return Convert.ToInt32(Math.Round(f));
	}

	public static int ToInt(this double d)
	{
		return Convert.ToInt32(Math.Round(d));
	}

	public static int ToIntFloor(this float f)
	{
		return Convert.ToInt32(Math.Floor(f));
	}

	public static int ToIntFloor(this double d)
	{
		return Convert.ToInt32(Math.Floor(d));
	}

	public static int ToIntCeil(this float f)
	{
		return Convert.ToInt32(Math.Ceiling(f));
	}

	public static int ToIntCeil(this double d)
	{
		return Convert.ToInt32(Math.Ceiling(d));
	}

	public static long ToLong(this float f)
	{
		return Convert.ToInt64(Math.Round(f));
	}

	public static long ToLong(this double d)
	{
		return Convert.ToInt64(Math.Round(d));
	}

	public static long ToLongFloor(this float f)
	{
		return Convert.ToInt64(Math.Floor(f));
	}

	public static long ToLongFloor(this double d)
	{
		return Convert.ToInt64(Math.Floor(d));
	}

	public static long ToLongCeil(this float f)
	{
		return Convert.ToInt64(Math.Ceiling(f));
	}

	public static long ToLongCeil(this double d)
	{
		return Convert.ToInt64(Math.Ceiling(d));
	}

	public static float ToRadians(this float val)
	{
		return (float)Math.PI / 180f * val;
	}

	public static int ToInt(this string s)
	{
		int num = 0;
		for (int i = 0; i < s.Length; i++)
		{
			num = num * 10 + (s[i] - 48);
		}
		return num;
	}

	public static long ToLong(this string s)
	{
		long num = 0L;
		for (int i = 0; i < s.Length; i++)
		{
			num = num * 10 + (s[i] - 48);
		}
		return num;
	}

	public static float ToKmh(this float ms)
	{
		return ms * 3.6f;
	}

	public static float ToMph(this float ms)
	{
		return ms * 2.23694f;
	}

	public static float ToKts(this float ms)
	{
		return ms * 1.94384f;
	}

	public static float ToKm(this float m)
	{
		return m * 0.001f;
	}

	public static float ToMiles(this float m)
	{
		return m * 0.000621371f;
	}

	public static float ToNauticalMiles(this float m)
	{
		return m * 0.000539957f;
	}

	public static float ToFeet(this float m)
	{
		return m * 3.28084f;
	}

	public static string ToCurrencyString(this long amount, bool printFreeWhenZero = false)
	{
		if (!printFreeWhenZero || amount != 0L)
		{
			return "$" + amount.ToString("#,0", CultureInfo.InvariantCulture);
		}
		return "FREE";
	}

	public static string ToCurrencyString(this ulong amount, bool printFreeWhenZero = false)
	{
		if (!printFreeWhenZero || amount != 0L)
		{
			return "$" + amount.ToString("#,0", CultureInfo.InvariantCulture);
		}
		return "FREE";
	}

	public static string ToCurrencyString(this int amount, bool printFreeWhenZero = false)
	{
		return ((long)amount).ToCurrencyString(printFreeWhenZero);
	}

	public static string ToCurrencyString(this uint amount, bool printFreeWhenZero = false)
	{
		return ((ulong)amount).ToCurrencyString(printFreeWhenZero);
	}

	public static string ToPriceTagString(this long price, long playerMoney, bool naWhenZero = false)
	{
		string result = ((playerMoney >= price) ? "~g~" : "~r~") + price.ToCurrencyString();
		if (price == 0L)
		{
			result = ((!naWhenZero) ? LocalizationController.S(Entries.Main.FREE) : "~r~N/A");
		}
		return result;
	}

	public static string ToPriceTagString(this int price, long playerMoney, bool naWhenZero = false)
	{
		return ((long)price).ToPriceTagString(playerMoney, naWhenZero);
	}

	public static bool TryParseIntCollection(this IEnumerable<string> collection, out List<int> result)
	{
		result = new List<int>();
		foreach (string item in collection)
		{
			if (!int.TryParse(item, out var result2))
			{
				return false;
			}
			result.Add(result2);
		}
		return true;
	}

	public static T GetAttribute<T>(Enum enumValue) where T : Attribute
	{
		return (T)(enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault()?.GetCustomAttributes(typeof(T), inherit: false).FirstOrDefault());
	}

	public static string GetValue<T>(Enum value) where T : ValueAttribute
	{
		if (value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(T), inherit: false) is T[] source && source.Any())
		{
			return source.First().Value;
		}
		return value.ToString();
	}

	public static string GetDescription(Enum value)
	{
		try
		{
			if (value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), inherit: false) is DescriptionAttribute[] source && source.Any())
			{
				return source.First().Description;
			}
			return value.ToString();
		}
		catch
		{
			return string.Empty;
		}
	}

	public static bool HasExtraData(this IExtraDataContainer item, string key)
	{
		return item.ExtraData.ContainsKey(key);
	}

	public static string GetExtraDataString(this IExtraDataContainer item, string key)
	{
		if (!item.HasExtraData(key))
		{
			return null;
		}
		return (string)item.ExtraData[key];
	}

	public static bool GetExtraDataBool(this IExtraDataContainer item, string key)
	{
		if (!item.HasExtraData(key))
		{
			return false;
		}
		return Convert.ToBoolean(item.ExtraData[key]);
	}

	public static int GetExtraDataInt(this IExtraDataContainer item, string key)
	{
		if (!item.HasExtraData(key))
		{
			return -1;
		}
		return Convert.ToInt32(item.ExtraData[key]);
	}

	public static float GetExtraDataFloat(this IExtraDataContainer item, string key)
	{
		if (!item.HasExtraData(key))
		{
			return -1f;
		}
		return Convert.ToSingle(item.ExtraData[key]);
	}

	public static float[] GetExtraDataFloatArray(this IExtraDataContainer item, string key)
	{
		if (!item.HasExtraData(key))
		{
			return null;
		}
		return ((JArray)item.ExtraData[key]).ToObject<float[]>();
	}

	public static bool IsPolice(this JobsEnum job)
	{
		return job == JobsEnum.Police;
	}

	public static bool IsEMSOrFD(this JobsEnum job)
	{
		return EMSOrFDJobEnums.Contains(job);
	}

	public static bool IsPublicService(this JobsEnum job)
	{
		return PublicJobEnums.Contains(job);
	}

	public static bool IsAbleToRevive(this JobsEnum job)
	{
		return ReviveJobEnums.Contains(job);
	}

	public static byte GetGamerTagColor(JobsEnum job, int wantedLevel)
	{
		byte result = 0;
		if (job == JobsEnum.Police)
		{
			result = 9;
		}
		else if (job == JobsEnum.Paramedic)
		{
			result = 21;
		}
		else if (job == JobsEnum.Firefighter)
		{
			result = 28;
		}
		else if (job == JobsEnum.Security)
		{
			result = 37;
		}
		else if (job == JobsEnum.Mechanic && wantedLevel == 0)
		{
			result = 4;
		}
		else if (job == JobsEnum.PrivateMedic && wantedLevel == 0)
		{
			result = 30;
		}
		else
		{
			switch (wantedLevel)
			{
			case 0:
				result = 1;
				break;
			case 1:
				result = 209;
				break;
			case 2:
				result = 127;
				break;
			case 3:
				result = 179;
				break;
			case 4:
				result = 130;
				break;
			case 5:
				result = 27;
				break;
			}
		}
		return result;
	}

	public static byte GetRadarBlipColor(JobsEnum job, int wantedLevel)
	{
		byte result = 0;
		if (job == JobsEnum.Police)
		{
			result = 57;
		}
		else if (job == JobsEnum.Paramedic)
		{
			result = 50;
		}
		else if (job == JobsEnum.Firefighter)
		{
			result = 6;
		}
		else if (job == JobsEnum.Security)
		{
			result = 15;
		}
		else if (job == JobsEnum.Mechanic && wantedLevel == 0)
		{
			result = 39;
		}
		else if (job == JobsEnum.PrivateMedic && wantedLevel == 0)
		{
			result = 8;
		}
		else
		{
			switch (wantedLevel)
			{
			case 0:
				result = 0;
				break;
			case 1:
				result = 5;
				break;
			case 2:
				result = 44;
				break;
			case 3:
				result = 47;
				break;
			case 4:
				result = 64;
				break;
			case 5:
				result = 49;
				break;
			}
		}
		return result;
	}

	public static int GetRadarBlipSprite(JobsEnum job, int wantedLevel)
	{
		int result = 1;
		switch (job)
		{
		case JobsEnum.Police:
			result = 593;
			break;
		case JobsEnum.Paramedic:
		case JobsEnum.Firefighter:
			result = 419;
			break;
		case JobsEnum.Security:
			result = 605;
			break;
		default:
			if (wantedLevel == 0)
			{
				result = 1;
			}
			else if (wantedLevel < 5)
			{
				result = 607;
			}
			else if (wantedLevel == 5)
			{
				result = 594;
			}
			break;
		}
		return result;
	}

	public static uint GetColorRGB(JobsEnum job, int wantedLevel)
	{
		uint result = 0u;
		if (job == JobsEnum.Police)
		{
			result = 10675967u;
		}
		else if (job == JobsEnum.Paramedic)
		{
			result = 2354700799u;
		}
		else if (job == JobsEnum.Firefighter)
		{
			result = 3193000703u;
		}
		else if (job == JobsEnum.Security)
		{
			result = 1908719359u;
		}
		else if (job == JobsEnum.Mechanic && wantedLevel == 0)
		{
			result = 2593823487u;
		}
		else if (job == JobsEnum.PrivateMedic && wantedLevel == 0)
		{
			result = 4269458431u;
		}
		else
		{
			switch (job)
			{
			case JobsEnum.Staff:
				result = 1037254143u;
				break;
			case JobsEnum.Invalid:
				result = 808464639u;
				break;
			default:
				switch (wantedLevel)
				{
				case 0:
					result = 4042322175u;
					break;
				case 1:
					result = 4292673791u;
					break;
				case 2:
					result = 4290183423u;
					break;
				case 3:
					result = 4288217343u;
					break;
				case 4:
					result = 4285464831u;
					break;
				case 5:
					result = 3993503999u;
					break;
				}
				break;
			}
		}
		return result;
	}

	public static string GetColorTextCode(JobsEnum job, int wantedLevel)
	{
		string result = "~s~";
		if (job == JobsEnum.Police)
		{
			result = "~b~";
		}
		else if (job == JobsEnum.Paramedic)
		{
			result = "~p~";
		}
		else if (job == JobsEnum.Firefighter)
		{
			result = "~HUD_COLOUR_NET_PLAYER1~";
		}
		else if (job == JobsEnum.Security)
		{
			result = "~HUD_COLOUR_NET_PLAYER10~";
		}
		else if (job == JobsEnum.Mechanic && wantedLevel == 0)
		{
			result = "~HUD_COLOUR_GREY~";
		}
		else if (job == JobsEnum.PrivateMedic && wantedLevel == 0)
		{
			result = "~HUD_COLOUR_NET_PLAYER3~";
		}
		else
		{
			switch (job)
			{
			case JobsEnum.Staff:
				result = "~g~";
				break;
			case JobsEnum.Invalid:
				result = "~m~";
				break;
			default:
				switch (wantedLevel)
				{
				case 0:
					result = "~s~";
					break;
				case 1:
					result = "~y~";
					break;
				case 2:
					result = "~o~";
					break;
				case 3:
					result = "~o~";
					break;
				case 4:
					result = "~o~";
					break;
				case 5:
					result = "~r~";
					break;
				}
				break;
			}
		}
		return result;
	}

	public static string GetColorConsoleCode(JobsEnum job, int wantedLevel)
	{
		string result = "^0";
		if (job == JobsEnum.Police)
		{
			result = "^4";
		}
		else if (job == JobsEnum.Paramedic || job == JobsEnum.Firefighter)
		{
			result = "^6";
		}
		else if (job == JobsEnum.Security)
		{
			result = "^5";
		}
		else if (job == JobsEnum.Mechanic && wantedLevel == 0)
		{
			result = "^7";
		}
		else
		{
			switch (job)
			{
			case JobsEnum.Staff:
				result = "^2";
				break;
			case JobsEnum.Invalid:
				result = "^0";
				break;
			default:
				switch (wantedLevel)
				{
				case 0:
					result = "^0";
					break;
				case 1:
					result = "^3";
					break;
				case 2:
				case 3:
				case 4:
					result = "^1";
					break;
				case 5:
					result = "^8";
					break;
				}
				break;
			}
		}
		return result;
	}

	public static int Min(params int[] values)
	{
		int num = int.MaxValue;
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Min(values[i], num);
		}
		return num;
	}

	public static int Max(params int[] values)
	{
		int num = int.MinValue;
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Max(values[i], num);
		}
		return num;
	}

	public static long Min(params long[] values)
	{
		long num = long.MaxValue;
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Min(values[i], num);
		}
		return num;
	}

	public static long Max(params long[] values)
	{
		long num = long.MinValue;
		for (int i = 0; i < values.Length; i++)
		{
			num = Math.Max(values[i], num);
		}
		return num;
	}

	public static float Clamp(this float val, float min, float max)
	{
		if (val > max)
		{
			return max;
		}
		if (val < min)
		{
			return min;
		}
		return val;
	}

	public static double Clamp(this double val, double min, double max)
	{
		if (val > max)
		{
			return max;
		}
		if (val < min)
		{
			return min;
		}
		return val;
	}

	public static int Clamp(this int val, int min, int max)
	{
		if (val > max)
		{
			return max;
		}
		if (val < min)
		{
			return min;
		}
		return val;
	}

	public static long Clamp(this long val, long min, long max)
	{
		if (val > max)
		{
			return max;
		}
		if (val < min)
		{
			return min;
		}
		return val;
	}

	public static int Square(this int val)
	{
		return val * val;
	}

	public static float Square(this float val)
	{
		return val * val;
	}

	public static double Square(this double val)
	{
		return val * val;
	}

	public static bool IsAngleInRange(int min, int max, int angle)
	{
		min %= 360;
		max %= 360;
		angle %= 360;
		while (min < 0)
		{
			min += 360;
		}
		while (max < min)
		{
			max += 360;
		}
		while (angle < min)
		{
			angle += 360;
		}
		if (min <= angle)
		{
			return angle <= max;
		}
		return false;
	}

	public static string GetBoneName(int boneId)
	{
		if (!new Dictionary<int, string>
		{
			[0] = "NONE",
			[12844] = "HEAD",
			[31085] = "HEAD",
			[31086] = "HEAD",
			[39317] = "NECK",
			[57597] = "SPINE",
			[23553] = "SPINE",
			[24816] = "SPINE",
			[24817] = "SPINE",
			[24818] = "SPINE",
			[10706] = "UPPER_BODY",
			[64729] = "UPPER_BODY",
			[11816] = "LOWER_BODY",
			[45509] = "LARM",
			[61163] = "LARM",
			[18905] = "LHAND",
			[4089] = "LFINGER",
			[4090] = "LFINGER",
			[4137] = "LFINGER",
			[4138] = "LFINGER",
			[4153] = "LFINGER",
			[4154] = "LFINGER",
			[4169] = "LFINGER",
			[4170] = "LFINGER",
			[4185] = "LFINGER",
			[4186] = "LFINGER",
			[26610] = "LFINGER",
			[26611] = "LFINGER",
			[26612] = "LFINGER",
			[26613] = "LFINGER",
			[26614] = "LFINGER",
			[58271] = "LLEG",
			[63931] = "LLEG",
			[2108] = "LFOOT",
			[14201] = "LFOOT",
			[40269] = "RARM",
			[28252] = "RARM",
			[57005] = "RHAND",
			[58866] = "RFINGER",
			[58867] = "RFINGER",
			[58868] = "RFINGER",
			[58869] = "RFINGER",
			[58870] = "RFINGER",
			[64016] = "RFINGER",
			[64017] = "RFINGER",
			[64064] = "RFINGER",
			[64065] = "RFINGER",
			[64080] = "RFINGER",
			[64081] = "RFINGER",
			[64096] = "RFINGER",
			[64097] = "RFINGER",
			[64112] = "RFINGER",
			[64113] = "RFINGER",
			[36864] = "RLEG",
			[51826] = "RLEG",
			[20781] = "RFOOT",
			[52301] = "RFOOT"
		}.TryGetValue(boneId, out var value))
		{
			return null;
		}
		if (!new Dictionary<string, string>
		{
			["HEAD"] = "Head",
			["NECK"] = "Neck",
			["SPINE"] = "Torso",
			["UPPER_BODY"] = "Torso",
			["LOWER_BODY"] = "Torso",
			["LARM"] = "Left Arm",
			["LHAND"] = "Left Hand",
			["LFINGER"] = "Left Hand",
			["LLEG"] = "Left Leg",
			["LFOOT"] = "Left Foot",
			["RARM"] = "Right Arm",
			["RHAND"] = "Right Hand",
			["RFINGER"] = "Right Hand",
			["RLEG"] = "Right Leg",
			["RFOOT"] = "Right Foot"
		}.TryGetValue(value, out var value2))
		{
			return null;
		}
		return value2;
	}

	public static List<Player> GetPlayersInRange(this PlayerList players, Vector3 coords, float range)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		List<Player> list = new List<Player>();
		foreach (Player player in players)
		{
			if (!((Entity)(object)player.Character == (Entity)null))
			{
				Vector3 position = ((Entity)player.Character).Position;
				if (((Vector3)(ref position)).DistanceToSquared(coords) <= range * range)
				{
					list.Add(player);
				}
			}
		}
		return list;
	}

	public static List<Player> GetPlayersInRange(this PlayerList players, Player player, float range)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Vector3? obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			Ped character = player.Character;
			obj = ((character != null) ? new Vector3?(((Entity)character).Position) : ((Vector3?)null));
		}
		Vector3? val = obj;
		return players.GetPlayersInRange(val.GetValueOrDefault(), range);
	}

	public static int GetLevelByXP(int xp)
	{
		int num = 1;
		int[] pLAYER_LEVELS = PLAYER_LEVELS;
		foreach (int num2 in pLAYER_LEVELS)
		{
			if (xp < num2)
			{
				break;
			}
			num++;
		}
		return num;
	}

	public static int CalculateHealPrice()
	{
		int entityHealth = API.GetEntityHealth(((PoolObject)Game.PlayerPed).Handle);
		int entityMaxHealth = API.GetEntityMaxHealth(((PoolObject)Game.PlayerPed).Handle);
		if (entityHealth == entityMaxHealth)
		{
			return -1;
		}
		int price = ShoppingScript.Supplies["Hospital"].First((BusinessSupply s) => s.Item == "heal").Price;
		float num = 1f - (float)entityHealth / (float)entityMaxHealth;
		int num2 = Convert.ToInt32(Math.Round((float)price * num));
		if (num2 < 0)
		{
			num2 = 0;
		}
		return num2;
	}

	public static int CalculateCurePrice()
	{
		return DiseaseScript.CurrentDiseaseDefinitions.Sum((IDisease d) => d.CureCost);
	}

	public static IEnumerable<string> Split(this string str, int chunkSize)
	{
		return from i in Enumerable.Range(0, str.Length / chunkSize)
			select str.Substring(i * chunkSize, chunkSize);
	}

	public static string RemoveExtraWhiteSpaces(this string str)
	{
		str = str.Replace('\t', ' ');
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder();
		string text = str;
		foreach (char c in text)
		{
			bool flag2 = c == ' ';
			if (!flag2 || !flag)
			{
				stringBuilder.Append(c);
				flag = flag2;
			}
		}
		return stringBuilder.ToString();
	}

	public static string Replace(this string str, string oldValue, string newValue, StringComparison comparisonType)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		if (str.Length == 0)
		{
			return str;
		}
		if (oldValue == null)
		{
			throw new ArgumentNullException("oldValue");
		}
		if (oldValue.Length == 0)
		{
			throw new ArgumentException("String cannot be of zero length.");
		}
		StringBuilder stringBuilder = new StringBuilder(str.Length);
		bool flag = string.IsNullOrEmpty(newValue);
		int num = 0;
		int num2;
		while ((num2 = str.IndexOf(oldValue, num, comparisonType)) != -1)
		{
			int num3 = num2 - num;
			if (num3 != 0)
			{
				stringBuilder.Append(str, num, num3);
			}
			if (!flag)
			{
				stringBuilder.Append(newValue);
			}
			num = num2 + oldValue.Length;
			if (num == str.Length)
			{
				return stringBuilder.ToString();
			}
		}
		int count = str.Length - num;
		stringBuilder.Append(str, num, count);
		return stringBuilder.ToString();
	}

	public static string GenerateAsciiString(int stringLength)
	{
		char[] array = new char[stringLength];
		for (int i = 0; i < stringLength; i++)
		{
			array[i] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[random.Next(0, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length)];
		}
		return new string(array);
	}

	public static string ReplaceHtmlSpecialCharacters(string input)
	{
		return input?.Replace("&quot;", "\"").Replace("&apos;", "'").Replace("&lt;", "<")
			.Replace("&gt;", ">")
			.Replace("&amp;", "&")
			.Replace("&nbsp;", " ");
	}

	public static bool IsAlphanumeric(this char c)
	{
		return new char[62]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
			'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
			'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd',
			'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
			'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
			'y', 'z'
		}.Contains(c);
	}

	public static string RegionalIndicatorsToLetters(this string input)
	{
		if (input == null)
		{
			return null;
		}
		bool flag = false;
		StringBuilder stringBuilder = null;
		for (int i = 0; i < input.Length; i++)
		{
			int num = UTF32Polyfill.ConvertToUtf32(input, i);
			if (UTF32Polyfill.IsHighSurrogate(input[i]))
			{
				i++;
			}
			if (num >= 127462 && num <= 127487)
			{
				if (!flag)
				{
					stringBuilder = new StringBuilder(input.Length);
					stringBuilder.Append(input, 0, i - 1);
					flag = true;
				}
				num -= 127397;
				stringBuilder.Append(UTF32Polyfill.ConvertFromUtf32(num));
			}
			else if (flag)
			{
				stringBuilder.Append(UTF32Polyfill.ConvertFromUtf32(num));
			}
		}
		if (!flag)
		{
			return input;
		}
		return stringBuilder.ToString();
	}

	public static string RemoveHealthOverlapingChars(this string input)
	{
		return input?.Replace("\ud83c\udff4\u200d☠\ufe0f", "\ufffd").Replace("\ud83d\ude93", "\ufffd").Replace("\ud83d\ude97", "\ufffd")
			.Replace("\ud83e\udd7d", "\ufffd")
			.Replace("\ud83d\udeac", "\ufffd")
			.Replace("\ud83c\udff4\udb40\udc67\udb40\udc62\udb40\udc73\udb40\udc63\udb40\udc74\udb40\udc7f", "\ufffd")
			.Replace("\ud83e\udee6", "\ufffd")
			.Replace("\ud83e\udd77\ud83c\udffb", "\ufffd")
			.Replace("\ud83e\udd77\ud83c\udffc", "\ufffd")
			.Replace("\ud83e\udd77\ud83c\udffd", "\ufffd")
			.Replace("\ud83e\udd77\ud83c\udffe", "\ufffd")
			.Replace("\ud83e\udd77\ud83c\udfff", "\ufffd")
			.Replace("\ud83e\ude90", "\ufffd")
			.Replace("\ud83d\udc41", "\ufffd")
			.Replace("\ud83d\udc45", "\ufffd")
			.Replace("\ud83d\ude9b", "\ufffd")
			.Replace("\ud83d\udcb5", "\ufffd")
			.Replace("\ud83d\udc1f", "\ufffd")
			.Replace("\ud83e\udd85", "\ufffd")
			.Replace("\ud83e\udd77", "\ufffd")
			.Replace("\ud83e\uddab", "\ufffd")
			.Replace("〽\ufe0f", "\ufffd")
			.Replace("\ud83d\udcbb", "\ufffd")
			.Replace("\ud83c\udf2e", "\ufffd")
			.Replace("\ud83c\udfb0", "\ufffd")
			.Replace("\ud83d\ude9a", "\ufffd")
			.Replace("\ud83d\udc16", "\ufffd")
			.Replace("\ud83d\udcce", "\ufffd")
			.Replace("\ud83d\udc0a", "\ufffd")
			.Replace("\ud83e\udd1c", "\ufffd")
			.Replace("\ud83e\udd1c\ud83c\udffb", "\ufffd")
			.Replace("\ud83e\udd1c\ud83c\udffc", "\ufffd")
			.Replace("\ud83e\udd1c\ud83c\udffd", "\ufffd")
			.Replace("\ud83e\udd1c\ud83c\udffe", "\ufffd")
			.Replace("\ud83e\udd1c\ud83c\udfff", "\ufffd")
			.Replace("\ud83e\udd1b", "\ufffd")
			.Replace("\ud83e\udd1b\ud83c\udffb", "\ufffd")
			.Replace("\ud83e\udd1b\ud83c\udffc", "\ufffd")
			.Replace("\ud83e\udd1b\ud83c\udffd", "\ufffd")
			.Replace("\ud83e\udd1b\ud83c\udffe", "\ufffd")
			.Replace("\ud83e\udd1b\ud83c\udfff", "\ufffd")
			.Replace("\ud83e\udd69", "\ufffd")
			.Replace("\ud83d\udc22", "\ufffd")
			.Replace("▪\ufe0f", "\ufffd")
			.Replace("\ud83e\udd54", "\ufffd")
			.Replace("\ud83d\udc60", "\ufffd")
			.Replace("⛓\ufe0f", "\ufffd");
	}

	public static string RemoveGta5TextFormatting_(this string input)
	{
		return RemoveGta5TextFormatting(input);
	}

	public static string RemoveGta5TextFormatting(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < input.Length; i++)
		{
			if (input[i] == '~')
			{
				flag = !flag;
			}
			else if (!flag)
			{
				stringBuilder.Append(input[i]);
			}
		}
		return stringBuilder.ToString();
	}

	public static string RemoveConsoleTextFormatting_(this string input)
	{
		return RemoveConsoleTextFormatting(input);
	}

	public static string RemoveConsoleTextFormatting(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		for (int i = 0; i < 10; i++)
		{
			input = input.Replace($"^{i}", "");
		}
		return input;
	}

	public static string EscapeForJson(this string str)
	{
		return str.Replace("\"", "\\\"");
	}

	public static bool CheckTimePassed(DateTime timeStamp, double milliseconds)
	{
		return (((timeStamp.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now) - timeStamp).TotalMilliseconds > milliseconds;
	}

	public static bool CheckTimePassed(DateTime timeStamp, TimeSpan timeSpan)
	{
		return ((timeStamp.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now) - timeStamp > timeSpan;
	}

	public static string ToMysqlDateTime(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
	}

	public static DateTime FromMysqlDateTime(this string dateTime)
	{
		return DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}

	public static string ToMysqlDate(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-dd");
	}

	public static DateTime FromMysqlDate(this string dateTime)
	{
		return DateTime.ParseExact(dateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture);
	}

	public static string ToFormalDateTime(this DateTime dateTime)
	{
		return dateTime.ToString("ddd MMM dd, yyyy h:mm tt");
	}

	public static DateTime FromFormalDateTime(this string dateTime)
	{
		return DateTime.ParseExact(dateTime, "ddd MMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture);
	}

	public static string ToFormalDate(this DateTime dateTime)
	{
		return dateTime.ToString("ddd MMM dd, yyyy");
	}

	public static DateTime FromFormalDate(this string dateTime)
	{
		return DateTime.ParseExact(dateTime, "ddd MMM dd, yyyy", CultureInfo.InvariantCulture);
	}

	public static string ToFormalDate2(this DateTime dateTime)
	{
		return dateTime.ToString("MMM dd, yyyy");
	}

	public static DateTime FromFormalDate2(this string dateTime)
	{
		return DateTime.ParseExact(dateTime, "MMM dd, yyyy", CultureInfo.InvariantCulture);
	}

	public static string SecondsToMinutesAndSeconds(int seconds)
	{
		return $"{default(DateTime).AddSeconds(seconds):mm:ss}";
	}

	public static string FormatTimerString(TimeSpan timeSpan, bool includeMiliseconds = false)
	{
		if (includeMiliseconds)
		{
			return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds / 10:D2}";
		}
		return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
	}

	public static string FormatShortDateString(this DateTime dt, bool addYear = false)
	{
		return dt.ToString(addYear ? "MMM dd, yyyy" : "MMM dd");
	}

	public static DateTime ParseDate(string dateString)
	{
		return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
	}

	public static DateTime ParseDateTime(string dateTimeString)
	{
		return DateTime.ParseExact(dateTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}

	public static string CalculateTimeAgo(DateTime referenceTime)
	{
		TimeSpan timeSpan = ((referenceTime.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now) - referenceTime;
		string result = string.Format("{0} second{1} ago", timeSpan.TotalSeconds.ToIntFloor(), (timeSpan.TotalSeconds.ToIntFloor() == 1) ? "" : "s");
		if (timeSpan.TotalMinutes >= 1.0)
		{
			result = string.Format("{0} minute{1} ago", timeSpan.TotalMinutes.ToIntFloor(), (timeSpan.TotalMinutes.ToIntFloor() == 1) ? "" : "s");
		}
		if (timeSpan.TotalHours >= 1.0)
		{
			result = string.Format("{0} hour{1} ago", timeSpan.TotalHours.ToIntFloor(), (timeSpan.TotalHours.ToIntFloor() == 1) ? "" : "s");
		}
		if (timeSpan.TotalDays >= 1.0)
		{
			result = string.Format("{0} day{1} ago", timeSpan.TotalDays.ToIntFloor(), (timeSpan.TotalDays.ToIntFloor() == 1) ? "" : "s");
		}
		return result;
	}

	public static string CalculateTimeIn(DateTime referenceTime)
	{
		DateTime dateTime = ((referenceTime.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now);
		return CalculateTimeIn(referenceTime - dateTime);
	}

	public static string CalculateTimeIn(TimeSpan span)
	{
		int num = span.TotalSeconds.ToIntFloor();
		if (num < 0)
		{
			num = 0;
		}
		string result = string.Format("in {0} second{1}", num, (num == 1) ? "" : "s");
		if (span.TotalMinutes >= 1.0)
		{
			result = string.Format("in {0} minute{1}", span.TotalMinutes.ToIntFloor(), (span.TotalMinutes.ToIntFloor() == 1) ? "" : "s");
		}
		if (span.TotalHours >= 1.0)
		{
			result = string.Format("in {0} hour{1}", span.TotalHours.ToIntFloor(), (span.TotalHours.ToIntFloor() == 1) ? "" : "s");
		}
		if (span.TotalDays >= 1.0)
		{
			result = string.Format("in {0} day{1}", span.TotalDays.ToIntFloor(), (span.TotalDays.ToIntFloor() == 1) ? "" : "s");
		}
		return result;
	}

	public static string CalculateDetailedTimeIn(DateTime referenceTime)
	{
		DateTime dateTime = ((referenceTime.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now);
		return CalculateDetailedTimeIn(referenceTime - dateTime);
	}

	public static string CalculateDetailedTimeIn(TimeSpan span)
	{
		if (span.TotalSeconds <= 0.0)
		{
			return "now";
		}
		int days = span.Days;
		int hours = span.Hours;
		int minutes = span.Minutes;
		int seconds = span.Seconds;
		List<string> list = new List<string>();
		if (days > 0)
		{
			list.Add(string.Format("{0} day{1}", days, (days == 1) ? "" : "s"));
		}
		if (hours > 0)
		{
			list.Add(string.Format("{0} hour{1}", hours, (hours == 1) ? "" : "s"));
		}
		if (minutes > 0)
		{
			list.Add(string.Format("{0} minute{1}", minutes, (minutes == 1) ? "" : "s"));
		}
		if (seconds > 0)
		{
			list.Add(string.Format("{0} second{1}", seconds, (seconds == 1) ? "" : "s"));
		}
		List<string> values = list.Take(2).ToList();
		return "in " + string.Join(" and ", values);
	}

	public static TimeSpan GetCooldownTimeLeft(DateTime timestamp, TimeSpan cooldown)
	{
		if (!CheckTimePassed(timestamp, cooldown))
		{
			DateTime dateTime = ((timestamp.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now);
			return cooldown - (dateTime - timestamp);
		}
		return TimeSpan.Zero;
	}

	public static string FormatTimeSpanString(TimeSpan dt)
	{
		if (!(dt.TotalDays >= 1.0))
		{
			if (!(dt.TotalHours >= 1.0))
			{
				if (!(dt.TotalMinutes >= 1.0))
				{
					return $"{Math.Round(dt.TotalSeconds):0} seconds";
				}
				return $"{Math.Round(dt.TotalMinutes):0} minutes";
			}
			return $"{Math.Round(dt.TotalHours):0} hours";
		}
		return $"{Math.Round(dt.TotalDays):0} days";
	}

	public static TimeSpan ParseTimeSpan(string timeString, char[] validTimeUnits = null)
	{
		if (string.IsNullOrEmpty(timeString) || timeString.Length < 2)
		{
			throw new ArgumentException("Invalid time format.");
		}
		string s = timeString.Substring(0, timeString.Length - 1);
		char c = timeString[timeString.Length - 1];
		if (!uint.TryParse(s, out var result))
		{
			throw new ArgumentException("Invalid numeric value.");
		}
		if (validTimeUnits == null)
		{
			validTimeUnits = new char[4] { 'd', 'h', 'm', 's' };
		}
		if (!validTimeUnits.Contains(c))
		{
			throw new ArgumentException(string.Format("Invalid time unit '{0}'. Valid units are: {1}", c, string.Join(", ", validTimeUnits)));
		}
		return c switch
		{
			'd' => TimeSpan.FromDays(result), 
			'h' => TimeSpan.FromHours(result), 
			'm' => TimeSpan.FromMinutes(result), 
			's' => TimeSpan.FromSeconds(result), 
			_ => throw new ArgumentException("Invalid time unit."), 
		};
	}

	public static Vector3 XYZ(this Vector4 vec)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(vec.X, vec.Y, vec.Z);
	}

	public static Vector2 XY(this Vector4 vec)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(vec.X, vec.Y);
	}

	public static Vector2 XY(this Vector3 vec)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(vec.X, vec.Y);
	}

	public static float[] ToFloatArray(this Vector2 vec)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		return new float[2] { vec.X, vec.Y };
	}

	public static float[] ToFloatArray(this Vector3 vec)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		return new float[3] { vec.X, vec.Y, vec.Z };
	}

	public static float[] ToFloatArray(this Vector4 vec)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		return new float[4] { vec.X, vec.Y, vec.Z, vec.W };
	}

	public static Vector2 ToVector2(this float[] fArray)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(fArray[0], fArray[1]);
	}

	public static Vector3 ToVector3(this float[] fArray)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(fArray[0], fArray[1], fArray[2]);
	}

	public static Vector4 ToVector4(this float[] fArray)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new Vector4(fArray[0], fArray[1], fArray[2], fArray[3]);
	}

	public static string ToCSV(this Vector2 vec)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return $"{vec.X}, {vec.Y}";
	}

	public static string ToCSV(this Vector3 vec)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return $"{vec.X}, {vec.Y}, {vec.Z}";
	}

	public static string ToCSV(this Vector4 vec)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		return $"{vec.X}, {vec.Y}, {vec.Z}, {vec.W}";
	}

	public static bool IsPointInPolygon(float[][] polygon, Vector3 testPoint)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		int num = polygon.Length - 1;
		for (int i = 0; i < polygon.Length; i++)
		{
			if (((polygon[i][1] < testPoint.Y && polygon[num][1] >= testPoint.Y) || (polygon[num][1] < testPoint.Y && polygon[i][1] >= testPoint.Y)) && polygon[i][0] + (testPoint.Y - polygon[i][1]) / (polygon[num][1] - polygon[i][1]) * (polygon[num][0] - polygon[i][0]) < testPoint.X)
			{
				flag = !flag;
			}
			num = i;
		}
		return flag;
	}

	public static bool IsVehicleModelARestrictedVehicle(string category, int vehicleModelHash)
	{
		return restrictedVehicles[category].Any((int v) => v == vehicleModelHash);
	}

	public static bool IsVehicleModelAPoliceVehicle(int vehicleModelHash)
	{
		return IsVehicleModelARestrictedVehicle("LawEnforcement", vehicleModelHash);
	}

	public static bool IsVehicleModelAnArmoredEmergencyVehicle(int vehicleModelHash)
	{
		return IsVehicleModelARestrictedVehicle("ArmoredEmergency", vehicleModelHash);
	}

	public static bool IsVehicleModelAParamedicVehicle(int vehicleModelHash)
	{
		return IsVehicleModelARestrictedVehicle("EMS", vehicleModelHash);
	}

	public static bool IsVehicleModelAFireDeptVehicle(int vehicleModelHash)
	{
		return IsVehicleModelARestrictedVehicle("FD", vehicleModelHash);
	}

	public static bool IsVehicleModelAMilitaryVehicle(int vehicleModelHash)
	{
		return IsVehicleModelARestrictedVehicle("Military", vehicleModelHash);
	}

	public static bool IsVehicleModelAnEmergencyVehicle(int vehicleModelHash)
	{
		if (!IsVehicleModelAPoliceVehicle(vehicleModelHash) && !IsVehicleModelAParamedicVehicle(vehicleModelHash) && !IsVehicleModelAFireDeptVehicle(vehicleModelHash) && !IsVehicleModelAnArmoredEmergencyVehicle(vehicleModelHash))
		{
			return IsVehicleModelAMilitaryVehicle(vehicleModelHash);
		}
		return true;
	}

	public static string GenerateLicensePlate()
	{
		string text = "";
		string text2 = "0XXX000";
		for (int i = 0; i < text2.Length; i++)
		{
			switch (text2[i])
			{
			case '0':
				text += (char)random.Next(48, 58);
				break;
			case 'X':
				text += (char)random.Next(65, 91);
				break;
			}
		}
		return text;
	}

	public static int CalculateRepairPrice(float engineHealth, float bodyHealth, float tankHealth, int vehicleCategory, uint tiresDamaged = 0u, MechanicRepairtype repairtype = MechanicRepairtype.Full)
	{
		float num = 1000f - engineHealth;
		float num2 = 1000f - bodyHealth;
		float num3 = 1000f - tankHealth;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		switch (repairtype)
		{
		case MechanicRepairtype.Quick:
			if (num <= 250f && num3 <= 250f)
			{
				return -1;
			}
			num = ((num > 250f) ? (num - 250f) : 0f);
			num4 = (int)Math.Round(num * 4f);
			num3 = ((num3 > 250f) ? (num3 - 250f) : 0f);
			num6 = (int)Math.Round(num3 * 3f);
			break;
		case MechanicRepairtype.Tires:
			if (tiresDamaged == 0)
			{
				return -1;
			}
			num7 = tiresDamaged * 500;
			break;
		case MechanicRepairtype.Full:
			if (num == 0f && num3 == 0f && num2 == 0f && tiresDamaged == 0)
			{
				return -1;
			}
			num4 = (int)Math.Round(num * 4f);
			num5 = (int)Math.Round(num2 * 6f);
			num6 = (int)Math.Round(num3 * 3f);
			num7 = tiresDamaged * 500;
			break;
		}
		int num8 = Convert.ToInt32(Math.Round(num4 + num5 + num6 + num7));
		switch (vehicleCategory)
		{
		case 1:
			num8 = Convert.ToInt32(Math.Round((float)num8 * 2f));
			break;
		case 2:
			num8 = Convert.ToInt32(Math.Round((float)num8 * 2.4f));
			break;
		case 3:
			num8 = Convert.ToInt32(Math.Round((float)num8 * 1.6f));
			break;
		}
		return num8;
	}

	public static int CalculateWashPrice(int vehicleCategory)
	{
		return vehicleCategory switch
		{
			1 => 4500, 
			2 => 3000, 
			3 => 2200, 
			_ => 800, 
		};
	}

	public static int CalculateResprayPrice(int vehicleCategory)
	{
		return vehicleCategory switch
		{
			1 => 20000, 
			2 => 15000, 
			3 => 9000, 
			_ => 6000, 
		};
	}

	public static int CalculateTintPrice(int tintIndex)
	{
		int[] array = new int[7] { 10000, 25000, 22000, 20000, 15000, 19000, 21000 };
		if (tintIndex >= 0 && tintIndex < array.Length)
		{
			return array[tintIndex];
		}
		return 0;
	}

	public static int GenerateHash(string input)
	{
		uint num = 0u;
		if (!string.IsNullOrEmpty(input))
		{
			int length = input.Length;
			input = input.ToLowerInvariant();
			for (int i = 0; i < length; i++)
			{
				num += input[i];
				num += num << 10;
				num ^= num >> 6;
			}
			num += num << 3;
			num ^= num >> 11;
			num += num << 15;
		}
		return (int)num;
	}

	public static async Task BackToMainThread()
	{
		await BaseScript.Delay(0);
	}

	public static string LoadCurrentResourceFile(string fileName)
	{
		return API.LoadResourceFile(API.GetCurrentResourceName(), fileName);
	}

	public static T LoadJson<T>(string fileName)
	{
		return LoadCurrentResourceFile(fileName).Unjson<T>();
	}

	public static T LoadJson<T>(string resName, string fileName)
	{
		return API.LoadResourceFile(resName, fileName).Unjson<T>();
	}

	public static string LoadEmbeddedResource(string resourceName, params Tuple<string, string>[] args)
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
		using StreamReader streamReader = new StreamReader(stream);
		string text = streamReader.ReadToEnd();
		if (args.Length != 0)
		{
			foreach (Tuple<string, string> tuple in args)
			{
				text = text.Replace("${" + tuple.Item1 + "}", tuple.Item2);
			}
		}
		return text;
	}

	public static int GetRandomInt(int minimum = 0, int maximumExclusive = 100)
	{
		return random.Next(minimum, maximumExclusive);
	}

	public static double GetRandomDouble(double minimum = 0.0, double maximum = 1.0)
	{
		return random.NextDouble() * (maximum - minimum) + minimum;
	}

	public static string ResolveLocalization(string inputString, string overrideLang = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(inputString))
			{
				return inputString;
			}
			string text = overrideLang;
			if (text == null)
			{
				text = LocalizationController.CurrentLanguage;
			}
			if (inputString.StartsWith("@{") && inputString.EndsWith("}"))
			{
				string text2 = inputString.Substring(2, inputString.Length - 3);
				List<object> list = new List<object>();
				if (text2.Contains(","))
				{
					string text3 = text2;
					text2 = text2.Substring(0, text2.IndexOf(','));
					foreach (string item in text3.Split(',').Skip(1))
					{
						string text4 = item;
						if (text4.Contains(":"))
						{
							int num = text4.IndexOf(':');
							string text5 = text4.Substring(num + 1, text4.Length - num - 1);
							text4 = text4.Substring(0, num);
							if (!(text5 == "$"))
							{
								if (text5 == null || text5.Length != 0)
								{
									text4 = string.Format(text4, text5);
								}
							}
							else
							{
								text4 = long.Parse(text4).ToCurrencyString();
							}
						}
						text4 = ResolveLocalization(text4);
						list.Add(text4);
					}
				}
				inputString = LocalizationController.GetLocalizedString(text, text2, list.ToArray());
			}
		}
		catch (Exception exception)
		{
			PrintException(exception);
			Debug.WriteLine("Input: " + inputString);
		}
		return inputString;
	}

	public static string ResolveGTALabel(string inputString)
	{
		if (inputString.StartsWith("!{") && inputString.EndsWith("}"))
		{
			inputString = API.GetLabelText(inputString.Substring(2, inputString.Length - 3));
			if (inputString == "NULL")
			{
				inputString = "[Unknown Name]";
			}
		}
		return inputString;
	}

	public static void PrintException(Exception exception)
	{
		Debug.WriteLine("^1An exception has occurred (" + exception.GetType().Name + "): " + exception.Message + "\n^3" + exception.StackTrace.Replace("\n", "\n^3") + "^0\n\n                       ^4> Attach a screenshot of this message if you want to report this problem <\n\n");
	}

	public static void PrintStackTrace()
	{
		Debug.WriteLine("Stack Trace:");
		StackTrace stackTrace = new StackTrace();
		for (int i = 1; i < 99; i++)
		{
			StackFrame frame = stackTrace.GetFrame(i);
			if (frame != null)
			{
				Debug.WriteLine($"    {i:00}. {frame.GetMethod().DeclaringType.FullName}.{frame.GetMethod().Name}");
				continue;
			}
			break;
		}
	}

	public static bool IsAladdinPotter(string userId)
	{
		return userId == "usr-n81ej0DznEysACVDaoFLiw";
	}

	public static string GetWeaponName(int damageHash)
	{
		WeaponDefinition weaponDefinitionByHash = Items.GetWeaponDefinitionByHash((uint)damageHash);
		string result = null;
		if (weaponDefinitionByHash != null)
		{
			result = weaponDefinitionByHash.Name;
		}
		else
		{
			switch (damageHash)
			{
			case -1553120962:
			case 133987706:
				result = "Vehicle";
				break;
			case 539292904:
				result = "Explosion";
				break;
			case -544306709:
				result = "Fire";
				break;
			case -842959696:
				result = "Fall";
				break;
			case -10959621:
			case 1936677264:
				result = "Drowned";
				break;
			case -999:
				result = "Drug Overdose";
				break;
			case -868994466:
				result = "Water Cannon";
				break;
			case 0:
				result = "Unknown";
				break;
			}
		}
		return result;
	}

	public static string GenerateMapLink(Vector3 pos1, string name1)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		string arg = Uri.EscapeDataString(name1 ?? "");
		return $"https://gtacnr.net/internal/map?p1={pos1.X:0.00},{pos1.Y:0.00},{arg}";
	}

	public static string GenerateMapLink(Vector3 pos1, string name1, Vector3 pos2, string name2)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		string text = Uri.EscapeDataString(name1 ?? "");
		string text2 = Uri.EscapeDataString(name2 ?? "");
		return $"https://gtacnr.net/internal/map?p1={pos1.X:0.00},{pos1.Y:0.00},{text}&p2={pos2.X:0.00},{pos2.Y:0.00},{text2}";
	}

	public static bool IsResourceLoadedOrLoading(string resourceName)
	{
		string resourceState = API.GetResourceState(resourceName);
		if (resourceState == "started" || resourceState == "starting")
		{
			return true;
		}
		return false;
	}
}
