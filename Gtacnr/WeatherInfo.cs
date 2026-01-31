using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr;

public struct WeatherInfo
{
	public Weather Weather { get; private set; }

	public string Id { get; private set; }

	public string Description { get; private set; }

	public int Chance { get; set; }

	public static List<WeatherInfo> All => new List<WeatherInfo>
	{
		new WeatherInfo((Weather)0, "EXTRASUNNY", "Extra-Sunny", 64),
		new WeatherInfo((Weather)1, "CLEAR", "Clear", 32),
		new WeatherInfo((Weather)2, "CLOUDS", "Clouds", 16),
		new WeatherInfo((Weather)5, "OVERCAST", "Overcast", 8),
		new WeatherInfo((Weather)3, "SMOG", "Smog", 4),
		new WeatherInfo((Weather)4, "FOGGY", "Foggy", 2),
		new WeatherInfo((Weather)6, "RAIN", "Rainy", 1),
		new WeatherInfo((Weather)7, "THUNDER", "Thunderstorm", 1),
		new WeatherInfo((Weather)8, "CLEARING", "Clearing", 0)
	};

	private WeatherInfo(Weather weather, string id, string name, int chance)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		Weather = weather;
		Id = id;
		Description = name;
		Chance = chance;
	}

	public override string ToString()
	{
		return Description;
	}

	public static WeatherInfo GetRandom()
	{
		WeightedChanceExecutor<WeatherInfo> weightedChanceExecutor = new WeightedChanceExecutor<WeatherInfo>();
		weightedChanceExecutor.Parameters = new WeightedChanceParam<WeatherInfo>[All.Count];
		int num = 0;
		foreach (WeatherInfo weather in All)
		{
			weightedChanceExecutor.Parameters[num] = new WeightedChanceParam<WeatherInfo>(() => weather, weather.Chance);
			num++;
		}
		return weightedChanceExecutor.Execute();
	}
}
