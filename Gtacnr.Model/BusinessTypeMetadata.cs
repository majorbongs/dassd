using System.Collections.Generic;

namespace Gtacnr.Model;

public class BusinessTypeMetadata
{
	private string name;

	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = Utils.ResolveLocalization(value);
		}
	}

	public bool ShowBlip { get; set; }

	public List<string> BlipJobs { get; set; }

	public int Sprite { get; set; }

	public int Color { get; set; }

	public float Scale { get; set; } = 1f;

	public string Ped { get; set; }

	public List<string> Outfits { get; set; }

	public bool RandomPedVariation { get; set; }

	public bool CashierFightsBack { get; set; }

	public float RobberyChance { get; set; }

	public int RobberyAmount { get; set; }

	public bool CanShoplift { get; set; } = true;

	public static BusinessTypeMetadata Clone(BusinessTypeMetadata meta)
	{
		return new BusinessTypeMetadata
		{
			ShowBlip = meta.ShowBlip,
			BlipJobs = meta.BlipJobs,
			Sprite = meta.Sprite,
			Color = meta.Color,
			Scale = meta.Scale,
			Name = meta.Name,
			Ped = meta.Ped,
			Outfits = meta.Outfits,
			RandomPedVariation = meta.RandomPedVariation,
			CashierFightsBack = meta.CashierFightsBack,
			RobberyChance = meta.RobberyChance,
			RobberyAmount = meta.RobberyAmount,
			CanShoplift = meta.CanShoplift
		};
	}
}
