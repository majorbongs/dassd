using System.Text;
using Gtacnr.Localization;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class DailyChallenge
{
	public string Id { get; set; }

	public string Name { get; set; }

	[JsonProperty("Description")]
	private string Description { get; set; }

	public string? Job { get; set; }

	public uint Min { get; set; }

	public uint Max { get; set; }

	public uint Step { get; set; }

	public uint CashPerStep { get; set; }

	public uint XPPerStep { get; set; }

	public bool ShowAsCurrency { get; set; }

	public string GetLocalizedDescriptionString(uint requiredPoints)
	{
		string value = (ShowAsCurrency ? LocalizationController.S(Description, requiredPoints.ToCurrencyString()) : LocalizationController.S(Description, requiredPoints));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(value);
		stringBuilder.AppendLine(LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_REWARDS));
		CalculateRewards(requiredPoints, out var cash, out var xp);
		bool flag = false;
		if (cash != 0)
		{
			stringBuilder.AppendLine("~g~" + cash.ToCurrencyString() + "~s~");
			flag = true;
		}
		if (xp != 0)
		{
			stringBuilder.AppendLine($"~b~{xp} XP~s~");
			flag = true;
		}
		if (!flag)
		{
			stringBuilder.Append(LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_REWARDS_NONE));
		}
		return stringBuilder.ToString().Replace("\r", "").Trim();
	}

	public int CalculateRewards(uint points, out uint cash, out uint xp)
	{
		uint num = points / Step;
		cash = num * CashPerStep;
		xp = num * XPPerStep;
		return 0;
	}
}
