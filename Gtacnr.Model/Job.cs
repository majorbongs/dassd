using System.Collections.Generic;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Model;

public class Job
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Emoji { get; set; }

	public int Paycheck { get; set; }

	public int MinLevel { get; set; } = 1;

	public int MinPlaytime { get; set; }

	public bool SeparateLoadout { get; set; }

	public bool SeparateOutfit { get; set; }

	public bool SeparateSpawnLocations { get; set; }

	public bool SeparateVehicles { get; set; }

	public bool HasJobInventory { get; set; }

	public bool CanOffer { get; set; }

	public bool CanOfferToPublicJobs { get; set; }

	public float InventoryCapacity { get; set; } = -1f;

	public HashSet<string> StockItems { get; set; } = new HashSet<string>();

	public HashSet<string> StockWeapons { get; set; } = new HashSet<string>();

	public HashSet<string> StockAmmo { get; set; } = new HashSet<string>();

	public HashSet<string> StockAttachments { get; set; } = new HashSet<string>();

	public bool CanStockItems
	{
		get
		{
			if (StockItems.Count <= 0 && StockWeapons.Count <= 0 && StockAmmo.Count <= 0)
			{
				return StockAttachments.Count > 0;
			}
			return true;
		}
	}

	public int Cooldown { get; set; }

	public bool HasToBeInnocent { get; set; }

	public float MaxPlayersPercent { get; set; } = 1f;

	public Loadout DefaultLoadout { get; set; }

	public Dictionary<Sex, Dictionary<string, List<string>>> DefaultOutfits { get; set; }

	public List<string> Services { get; set; } = new List<string>();

	public string RequiredItem { get; set; }

	public float DefaultRadio { get; set; }

	public override string ToString()
	{
		return Name;
	}

	public string GetColoredName(int wantedLevel = 0, bool lowerCase = false)
	{
		string text = ((Id != "none") ? Name : "Civilian");
		if (lowerCase)
		{
			text = text.ToLower();
		}
		return Utils.GetColorTextCode(Utils.JobMapper.JobToEnum(Id), wantedLevel) + text + "~s~";
	}

	public MenuItem ToSwitchMenuItem()
	{
		int levelByXP = Utils.GetLevelByXP(Users.CachedXP);
		bool flag = MinLevel > 0 && levelByXP < MinLevel;
		MenuItem menuItem = new MenuItem(LocalizationController.S(Entries.Jobs.JOBS_SWITCH_TO, GetColoredName(0, lowerCase: true)));
		menuItem.Description = LocalizationController.S(Entries.Jobs.JOBS_SWITCH_TO_DESCRIPTION, GetColoredName(0, lowerCase: true)) + (flag ? ("\n~y~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, MinLevel)) : "");
		menuItem.Enabled = !flag;
		menuItem.RightIcon = (flag ? MenuItem.Icon.LOCK : MenuItem.Icon.NONE);
		return menuItem;
	}
}
