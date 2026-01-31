using System;
using Gtacnr.Model.PrefixedGUIDs;
using MenuAPI;

namespace Gtacnr.Model;

public class Crew
{
	public CrewId Id { get; set; }

	public string Acronym { get; set; }

	public string Name { get; set; }

	public string Motto { get; set; }

	public string Description { get; set; }

	public string LogoURL { get; set; }

	public int Score { get; set; }

	public DateTime? CreationDateTime { get; set; }

	public CrewRankData RankData { get; set; } = new CrewRankData();

	public CrewStyleData StyleData { get; set; } = new CrewStyleData();

	public CrewSettingsData SettingsData { get; set; } = new CrewSettingsData();

	public AcronymStyleData AcronymStyle { get; set; } = new AcronymStyleData();

	public MenuItem ToMenuItem()
	{
		MenuItem menuItem = new MenuItem(Acronym);
		menuItem.Description = "~b~" + Name + "~n~~b~" + Motto + "~n~" + $"~b~Score: ~s~{Score}~n~" + "~b~Created: ~s~" + (CreationDateTime?.ToString("MMMM dd, yyyy") ?? "Unknown");
		menuItem.ItemData = this;
		return menuItem;
	}
}
